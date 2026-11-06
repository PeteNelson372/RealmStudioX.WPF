using RealmStudioShapeRenderingLib;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Celestial;

/// <summary>Renders and animates a cloud layer attached to a celestial body.</summary>
internal sealed class CloudLayerAttachmentRenderer : IAttachmentRenderer, ICelestialBodyAnimationTarget
{
    private static readonly MeshGeometry3D SphereMesh = CreateSphereMesh(1.0, 128, 64);

    private readonly Model3DGroup _root = new();
    private readonly GeometryModel3D _surfaceModel = new();

    private readonly ImageBrush _textureBrush = new()
    {
        Stretch = Stretch.Fill,
        Opacity = 1.0
    };

    private CelestialBodyAttachment? _attachment;

    public string AttachmentId { get; private set; } = string.Empty;

    public CelestialBodyAttachmentType AttachmentType => CelestialBodyAttachmentType.CloudLayer;

    public Model3DGroup Root => _root;

    private readonly Transform3DGroup _animationTransform = new();
    public Transform3DGroup AnimationTransform => _animationTransform;

    private readonly Dictionary<string, ICelestialBodyAnimationRenderer> _animationRenderers = [];

    public bool IsEnabled { get; private set; }

    public CloudLayerAttachmentRenderer()
    {
        _root.Transform = _animationTransform;

        DiffuseMaterial material = new(_textureBrush);

        _surfaceModel.Geometry = SphereMesh;
        _surfaceModel.Material = material;
        _surfaceModel.BackMaterial = material;

        _root.Children.Add(_surfaceModel);
    }

    private void RefreshAnimationRenderers()
    {
        if (_attachment is null)
            return;

        HashSet<string> activeIds = [.. _attachment.Animations.Select(animation => animation.Id)];

        foreach (string id in _animationRenderers.Keys.ToArray())
        {
            if (!activeIds.Contains(id))
            {
                _animationRenderers[id].ResetAnimation();
                _animationRenderers.Remove(id);
            }
        }

        foreach (CelestialBodyAnimation animation in _attachment.Animations)
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

    public void Update(
        CelestialBodyAttachment attachment,
        BitmapSource? texture,
        double hostRadius)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        _attachment = attachment;
        AttachmentId = attachment.Id;

        _textureBrush.ImageSource = texture;

        // Slightly larger than the host body's surface to prevent z-fighting.
        const double CloudLayerRadiusScale = 1.003;

        double radius = Math.Max(0.001, hostRadius) * CloudLayerRadiusScale;

        _surfaceModel.Transform = new ScaleTransform3D(
            radius,
            radius,
            radius);

        IsEnabled = attachment.Enabled && texture is not null;

        RefreshAnimationRenderers();
    }

    public void AdvanceAnimation(CelestialAnimationFrame frame)
    {
        if (!IsEnabled || frame.DeltaSeconds <= 0.0)
            return;

        foreach (ICelestialBodyAnimationRenderer renderer
                 in _animationRenderers.Values)
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
    }

    private static MeshGeometry3D CreateSphereMesh(
        double radius,
        int longitudeSegments,
        int latitudeSegments)
    {
        MeshGeometry3D mesh = new();

        for (int latitude = 0; latitude <= latitudeSegments; latitude++)
        {
            double v = (double)latitude / latitudeSegments;
            double phi = Math.PI * v;
            double y = Math.Cos(phi);
            double ringRadius = Math.Sin(phi);

            for (int longitude = 0; longitude <= longitudeSegments; longitude++)
            {
                double u = (double)longitude / longitudeSegments;
                double theta = 2.0 * Math.PI * u;

                double x = ringRadius * Math.Cos(theta);
                double z = ringRadius * Math.Sin(theta);

                mesh.Positions.Add(
                    new Point3D(radius * x, radius * y, radius * z));

                mesh.Normals.Add(new Vector3D(x, y, z));
                mesh.TextureCoordinates.Add(new Point(u, v));
            }
        }

        for (int latitude = 0; latitude < latitudeSegments; latitude++)
        {
            for (int longitude = 0; longitude < longitudeSegments; longitude++)
            {
                int current =
                    latitude * (longitudeSegments + 1) + longitude;

                int next = current + longitudeSegments + 1;

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