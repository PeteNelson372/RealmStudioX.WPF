
using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.WorldGlobe;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Celestial;

internal sealed class PlanetaryRingsAttachmentRenderer :
    IAttachmentRenderer,
    ICelestialBodyAnimationTarget,
    IRingTurbulenceAnimationTarget
{
    private const int AngularSegments = 360;
    private const int RadialSegments = 64;

    private readonly Model3DGroup _root = new();
    private readonly GeometryModel3D _ringModel = new();

    private readonly Transform3DGroup _animationTransform = new();

    private readonly Dictionary<string, ICelestialBodyAnimationRenderer>
        _animationRenderers = [];

    private PlanetaryRingSettings? _settings;

    private BitmapSource? _sourceTexture;

    private double _hostRadius;

    private bool _hasGeometry;

    public string AttachmentId { get; private set; } = string.Empty;

    public CelestialBodyAttachmentType AttachmentType =>
        CelestialBodyAttachmentType.PlanetaryRings;

    public Model3DGroup Root => _root;

    public bool IsEnabled { get; private set; }

    public Transform3DGroup AnimationTransform =>
        _animationTransform;

    public BitmapSource SourceTexture =>
        _sourceTexture
        ?? throw new InvalidOperationException(
            "The ring texture has not been initialized.");

    public PlanetaryRingsAttachmentRenderer()
    {
        // Do not add the ring model here.
        // It is added only after a valid texture is available
        // and the attachment has been enabled.
    }

    public void Update(
        CelestialBodyAttachment attachment,
        BitmapSource? texture,
        double hostRadius)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        AttachmentId = attachment.Id;

        PlanetaryRingSettings settings = GetSettings(attachment);

        bool geometryChanged =
            !_hasGeometry ||
            _settings is null ||
            _settings.InnerRadiusScale != settings.InnerRadiusScale ||
            _settings.OuterRadiusScale != settings.OuterRadiusScale ||
            _settings.Inclination != settings.Inclination ||
            _hostRadius != hostRadius;

        _settings = settings.Clone();
        _hostRadius = hostRadius;

        // Both the attachment and its settings must be enabled,
        // and a valid texture must be supplied.
        bool shouldEnable =
            attachment.Enabled &&
            settings.Enabled &&
            texture is not null;

        if (!shouldEnable)
        {
            DisableRings();
            return;
        }

        bool textureChanged =
            !ReferenceEquals(_sourceTexture, texture);

        _sourceTexture = texture;

        if (textureChanged)
            UpdateTextureMaterial();

        if (geometryChanged)
            UpdateRingGeometry();

        UpdateRootTransform();

        if (!_root.Children.Contains(_ringModel))
            _root.Children.Add(_ringModel);

        IsEnabled = true;

        RefreshAnimationRenderers(attachment.Animations);
    }

    private static PlanetaryRingSettings GetSettings(
        CelestialBodyAttachment attachment)
    {
        return attachment.AttachmentSettings as PlanetaryRingSettings
            ?? throw new InvalidOperationException(
                $"The planetary rings attachment '{attachment.Name}' " +
                "does not contain valid PlanetaryRingSettings.");
    }

    private void DisableRings()
    {
        // Reset animation renderers BEFORE clearing the source texture.
        // Some renderers restore the original texture during reset.
        ClearAnimationRenderers();

        IsEnabled = false;

        _root.Children.Remove(_ringModel);

        _ringModel.Geometry = null;
        _ringModel.Material = null;
        _ringModel.BackMaterial = null;

        _hasGeometry = false;

        _sourceTexture = null;

        // Remove any transforms left by animation renderers.
        _animationTransform.Children.Clear();
    }

    private void ClearAnimationRenderers()
    {
        foreach (ICelestialBodyAnimationRenderer renderer
                 in _animationRenderers.Values)
        {
            renderer.ResetAnimation();
        }

        _animationRenderers.Clear();
    }

    private void RefreshAnimationRenderers(
        ObservableCollection<CelestialBodyAnimation> animations)
    {
        HashSet<string> activeIds =
            animations.Select(animation => animation.Id).ToHashSet();

        foreach (string id in _animationRenderers.Keys.ToArray())
        {
            if (!activeIds.Contains(id))
            {
                _animationRenderers[id].ResetAnimation();
                _animationRenderers.Remove(id);
            }
        }

        foreach (CelestialBodyAnimation animation in animations)
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

    private void UpdateTextureMaterial()
    {
        if (_sourceTexture is null)
            return;

        ImageBrush brush = new(_sourceTexture)
        {
            Stretch = Stretch.Fill
        };

        DiffuseMaterial material = new(brush);

        if (_sourceTexture.IsFrozen && brush.CanFreeze)
        {
            brush.Freeze();

            if (material.CanFreeze)
                material.Freeze();
        }

        _ringModel.Material = material;
        _ringModel.BackMaterial = material;
    }

    public void SetAnimatedTexture(BitmapSource? texture)
    {
        // A null texture means restore the original ring texture.
        BitmapSource? materialTexture = texture ?? _sourceTexture;

        if (materialTexture is null)
            return;

        ImageBrush brush = new(materialTexture)
        {
            Stretch = Stretch.Fill
        };

        DiffuseMaterial material = new(brush);

        // The turbulence texture is a live WriteableBitmap.
        // Never freeze its brush or material.
        if (texture is null && brush.CanFreeze)
        {
            brush.Freeze();

            if (material.CanFreeze)
                material.Freeze();
        }

        _ringModel.Material = material;
        _ringModel.BackMaterial = material;
    }

    private void UpdateRingGeometry()
    {
        if (_settings is null || _sourceTexture is null)
            return;

        double innerRadius =
            _settings.InnerRadiusScale * _hostRadius;

        double outerRadius =
            _settings.OuterRadiusScale * _hostRadius;

        _ringModel.Geometry =
            CreateRingMesh(innerRadius, outerRadius);

        _hasGeometry = true;
    }

    private void UpdateRootTransform()
    {
        Transform3DGroup transform = new();

        if (_settings is not null)
        {
            transform.Children.Add(
                new RotateTransform3D(
                    new AxisAngleRotation3D(
                        new Vector3D(1, 0, 0),
                        _settings.Inclination)));
        }

        transform.Children.Add(_animationTransform);

        _root.Transform = transform;
    }

    private static MeshGeometry3D CreateRingMesh(
        double innerRadius,
        double outerRadius)
    {
        MeshGeometry3D mesh = new();

        Point3DCollection positions = [];
        Vector3DCollection normals = [];
        PointCollection textureCoordinates = [];
        Int32Collection triangleIndices = [];

        int rowLength = AngularSegments + 1;

        for (int radial = 0; radial <= RadialSegments; radial++)
        {
            double radialFraction =
                (double)radial / RadialSegments;

            double radius =
                innerRadius +
                (outerRadius - innerRadius) * radialFraction;

            for (int angular = 0; angular <= AngularSegments; angular++)
            {
                double angularFraction =
                    (double)angular / AngularSegments;

                double angle =
                    angularFraction * Math.PI * 2.0;

                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);

                positions.Add(new Point3D(x, 0, z));
                normals.Add(new Vector3D(0, 1, 0));

                // U follows the radial direction.
                // V follows the circumference.
                textureCoordinates.Add(
                    new Point(radialFraction, angularFraction));
            }
        }

        for (int radial = 0; radial < RadialSegments; radial++)
        {
            for (int angular = 0; angular < AngularSegments; angular++)
            {
                int a = radial * rowLength + angular;
                int b = a + 1;
                int c = a + rowLength;
                int d = c + 1;

                triangleIndices.Add(a);
                triangleIndices.Add(b);
                triangleIndices.Add(c);

                triangleIndices.Add(b);
                triangleIndices.Add(d);
                triangleIndices.Add(c);
            }
        }

        mesh.Positions = positions;
        mesh.Normals = normals;
        mesh.TextureCoordinates = textureCoordinates;
        mesh.TriangleIndices = triangleIndices;

        if (mesh.CanFreeze)
            mesh.Freeze();

        return mesh;
    }

    public void AdvanceAnimation(WorldGlobeAnimationFrame frame)
    {
        if (!IsEnabled)
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
}
