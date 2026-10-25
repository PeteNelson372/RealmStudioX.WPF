using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.EditorUtilities;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.WorldGlobe;

internal sealed class SunLocalStarSceneObject : ILocalStarSceneObject, IWorldGlobeAnimatedObject
{
    private static readonly Lazy<BitmapSource> SunTexture = new(() => WorldGlobeAssets.LoadBitmap("sol-texture.jpg"));

    private readonly MeshGeometry3D _unitSphereMesh;

    private readonly Model3DGroup _positionGroup = new();

    private readonly GeometryModel3D _sunSurface;
    private readonly GeometryModel3D _corona;
    private readonly PointLight _light;

    private readonly EmissiveMaterial _sunMaterial;
    private readonly SolidColorBrush _coronaBrush;

    public Model3DGroup Root { get; } = new();

    public LocalStarType Type => LocalStarType.Sun;

    private const double SurfaceSecondsPerRevolution = 60.0;

    private double _surfaceAnimationAngle;
    private double _surfaceRadius = 0.15;
    private double _rotationRate = 1.0;

    private WorldRotationDirection _rotationDirection = WorldRotationDirection.Prograde;

    private Quaternion _baseSurfaceRotation = Quaternion.Identity;

    public SunLocalStarSceneObject(LocalStar star, MeshGeometry3D unitSphereMesh)
    {
        _unitSphereMesh = unitSphereMesh;

        Root.Transform = star.LocationTransform;
        Root.Children.Add(_positionGroup);

        // Visible, self-illuminated Sun.
        ImageBrush sunBrush = new(SunTexture.Value)
        {
            Stretch = Stretch.Fill,
            Opacity = 1.0
        };

        sunBrush.Freeze();

        _sunMaterial = new EmissiveMaterial(sunBrush)
        {
            Color = star.StarColor
        };

        _sunSurface = new GeometryModel3D
        {
            Geometry = _unitSphereMesh,
            Material = _sunMaterial,
            Transform = CreateStarSurfaceTransform(
                star.Radius,
                star.RotationQuaternion)
        };

        // Simple colored corona.
        _coronaBrush = new SolidColorBrush(star.StarColor)
        {
            Opacity = 0.12
        };

        EmissiveMaterial coronaMaterial =
            new(_coronaBrush);

        _corona = new GeometryModel3D
        {
            Geometry = _unitSphereMesh,
            Material = coronaMaterial,
            Transform = CreateStarSurfaceTransform(
                star.Radius * 1.12,
                star.RotationQuaternion)
        };

        // Point light at the Sun's local origin.
        _light = new PointLight
        {
            Position = new Point3D(0, 0, 0),
            Color = ScaleLightColor(
                star.StarColor,
                star.LightIntensity),
            Range = GetLightRange(star),
            ConstantAttenuation = 1.0,
            LinearAttenuation = 0.0,
            QuadraticAttenuation = 0.02
        };

        _positionGroup.Children.Add(_corona);
        _positionGroup.Children.Add(_sunSurface);
        _positionGroup.Children.Add(_light);

        Update(star);
    }

    public void Update(LocalStar star)
    {
        Root.Transform = star.LocationTransform;

        _positionGroup.Transform = new TranslateTransform3D(
            star.Center.X,
            star.Center.Y,
            star.Center.Z);

        _sunSurface.Transform = CreateStarSurfaceTransform(
            star.Radius,
            star.RotationQuaternion);

        _sunMaterial.Color = star.StarColor;

        _corona.Transform = CreateStarSurfaceTransform(
            star.Radius * 1.12,
            star.RotationQuaternion);

        _coronaBrush.Color = star.StarColor;

        _light.Color = ScaleLightColor(
            star.StarColor,
            star.LightIntensity);

        _light.Range = GetLightRange(star);

        _surfaceRadius = Math.Max(0.001, star.Radius);
        _baseSurfaceRotation = star.RotationQuaternion;

        _rotationRate = double.IsFinite(star.RotationRate)
            ? Math.Clamp(star.RotationRate, 0.0, 10.0)
            : 0.0;

        _rotationDirection = star.RotationDirection;

        ApplySurfaceTransform();

    }

    private static Transform3D CreateStarSurfaceTransform(double radius, Quaternion rotation)
    {
        Transform3DGroup transform = new();

        transform.Children.Add(
            new ScaleTransform3D(radius, radius, radius));

        transform.Children.Add(
            new RotateTransform3D(
                new QuaternionRotation3D(rotation)));

        return transform;
    }

    private static Color ScaleLightColor(
        Color color,
        double intensity)
    {
        intensity = Math.Clamp(intensity, 0.0, 1.0);

        return Color.FromRgb(
            (byte)Math.Round(color.R * intensity),
            (byte)Math.Round(color.G * intensity),
            (byte)Math.Round(color.B * intensity));
    }

    private static double GetLightRange(LocalStar star)
    {
        Vector3D centerVector = new(
            star.Center.X,
            star.Center.Y,
            star.Center.Z);

        return Math.Max(star.LightRange, centerVector.Length + 2.0);
    }

    public void AdvanceAnimation(WorldGlobeAnimationFrame frame)
    {
        if (frame.DeltaSeconds <= 0.0 || _rotationRate <= 0.0)
            return;

        double direction =
            _rotationDirection == WorldRotationDirection.Prograde
                ? 1.0
                : -1.0;

        double degreesPerSecond =
            360.0 / SurfaceSecondsPerRevolution * _rotationRate;

        _surfaceAnimationAngle +=
            direction * degreesPerSecond * frame.DeltaSeconds;

        _surfaceAnimationAngle %= 360.0;

        if (_surfaceAnimationAngle < 0.0)
            _surfaceAnimationAngle += 360.0;

        ApplySurfaceTransform();
    }

    public void ResetAnimation()
    {
        _surfaceAnimationAngle = 0.0;
        ApplySurfaceTransform();
    }

    private void ApplySurfaceTransform()
    {
        Quaternion spinRotation = new(
            new Vector3D(0, 1, 0),
            _surfaceAnimationAngle);

        Quaternion surfaceRotation = Quaternion.Multiply(
            _baseSurfaceRotation,
            spinRotation);

        _sunSurface.Transform = CreateStarSurfaceTransform(_surfaceRadius, surfaceRotation);
    }
}