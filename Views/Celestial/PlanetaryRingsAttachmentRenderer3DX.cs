
using HelixToolkit;
using HelixToolkit.Maths;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using RealmStudioX.Core;
using System.Numerics;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using DiffuseMaterial = HelixToolkit.Wpf.SharpDX.DiffuseMaterial;
using MeshGeometry3D = HelixToolkit.SharpDX.MeshGeometry3D;

namespace RealmStudioX.WPF.Views.Celestial;

internal sealed class PlanetaryRingsAttachmentRenderer3DX :
    ICelestialAnimatedObject,
    ICelestialBodyAnimationTarget,
    IRingTurbulenceAnimationTarget
{
    private const int AngularSegments = 360;
    private const int RadialSegments = 64;

    private readonly GroupModel3D _root = new();
    private readonly MeshGeometryModel3D _ringModel = new();

    private readonly Transform3DGroup _animationTransform = new();

    private readonly Dictionary<string, ICelestialBodyAnimationRenderer> _animationRenderers = [];

    private readonly Func<BitmapSource, TextureModel> _textureModelFactory;

    private PlanetaryRingSettings? _settings;
    private BitmapSource? _sourceTexture;
    private double _hostRadius;
    private bool _hasGeometry;

    public string AttachmentId { get; private set; } = string.Empty;

    public GroupModel3D Root => _root;

    public bool IsEnabled { get; private set; }

    public Transform3DGroup AnimationTransform => _animationTransform;

    public BitmapSource? SourceTexture => _sourceTexture;

    private const double RingThicknessScale = 0.01;

    public PlanetaryRingsAttachmentRenderer3DX(Func<BitmapSource, TextureModel> textureModelFactory)
    {
        _textureModelFactory = textureModelFactory
            ?? throw new ArgumentNullException(nameof(textureModelFactory));

        // Render both sides of the ring mesh.
        _ringModel.CullMode = SharpDX.Direct3D11.CullMode.None;

        _root.Children.Add(_ringModel);
    }

    public void Update(
        CelestialBodyAttachment attachment,
        BitmapSource? texture,
        double hostRadius)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        AttachmentId = attachment.Id;

        PlanetaryRingSettings settings =
            attachment.AttachmentSettings as PlanetaryRingSettings
            ?? throw new InvalidOperationException(
                $"The planetary rings attachment '{attachment.Name}' " +
                "does not contain valid PlanetaryRingSettings.");

        bool geometryChanged =
            !_hasGeometry ||
            _settings is null ||
            _settings.InnerRadiusScale != settings.InnerRadiusScale ||
            _settings.OuterRadiusScale != settings.OuterRadiusScale ||
            _settings.Inclination != settings.Inclination ||
            _hostRadius != hostRadius;

        _settings = settings.Clone();
        _hostRadius = hostRadius;

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

        IsEnabled = true;

        RefreshAnimationRenderers(attachment.Animations);
    }

    private void DisableRings()
    {
        ClearAnimationRenderers();

        IsEnabled = false;

        _ringModel.Geometry = null;
        _ringModel.Material = null;

        _hasGeometry = false;
        _sourceTexture = null;

        _animationTransform.Children.Clear();
    }

    private void UpdateTextureMaterial()
    {
        if (_sourceTexture is null)
            return;

        _ringModel.Material = CreateRingMaterial(_sourceTexture);
    }

    private DiffuseMaterial CreateRingMaterial(BitmapSource texture)
    {
        return new DiffuseMaterial
        {
            // White ensures the texture supplies the actual color.
            DiffuseColor = new Color4(1, 1, 1, 1),

            // Use the ring texture, including its alpha channel.
            DiffuseMap = _textureModelFactory(texture),

            // Do not let scene lighting alter the texture's appearance.
            EnableUnLit = true
        };
    }

    public void SetAnimatedTexture(BitmapSource? texture)
    {
        // A null texture restores the original ring texture.
        BitmapSource? materialTexture = texture ?? _sourceTexture;

        if (materialTexture is null)
            return;

        _ringModel.Material = CreateRingMaterial(materialTexture);
    }

    private void UpdateRingGeometry()
    {
        if (_settings is null || _sourceTexture is null)
            return;

        double innerRadius =
            _settings.InnerRadiusScale * _hostRadius;

        double outerRadius =
            _settings.OuterRadiusScale * _hostRadius;

        double thickness = _hostRadius * RingThicknessScale;

        _ringModel.Geometry =
            CreateRingMesh(innerRadius, outerRadius, thickness);

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
        double outerRadius,
        double thickness)
    {
        MeshGeometry3D mesh = new()
        {
            Positions = new Vector3Collection(),
            Normals = new Vector3Collection(),
            TextureCoordinates = new Vector2Collection(),
            Indices = new IntCollection()
        };

        double halfThickness = thickness * 0.5;

        int[,] top = new int[RadialSegments + 1, AngularSegments + 1];
        int[,] bottom = new int[RadialSegments + 1, AngularSegments + 1];

        int AddVertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            int index = mesh.Positions.Count;

            mesh.Positions.Add(position);
            mesh.Normals.Add(normal);
            mesh.TextureCoordinates.Add(uv);

            return index;
        }

        void AddTriangle(int a, int b, int c)
        {
            mesh.Indices.Add(a);
            mesh.Indices.Add(b);
            mesh.Indices.Add(c);
        }

        // Create the top and bottom annular surfaces.
        for (int radial = 0; radial <= RadialSegments; radial++)
        {
            double radialFraction = (double)radial / RadialSegments;

            double radius =
                innerRadius +
                (outerRadius - innerRadius) * radialFraction;

            for (int angular = 0; angular <= AngularSegments; angular++)
            {
                double angularFraction = (double)angular / AngularSegments;
                double angle = angularFraction * Math.PI * 2.0;

                float x = (float)(radius * Math.Cos(angle));
                float z = (float)(radius * Math.Sin(angle));

                Vector2 uv = new(
                    (float)radialFraction,
                    (float)angularFraction);

                top[radial, angular] = AddVertex(
                    new Vector3(x, (float)halfThickness, z),
                    new Vector3(0, 1, 0),
                    uv);

                bottom[radial, angular] = AddVertex(
                    new Vector3(x, (float)-halfThickness, z),
                    new Vector3(0, -1, 0),
                    uv);
            }
        }

        // Top and bottom faces.
        for (int radial = 0; radial < RadialSegments; radial++)
        {
            for (int angular = 0; angular < AngularSegments; angular++)
            {
                int a = top[radial, angular];
                int b = top[radial, angular + 1];
                int c = top[radial + 1, angular];
                int d = top[radial + 1, angular + 1];

                // Top face: outward normal +Y.
                AddTriangle(a, b, c);
                AddTriangle(b, d, c);

                a = bottom[radial, angular];
                b = bottom[radial, angular + 1];
                c = bottom[radial + 1, angular];
                d = bottom[radial + 1, angular + 1];

                // Bottom face: outward normal -Y.
                AddTriangle(a, c, b);
                AddTriangle(b, c, d);
            }
        }

        // Inner and outer cylindrical walls.
        for (int angular = 0; angular < AngularSegments; angular++)
        {
            double angleA = (double)angular / AngularSegments * Math.PI * 2.0;
            double angleB = (double)(angular + 1) / AngularSegments * Math.PI * 2.0;

            float cosA = (float)Math.Cos(angleA);
            float sinA = (float)Math.Sin(angleA);
            float cosB = (float)Math.Cos(angleB);
            float sinB = (float)Math.Sin(angleB);

            float uA = (float)angular / AngularSegments;
            float uB = (float)(angular + 1) / AngularSegments;

            // Outer wall.
            int outerTopA = AddVertex(
                new Vector3(
                    (float)(outerRadius * cosA),
                    (float)halfThickness,
                    (float)(outerRadius * sinA)),
                new Vector3(cosA, 0, sinA),
                new Vector2(uA, 0));

            int outerTopB = AddVertex(
                new Vector3(
                    (float)(outerRadius * cosB),
                    (float)halfThickness,
                    (float)(outerRadius * sinB)),
                new Vector3(cosB, 0, sinB),
                new Vector2(uB, 0));

            int outerBottomA = AddVertex(
                new Vector3(
                    (float)(outerRadius * cosA),
                    (float)-halfThickness,
                    (float)(outerRadius * sinA)),
                new Vector3(cosA, 0, sinA),
                new Vector2(uA, 1));

            int outerBottomB = AddVertex(
                new Vector3(
                    (float)(outerRadius * cosB),
                    (float)-halfThickness,
                    (float)(outerRadius * sinB)),
                new Vector3(cosB, 0, sinB),
                new Vector2(uB, 1));

            AddTriangle(outerTopA, outerTopB, outerBottomA);
            AddTriangle(outerTopB, outerBottomB, outerBottomA);

            // Inner wall.
            int innerTopA = AddVertex(
                new Vector3(
                    (float)(innerRadius * cosA),
                    (float)halfThickness,
                    (float)(innerRadius * sinA)),
                new Vector3(-cosA, 0, -sinA),
                new Vector2(uA, 0));

            int innerTopB = AddVertex(
                new Vector3(
                    (float)(innerRadius * cosB),
                    (float)halfThickness,
                    (float)(innerRadius * sinB)),
                new Vector3(-cosB, 0, -sinB),
                new Vector2(uB, 0));

            int innerBottomA = AddVertex(
                new Vector3(
                    (float)(innerRadius * cosA),
                    (float)-halfThickness,
                    (float)(innerRadius * sinA)),
                new Vector3(-cosA, 0, -sinA),
                new Vector2(uA, 1));

            int innerBottomB = AddVertex(
                new Vector3(
                    (float)(innerRadius * cosB),
                    (float)-halfThickness,
                    (float)(innerRadius * sinB)),
                new Vector3(-cosB, 0, -sinB),
                new Vector2(uB, 1));

            AddTriangle(innerTopA, innerBottomA, innerTopB);
            AddTriangle(innerTopB, innerBottomA, innerBottomB);
        }

        return mesh;
    }


    private void RefreshAnimationRenderers(
        IEnumerable<CelestialBodyAnimation> animations)
    {
        CelestialBodyAnimation[] activeAnimations = animations.ToArray();

        HashSet<string> activeIds =
            activeAnimations.Select(animation => animation.Id).ToHashSet();

        foreach (string id in _animationRenderers.Keys.ToArray())
        {
            if (activeIds.Contains(id))
                continue;

            _animationRenderers[id].ResetAnimation();
            _animationRenderers.Remove(id);
        }

        foreach (CelestialBodyAnimation animation in activeAnimations)
        {
            if (!_animationRenderers.TryGetValue(
                    animation.Id,
                    out ICelestialBodyAnimationRenderer? renderer))
            {
                renderer = AnimationRendererFactory.Create(animation);
                _animationRenderers.Add(animation.Id, renderer);
            }
            else if (renderer.AnimationType != animation.AnimationType)
            {
                renderer.ResetAnimation();
                renderer = AnimationRendererFactory.Create(animation);
                _animationRenderers[animation.Id] = renderer;
            }

            renderer.Update(animation, this);
        }
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

    public void AdvanceAnimation(CelestialAnimationFrame frame)
    {
        if (!IsEnabled || frame.DeltaSeconds <= 0)
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
