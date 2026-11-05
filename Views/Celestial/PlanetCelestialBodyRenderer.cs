
using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.WorldGlobe;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Celestial;

internal sealed class PlanetCelestialBodyRenderer :
    ICelestialBodyRenderer,
    IAttachmentContainer,
    ICelestialBodyVisualTarget,
    IWorldGlobeAnimatedObject
{
    public CelestialBodyRendererType Type => CelestialBodyRendererType.Planet;

    private static readonly MeshGeometry3D SphereMesh =
        CreateSphereMesh(radius: 1.0, thetaDiv: 128, phiDiv: 64);

    private readonly Model3DGroup _root = new();
    private readonly Model3DGroup _surfaceGroup = new();
    private readonly Model3DGroup _activityGroup = new();
    private readonly Model3DGroup _attachmentRoot = new();

    private readonly GeometryModel3D _surfaceModel = new();

    private readonly ScaleTransform3D _scaleTransform =
        new(1.0, 1.0, 1.0);

    private readonly ScaleTransform3D _activityScaleTransform =
        new(1.0, 1.0, 1.0);

    private readonly Transform3DGroup _animationTransform = new();

    private CelestialBody? _body;

    private readonly Dictionary<string, IAttachmentRenderer> _attachmentRenderers = [];

    private readonly Dictionary<string, ICelestialBodyAnimationRenderer> _animationRenderers = [];

    // ICelestialBodyAnimationTarget
    public Transform3DGroup AnimationTransform => _animationTransform;

    // ICelestialBodyVisualTarget
    public Model3DGroup BodyVisualRoot => _surfaceGroup;

    public Model3DGroup ActivityVisualRoot => _activityGroup;

    public Color BodyColor => _body?.BaseColor ?? Colors.White;

    public PlanetCelestialBodyRenderer()
    {
        Transform3DGroup surfaceTransform = new();

        surfaceTransform.Children.Add(_scaleTransform);
        surfaceTransform.Children.Add(_animationTransform);

        _surfaceGroup.Transform = surfaceTransform;

        // Activity visuals receive body-radius scaling,
        // but do not inherit the surface animation transform.
        _activityGroup.Transform = _activityScaleTransform;

        _surfaceModel.Geometry = SphereMesh;

        _surfaceGroup.Children.Add(_surfaceModel);

        // The planet surface, activity visuals, and attachments
        // are siblings with independent transform responsibilities.
        _root.Children.Add(_surfaceGroup);
        _root.Children.Add(_activityGroup);
        _root.Children.Add(_attachmentRoot);
    }

    public Model3DGroup Root => _root;

    public void Update(CelestialBody body, ImageSource? texture)
    {
        ArgumentNullException.ThrowIfNull(body);

        _body = body;

        _scaleTransform.ScaleX = body.Radius;
        _scaleTransform.ScaleY = body.Radius;
        _scaleTransform.ScaleZ = body.Radius;

        _activityScaleTransform.ScaleX = body.Radius;
        _activityScaleTransform.ScaleY = body.Radius;
        _activityScaleTransform.ScaleZ = body.Radius;

        Material material;

        if (texture is not null)
        {
            ImageBrush imageBrush = new(texture)
            {
                Stretch = Stretch.Fill,
                TileMode = TileMode.None
            };

            material = new DiffuseMaterial(imageBrush);
        }
        else
        {
            material = new DiffuseMaterial(
                new SolidColorBrush(body.BaseColor));
        }

        _surfaceModel.Material = material;
        _surfaceModel.BackMaterial = material;

        RefreshAnimationRenderers();
    }

    internal static class AttachmentRendererFactory
    {
        public static IAttachmentRenderer Create(
            CelestialBodyAttachment attachment)
        {
            ArgumentNullException.ThrowIfNull(attachment);

            return attachment.Type switch
            {
                CelestialBodyAttachmentType.PlanetaryRings =>
                    new PlanetaryRingsAttachmentRenderer(),

                CelestialBodyAttachmentType.CloudLayer =>
                    new CloudLayerAttachmentRenderer(),

                _ => throw new NotSupportedException(
                    $"Unsupported attachment type: {attachment.Type}")
            };
        }
    }

    public void RefreshAttachments(
        Func<CelestialBodyAttachment, BitmapSource?> textureResolver)
    {
        ArgumentNullException.ThrowIfNull(textureResolver);

        if (_body is null)
            return;

        HashSet<string> currentAttachmentIds = [];

        foreach (CelestialBodyAttachment attachment in _body.Attachments)
        {
            currentAttachmentIds.Add(attachment.Id);

            if (!_attachmentRenderers.TryGetValue(
                    attachment.Id,
                    out IAttachmentRenderer? renderer))
            {
                renderer = AttachmentRendererFactory.Create(attachment);

                _attachmentRenderers.Add(attachment.Id, renderer);
            }
            else if (renderer.AttachmentType != attachment.Type)
            {
                _attachmentRoot.Children.Remove(renderer.Root);

                renderer.ResetAnimation();

                renderer = AttachmentRendererFactory.Create(attachment);

                _attachmentRenderers[attachment.Id] = renderer;
            }

            BitmapSource? texture = textureResolver(attachment);

            renderer.Update(attachment, texture, _body.Radius);

            bool isInScene =
                _attachmentRoot.Children.Contains(renderer.Root);

            if (renderer.IsEnabled && !isInScene)
            {
                _attachmentRoot.Children.Add(renderer.Root);
            }
            else if (!renderer.IsEnabled && isInScene)
            {
                _attachmentRoot.Children.Remove(renderer.Root);
            }
        }

        string[] removedAttachmentIds = _attachmentRenderers.Keys
            .Where(id => !currentAttachmentIds.Contains(id))
            .ToArray();

        foreach (string attachmentId in removedAttachmentIds)
        {
            IAttachmentRenderer renderer =
                _attachmentRenderers[attachmentId];

            _attachmentRoot.Children.Remove(renderer.Root);

            renderer.ResetAnimation();

            _attachmentRenderers.Remove(attachmentId);
        }
    }

    private void RefreshAnimationRenderers()
    {
        if (_body is null)
            return;

        HashSet<string> activeIds = _body.Animations
            .Select(animation => animation.Id)
            .ToHashSet();

        foreach (string id in _animationRenderers.Keys.ToArray())
        {
            if (!activeIds.Contains(id))
            {
                _animationRenderers[id].ResetAnimation();
                _animationRenderers.Remove(id);
            }
        }

        foreach (CelestialBodyAnimation animation in _body.Animations)
        {
            if (!_animationRenderers.TryGetValue(
                    animation.Id,
                    out ICelestialBodyAnimationRenderer? renderer))
            {
                renderer = AnimationRendererFactory.Create(animation);
                _animationRenderers.Add(animation.Id, renderer);
            }

            renderer.Update(animation, this);
        }
    }

    public void AdvanceAnimation(WorldGlobeAnimationFrame frame)
    {
        if (_body is null || frame.DeltaSeconds <= 0.0)
            return;

        foreach (ICelestialBodyAnimationRenderer renderer
                 in _animationRenderers.Values)
        {
            renderer.AdvanceAnimation(frame);
        }

        foreach (IAttachmentRenderer renderer in _attachmentRenderers.Values)
        {
            renderer.AdvanceAnimation(frame);
        }
    }

    public void ResetAnimation()
    {
        foreach (ICelestialBodyAnimationRenderer renderer
                 in _animationRenderers.Values)
        {
            renderer.ResetAnimation();
        }

        foreach (IAttachmentRenderer renderer in _attachmentRenderers.Values)
        {
            renderer.ResetAnimation();
        }
    }

    private static MeshGeometry3D CreateSphereMesh(
        double radius,
        int thetaDiv,
        int phiDiv)
    {
        MeshGeometry3D mesh = new();

        for (int latitude = 0; latitude <= phiDiv; latitude++)
        {
            double phi = Math.PI * latitude / phiDiv;

            double y = Math.Cos(phi);
            double ringRadius = Math.Sin(phi);

            for (int longitude = 0; longitude <= thetaDiv; longitude++)
            {
                double theta =
                    2.0 * Math.PI * longitude / thetaDiv;

                double x = ringRadius * Math.Cos(theta);
                double z = ringRadius * Math.Sin(theta);

                mesh.Positions.Add(new Point3D(
                    radius * x,
                    radius * y,
                    radius * z));

                mesh.Normals.Add(new Vector3D(x, y, z));

                mesh.TextureCoordinates.Add(new Point(
                    (double)longitude / thetaDiv,
                    (double)latitude / phiDiv));
            }
        }

        for (int latitude = 0; latitude < phiDiv; latitude++)
        {
            for (int longitude = 0; longitude < thetaDiv; longitude++)
            {
                int current =
                    latitude * (thetaDiv + 1) + longitude;

                int next = current + thetaDiv + 1;

                mesh.TriangleIndices.Add(current);
                mesh.TriangleIndices.Add(next);
                mesh.TriangleIndices.Add(current + 1);

                mesh.TriangleIndices.Add(current + 1);
                mesh.TriangleIndices.Add(next);
                mesh.TriangleIndices.Add(next + 1);
            }
        }

        return mesh;
    }
}
