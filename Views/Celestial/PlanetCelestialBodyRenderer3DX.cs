
using HelixToolkit;
using HelixToolkit.Maths;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using System.Numerics;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;
using MeshGeometry3D = HelixToolkit.SharpDX.MeshGeometry3D;

namespace RealmStudioX.WPF.Views.Celestial;

internal sealed class PlanetCelestialBodyRenderer3DX :
    ICelestialBodyRenderer3DX,
    ICelestialAnimatedObject,
    IAttachmentContainer
{
    public CelestialBodyRendererType Type =>
        CelestialBodyRendererType.Planet;

    private static readonly MeshGeometry3D SphereMesh = CreateSphereMesh();
    internal static MeshGeometry3D SharedSphereMesh => SphereMesh;

    private readonly GroupModel3D _root = new();
    private readonly GroupModel3D _tiltGroup = new();
    private readonly GroupModel3D _surfaceGroup = new();
    private readonly GroupModel3D _activityGroup = new();
    private readonly GroupModel3D _attachmentRoot = new();

    private readonly MeshGeometryModel3D _surfaceModel = new();

    private readonly ScaleTransform3D _scaleTransform =
        new(1.0, 1.0, 1.0);

    private readonly ScaleTransform3D _activityScaleTransform =
        new(1.0, 1.0, 1.0);

    private readonly AxisAngleRotation3D _axialTiltRotation =
        new(new Vector3D(1.0, 0.0, 0.0), 0.0);

    private readonly Transform3DGroup _animationTransform = new();

    private const double SecondsPerRevolution = 60.0;

    private readonly AxisAngleRotation3D _surfaceRotation =
        new(new Vector3D(0, 1, 0), 0);

    private readonly RotateTransform3D _surfaceRotationTransform;

    private BasicRotationSettings? _rotationSettings;
    private double _rotationAngle = 180.0;
    private bool _rotationEnabled;

    private readonly Dictionary<string, CloudLayerAttachmentRenderer3DX> _cloudLayerRenderers = [];
    private readonly Dictionary<string, PlanetaryRingsAttachmentRenderer3DX> _planetaryRingRenderers = [];

    private readonly Func<CelestialTextureSource?, BitmapSource?> _textureResolver;

    private readonly Func<BitmapSource, TextureModel> _textureModelFactory;

    private CelestialBody? _body;
    private CelestialBodyNode? _node;

    public Element3D Root => _root;

    public PlanetCelestialBodyRenderer3DX(
        Func<CelestialTextureSource?, BitmapSource?> textureResolver,
        Func<BitmapSource, TextureModel> textureModelFactory)
    {
        _textureResolver = textureResolver
            ?? throw new ArgumentNullException(nameof(textureResolver));

        _textureModelFactory = textureModelFactory
            ?? throw new ArgumentNullException(nameof(textureModelFactory));

        // Axial tilt applies to the complete planetary visual hierarchy.
        _tiltGroup.Transform =
            new RotateTransform3D(_axialTiltRotation);

        // Surface scaling and animation are independent of the
        // axial tilt applied by the parent group.
        Transform3DGroup surfaceTransform = new();

        surfaceTransform.Children.Add(_scaleTransform);
        surfaceTransform.Children.Add(_animationTransform);

        _surfaceRotationTransform =
            new RotateTransform3D(_surfaceRotation);

        _animationTransform.Children.Add(
            _surfaceRotationTransform);

        _surfaceGroup.Transform = surfaceTransform;

        // Activity visuals receive placement scaling but do not
        // inherit surface rotation.
        _activityGroup.Transform = _activityScaleTransform;

        _surfaceModel.Geometry = SphereMesh;

        _surfaceGroup.Children.Add(_surfaceModel);

        _tiltGroup.Children.Add(_surfaceGroup);
        _tiltGroup.Children.Add(_activityGroup);
        _tiltGroup.Children.Add(_attachmentRoot);

        _root.Children.Add(_tiltGroup);
    }

    public void Update(
        CelestialBodyNode node,
        CelestialTextureSource? textureSource)
    {
        UpdatePlacement(node);

        UpdateRotationAnimation();

        UpdateSurfaceMaterial(textureSource);
    }

    private void UpdateRotationAnimation()
    {
        CelestialBodyAnimation? animation =
            _body?.Animations.FirstOrDefault(item =>
                item.AnimationType ==
                CelestialBodyAnimationType.BasicRotation);

        if (animation is null)
        {
            _rotationSettings = null;
            _rotationEnabled = false;
            ResetAnimation();
            return;
        }

        _rotationSettings =
            animation.Settings as BasicRotationSettings
            ?? throw new InvalidOperationException(
                "BasicRotation animation requires BasicRotationSettings.");

        _rotationEnabled = animation.Enabled;

        if (!_rotationEnabled)
            ResetAnimation();
    }

    public void UpdatePlacement(CelestialBodyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(node.Body);
        ArgumentNullException.ThrowIfNull(node.Placement);

        _node = node;
        _body = node.Body;

        double size = node.Placement.Size;

        _axialTiltRotation.Angle = node.Placement.AxialTilt;

        _scaleTransform.ScaleX = size;
        _scaleTransform.ScaleY = size;
        _scaleTransform.ScaleZ = size;

        _activityScaleTransform.ScaleX = size;
        _activityScaleTransform.ScaleY = size;
        _activityScaleTransform.ScaleZ = size;
    }

    private void UpdateSurfaceMaterial2(CelestialTextureSource? textureSource)
    {
        if (_body is null)
            return;

        BitmapSource? texture = _textureResolver(textureSource);

        TextureModel? textureModel = texture is not null
            ? _textureModelFactory(texture)
            : null;

        bool hasTexture = textureModel is not null;

        PhongMaterial material = new()
        {
            // No diffuse contribution.
            DiffuseColor = hasTexture
                ? new Color4(0.0f, 0.0f, 0.0f, 1.0f)
                : ToColor4(_body.BaseColor),

            // No ambient contribution.
            //AmbientColor = new Color4(0.5f, 0.5f, 0.5f, 1.0f),

            // The emissive map should reproduce the bitmap's colors.
            EmissiveColor = hasTexture
                ? new Color4(0.75f, 0.75f, 0.75f, 1.0f)
                : new Color4(0.0f, 0.0f, 0.0f, 1.0f),

            RenderEmissiveMap = hasTexture,

            // No specular contribution.
            SpecularColor = new Color4(0.0f, 0.0f, 0.0f, 1.0f),
            SpecularShininess = 20.0f
        };

        if (textureModel is not null)
        {
            // Assign ONLY the emissive map.
            material.EmissiveMap = textureModel;
        }

        _surfaceModel.Material = material;
    }

    private void UpdateSurfaceMaterial3(CelestialTextureSource? textureSource)
    {
        if (_body is null)
            return;

        BitmapSource? texture = _textureResolver(textureSource);

        TextureModel? textureModel = texture is not null
            ? _textureModelFactory(texture)
            : null;

        bool hasTexture = textureModel is not null;

        PhongMaterial material = new()
        {
            // No diffuse contribution.
            DiffuseColor = new Color4(1f, 1f, 1f, 1.0f),

            // Ambient contribution.
            AmbientColor = new Color4(0.8f, 0.8f, 0.8f, 1.0f),


            //RenderEmissiveMap = hasTexture,

            // No specular contribution.
            SpecularColor = new Color4(0.0f, 0.0f, 0.0f, 1.0f),
            SpecularShininess = 20.0f
        };

        if (textureModel is not null)
        {
            // Assign ONLY the diffuse map.
            material.DiffuseMap = textureModel;
            material.RenderDiffuseMap = true;
        }

        _surfaceModel.Material = material;
    }

    private void UpdateSurfaceMaterial(CelestialTextureSource? textureSource)
    {
        if (_body is null)
            return;

        BitmapSource? texture = _textureResolver(textureSource);

        TextureModel? textureModel = texture is not null
            ? _textureModelFactory(texture)
            : null;

        bool hasTexture = textureModel is not null;

        PBRMaterial material = new()
        {
            // Base surface color. White allows the texture's original
            // colors to pass through without color tinting.
            AlbedoColor = new Color4(0.25f, 0.25f, 0.25f, 1f),

            // A non-metallic, relatively matte planetary surface.
            MetallicFactor = 0.0,
            RoughnessFactor = 0.0
        };

        if (textureModel is not null)
        {
            material.AlbedoMap = textureModel;
            material.RenderAlbedoMap = true;

            material.EmissiveMap = textureModel;
            material.RenderEmissiveMap = true;
            material.EmissiveColor = new Color4(1f, 1f, 1f, 0f);
        }

        _surfaceModel.Material = material;
    }

    private static Color4 ToColor4(Color color)
    {
        return new Color4(
            color.ScR,
            color.ScG,
            color.ScB,
            color.ScA);
    }

    #region Attachments



    public void RefreshAttachments(
        Func<CelestialBodyAttachment, BitmapSource?> textureResolver)
    {
        ArgumentNullException.ThrowIfNull(textureResolver);

        if (_body is null || _node is null)
            return;

        HashSet<string> activeCloudIds =
        [
            .. _body.Attachments
        .Where(attachment =>
            attachment.Type == CelestialBodyAttachmentType.CloudLayer)
        .Select(attachment => attachment.Id)
        ];

        HashSet<string> activeRingIds =
        [
            .. _body.Attachments
        .Where(attachment =>
            attachment.Type == CelestialBodyAttachmentType.PlanetaryRings)
        .Select(attachment => attachment.Id)
        ];

        // Remove cloud renderers whose attachments no longer exist.
        foreach (string id in _cloudLayerRenderers.Keys.ToArray())
        {
            if (activeCloudIds.Contains(id))
                continue;

            CloudLayerAttachmentRenderer3DX renderer =
                _cloudLayerRenderers[id];

            renderer.ResetAnimation();
            _attachmentRoot.Children.Remove(renderer.Root);
            _cloudLayerRenderers.Remove(id);
        }

        // Remove ring renderers whose attachments no longer exist.
        foreach (string id in _planetaryRingRenderers.Keys.ToArray())
        {
            if (activeRingIds.Contains(id))
                continue;

            PlanetaryRingsAttachmentRenderer3DX renderer =
                _planetaryRingRenderers[id];

            renderer.ResetAnimation();
            _attachmentRoot.Children.Remove(renderer.Root);
            _planetaryRingRenderers.Remove(id);
        }

        // Update the remaining attachments.
        foreach (CelestialBodyAttachment attachment in _body.Attachments)
        {
            switch (attachment.Type)
            {
                case CelestialBodyAttachmentType.CloudLayer:
                    {
                        if (!_cloudLayerRenderers.TryGetValue(
                                attachment.Id,
                                out CloudLayerAttachmentRenderer3DX? renderer))
                        {
                            renderer = new CloudLayerAttachmentRenderer3DX(
                                SphereMesh,
                                _textureModelFactory);

                            _cloudLayerRenderers.Add(attachment.Id, renderer);
                        }

                        BitmapSource? texture = textureResolver(attachment);

                        renderer.Update(
                            attachment,
                            texture,
                            _node.Placement.Size);

                        if (renderer.IsEnabled)
                        {
                            if (!_attachmentRoot.Children.Contains(renderer.Root))
                                _attachmentRoot.Children.Add(renderer.Root);
                        }
                        else
                        {
                            _attachmentRoot.Children.Remove(renderer.Root);
                        }

                        break;
                    }

                case CelestialBodyAttachmentType.PlanetaryRings:
                    {
                        if (!_planetaryRingRenderers.TryGetValue(
                                attachment.Id,
                                out PlanetaryRingsAttachmentRenderer3DX? renderer))
                        {
                            renderer = new PlanetaryRingsAttachmentRenderer3DX(
                                _textureModelFactory);

                            _planetaryRingRenderers.Add(attachment.Id, renderer);
                        }

                        BitmapSource? texture = textureResolver(attachment);

                        renderer.Update(
                            attachment,
                            texture,
                            _node.Placement.Size);

                        if (renderer.IsEnabled)
                        {
                            if (!_attachmentRoot.Children.Contains(renderer.Root))
                                _attachmentRoot.Children.Add(renderer.Root);
                        }
                        else
                        {
                            _attachmentRoot.Children.Remove(renderer.Root);
                        }

                        break;
                    }
            }
        }
    }



    #endregion

    #region Animation

    public void AdvanceAnimation(CelestialAnimationFrame frame)
    {
        if (frame.DeltaSeconds <= 0.0)
            return;

        // Advance the planet's surface rotation independently.
        if (_rotationEnabled && _rotationSettings is not null)
        {
            double rotationRate =
                double.IsFinite(_rotationSettings.RotationRate)
                    ? _rotationSettings.RotationRate
                    : 1.0;

            double degreesPerSecond =
                360.0 / SecondsPerRevolution * rotationRate;

            if (_rotationSettings.RotationDirection ==
                WorldRotationDirection.Retrograde)
            {
                degreesPerSecond = -degreesPerSecond;
            }

            _rotationAngle =
                (_rotationAngle +
                 degreesPerSecond * frame.DeltaSeconds) % 360.0;

            _surfaceRotation.Angle = _rotationAngle;
        }

        // Cloud rotation has its own animation transform.
        foreach (CloudLayerAttachmentRenderer3DX renderer in _cloudLayerRenderers.Values)
        {
            renderer.AdvanceAnimation(frame);
        }

        foreach (PlanetaryRingsAttachmentRenderer3DX renderer in _planetaryRingRenderers.Values)
        {
            renderer.AdvanceAnimation(frame);
        }
    }

    public void ResetAnimation()
    {
        // Preserve the surface's existing initial orientation.
        _rotationAngle = 180.0;
        _surfaceRotation.Angle = 180.0;

        foreach (CloudLayerAttachmentRenderer3DX renderer
                 in _cloudLayerRenderers.Values)
        {
            renderer.ResetAnimation();
        }
    }

    #endregion

    #region Geometry


    private static MeshGeometry3D CreateSphereMesh(
        double radius = 1.0,
        int longitudeSegments = 128,
        int latitudeSegments = 64)
    {
        MeshGeometry3D mesh = new()
        {
            Positions = new Vector3Collection(),
            Normals = new Vector3Collection(),
            TextureCoordinates = new Vector2Collection(),
            Indices = new IntCollection()
        };

        int rowLength = longitudeSegments + 1;

        for (int latitude = 0; latitude <= latitudeSegments; latitude++)
        {
            double v = (double)latitude / latitudeSegments;
            double phi = Math.PI / 2.0 - Math.PI * v;

            double cosPhi = Math.Cos(phi);
            double y = Math.Sin(phi);

            for (int longitude = 0; longitude <= longitudeSegments; longitude++)
            {
                double u = (double)longitude / longitudeSegments;
                double theta = 2.0 * Math.PI * u;

                double x = cosPhi * Math.Cos(theta);
                double z = cosPhi * Math.Sin(theta);

                mesh.Positions.Add(new Vector3(
                    (float)(radius * x),
                    (float)(radius * y),
                    (float)(radius * z)));

                mesh.Normals.Add(new Vector3(
                    (float)x,
                    (float)y,
                    (float)z));

                mesh.TextureCoordinates.Add(new Vector2(
                    (float)(1.0 - u),
                    (float)v));
            }
        }

        for (int latitude = 0; latitude < latitudeSegments; latitude++)
        {
            for (int longitude = 0; longitude < longitudeSegments; longitude++)
            {
                int a = latitude * rowLength + longitude;
                int b = a + 1;
                int c = a + rowLength;
                int d = c + 1;

                mesh.Indices.Add(a);
                mesh.Indices.Add(b);
                mesh.Indices.Add(c);

                mesh.Indices.Add(b);
                mesh.Indices.Add(d);
                mesh.Indices.Add(c);
            }
        }

        return mesh;
    }


    #endregion
}
