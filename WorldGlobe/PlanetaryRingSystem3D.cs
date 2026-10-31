using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.WorldGlobe;

internal sealed class PlanetaryRingSystem3D
{
    private const int AngularSegments = 360;
    private const int RadialSegments = 64;

    // One revolution every 240 seconds at rotation rate 1.0.
    private const double SecondsPerRevolution = 240.0;

    private readonly QuaternionRotation3D _rotation = new(Quaternion.Identity);

    private readonly GeometryModel3D _ringModel = new();

    private PlanetaryRingSettings _settings;

    private double _rotationAngle;

    private string _loadedTexturePath = string.Empty;

    public Model3DGroup Root { get; } = new();

    public PlanetaryRingSystem3D(
        BitmapSource texture,
        PlanetaryRingSettings settings)
    {
        _settings = settings.Clone();

        UpdateTexture(texture);

        Root.Transform = new RotateTransform3D(_rotation);
        Root.Children.Add(_ringModel);

        UpdateRingGeometry();
    }

    public void UpdateSettings(
        PlanetaryRingSettings settings,
        BitmapSource? texture = null)
    {
        bool geometryChanged =
            _settings.InnerRadiusScale != settings.InnerRadiusScale ||
            _settings.OuterRadiusScale != settings.OuterRadiusScale ||
            _settings.Inclination != settings.Inclination;

        _settings = settings.Clone();

        if (texture is not null)
            UpdateTexture(texture);

        if (geometryChanged)
            UpdateRingGeometry();

        if (!_settings.EnableRotation)
            ResetAnimation();
    }

    private void UpdateTexture(BitmapSource texture)
    {
        ImageBrush textureBrush = new(texture)
        {
            Stretch = Stretch.Fill
        };

        textureBrush.Freeze();

        DiffuseMaterial material = new(textureBrush);
        material.Freeze();

        _ringModel.Material = material;
        _ringModel.BackMaterial = material;
    }

    public bool IsTextureChanged(string? texturePath)
    {
        return !string.Equals(
            _loadedTexturePath,
            texturePath ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateRingGeometry()
    {
        _ringModel.Geometry = CreateRingMesh(_settings.InnerRadiusScale, _settings.OuterRadiusScale);

        // Inclination is independent of the globe's axial tilt.
        Root.Transform = new RotateTransform3D(
            new AxisAngleRotation3D(
                new Vector3D(1, 0, 0),
                _settings.Inclination));

        // Keep the rotation transform as a separate transform.
        Root.Transform = new Transform3DGroup
        {
            Children =
            {
                new RotateTransform3D(
                    new AxisAngleRotation3D(
                        new Vector3D(1, 0, 0),
                        _settings.Inclination)),
                new RotateTransform3D(_rotation)
            }
        };
    }

    private static MeshGeometry3D CreateRingMesh(
        double innerRadius,
        double outerRadius)
    {
        MeshGeometry3D mesh = new();

        Point3DCollection positions = new();
        Vector3DCollection normals = new();
        PointCollection textureCoordinates = new();
        Int32Collection triangleIndices = new();

        int rowLength = AngularSegments + 1;

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

                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);

                positions.Add(new Point3D(x, 0, z));
                normals.Add(new Vector3D(0, 1, 0));

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
        if (!_settings.EnableRotation || frame.DeltaSeconds <= 0.0)
            return;

        double degreesPerSecond =
            360.0 / SecondsPerRevolution *
            _settings.RotationRate;

        if (_settings.RotationDirection ==
            RealmStudioShapeRenderingLib.WorldRotationDirection.Retrograde)
        {
            degreesPerSecond = -degreesPerSecond;
        }

        _rotationAngle += degreesPerSecond * frame.DeltaSeconds;
        _rotationAngle %= 360.0;

        _rotation.Quaternion = new Quaternion(
            new Vector3D(0, 1, 0),
            _rotationAngle);
    }

    public void ResetAnimation()
    {
        _rotationAngle = 0.0;
        _rotation.Quaternion = Quaternion.Identity;
    }
}