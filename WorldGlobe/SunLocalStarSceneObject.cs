using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.EditorUtilities;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.WorldGlobe;

internal sealed class SunLocalStarSceneObject : ILocalStarSceneObject, IWorldGlobeAnimatedObject, ICameraFacingSceneObject
{
    private static readonly Lazy<BitmapSource> SunTexture = new(() => WorldGlobeAssets.LoadBitmap("sol-texture.jpg"));

    private readonly MeshGeometry3D _unitSphereMesh;

    private readonly Model3DGroup _positionGroup = new();

    private readonly GeometryModel3D _sunSurface;
    private readonly GeometryModel3D _corona;
    private readonly PointLight _light;

    private readonly EmissiveMaterial _sunMaterial;

    private readonly ImageBrush _coronaBrush;
    private readonly EmissiveMaterial _coronaMaterial;

    private double _coronaRadius = 0.15;
    private double _coronaAnimationAngle;

    private Quaternion _baseCoronaRotation = Quaternion.Identity;

    // Primary rotation.
    private readonly Vector3D _coronaRotationAxis = CreateRandomRotationAxis();

    private double _coronaRotationRate = 12.0;
    private bool _enableCorona = true;

    private readonly double _coronaRotationDirection = Random.Shared.Next(2) == 0 ? -1.0 : 1.0;

    // Secondary rotational movement.
    private readonly Vector3D _coronaWobbleAxisA = CreateRandomRotationAxis();

    private readonly Vector3D _coronaWobbleAxisB = CreateRandomRotationAxis();

    private readonly double _coronaWobbleAmplitudeA = 3.0 + Random.Shared.NextDouble() * 4.0;

    private readonly double _coronaWobbleAmplitudeB = 2.0 + Random.Shared.NextDouble() * 3.0;

    private readonly double _coronaWobbleFrequencyA = 0.35 + Random.Shared.NextDouble() * 0.45;

    private readonly double _coronaWobbleFrequencyB = 0.65 + Random.Shared.NextDouble() * 0.65;

    private readonly double _coronaWobblePhaseA = Random.Shared.NextDouble() * Math.PI * 2.0;

    private readonly double _coronaWobblePhaseB = Random.Shared.NextDouble() * Math.PI * 2.0;

    private double _coronaElapsedTime;

    private readonly Model3DGroup _billboardGroup = new();

    private readonly QuaternionRotation3D _billboardRotation = new(Quaternion.Identity);

    private readonly ScaleTransform3D _billboardScale = new(1.0, 1.0, 1.0);

    private readonly SolarActivityEffect3D _solarActivity;

    private Transform3D _locationTransform = Transform3D.Identity;
    private Point3D _starCenter;
    private Camera? _camera;

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
        SunSettings settings = star.LocalStarDetailSettings as SunSettings ?? new SunSettings();

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
            BackMaterial = _sunMaterial,
            Transform = CreateStarSurfaceTransform(star.Radius, star.RotationQuaternion)
        };

        // Textured corona. It uses the same texture as the Sun's surface,
        // but rotates independently to create the impression of a roiling atmosphere.
        _coronaBrush = new ImageBrush(SunTexture.Value)
        {
            Stretch = Stretch.Fill,
            Opacity = settings.EnableCorona
                ? Clamp(settings.CoronaOpacity, 0.18, 0.05, 0.30)
                : 0.0
        };

        _coronaMaterial = new EmissiveMaterial(_coronaBrush)
        {
            Color = star.StarColor
        };

        _corona = new GeometryModel3D
        {
            Geometry = _unitSphereMesh,
            Material = _coronaMaterial,
            Transform = CreateStarSurfaceTransform(star.Radius * 1.12, star.RotationQuaternion)
        };

        // Point light at the Sun's local origin.
        _light = new PointLight
        {
            Position = new Point3D(0, 0, 0),
            Color = ScaleLightColor(star.StarColor, star.LightIntensity),
            Range = GetLightRange(star),
            ConstantAttenuation = 1.0,
            LinearAttenuation = 0.0,
            QuadraticAttenuation = 0.02
        };

        Transform3DGroup billboardTransform = new();

        billboardTransform.Children.Add(new RotateTransform3D(_billboardRotation));

        billboardTransform.Children.Add(_billboardScale);

        _solarActivity = new SolarActivityEffect3D(
            star.StarColor,
            includeCoronaGlow: false,
            activityGeometryScale: settings.ActivityGeometryScale,
            activityThicknessScale: settings.ActivityThicknessScale);

        _billboardGroup.Children.Add(_solarActivity.Root);

        _billboardGroup.Transform = billboardTransform;

        _positionGroup.Children.Add(_corona);
        _positionGroup.Children.Add(_sunSurface);
        _positionGroup.Children.Add(_billboardGroup);
        _positionGroup.Children.Add(_light);

        Update(star);
    }

    public void Update(LocalStar star)
    {
        SunSettings settings = star.LocalStarDetailSettings as SunSettings ?? new SunSettings();

        _locationTransform = star.LocationTransform ?? Transform3D.Identity;
        Root.Transform = _locationTransform;

        _starCenter = star.Center;

        Root.Transform = star.LocationTransform;

        _positionGroup.Transform = new TranslateTransform3D(
            star.Center.X,
            star.Center.Y,
            star.Center.Z);

        _sunSurface.Transform = CreateStarSurfaceTransform(
            star.Radius,
            star.RotationQuaternion);

        _sunMaterial.Color = star.StarColor;

        _coronaMaterial.Color = star.StarColor;

        _enableCorona = settings.EnableCorona;

        _coronaBrush.Opacity = _enableCorona
            ? Clamp(settings.CoronaOpacity, 0.18, 0.05, 0.30)
            : 0.0;

        _coronaRotationRate = Clamp(
            settings.CoronaRotationSpeed,
            12.0,
            0.0,
            25.0);

        _coronaRadius = Math.Max(0.001, star.Radius * 1.12);
        _baseCoronaRotation = star.RotationQuaternion;

        ApplyCoronaTransform();

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

        double radius = Math.Max(0.001, star.Radius);

        _billboardScale.ScaleX = radius;
        _billboardScale.ScaleY = radius;
        _billboardScale.ScaleZ = radius;

        _solarActivity.Update(
            star.StarColor,
            settings.EnableProminences,
            settings.EnableFlares,
            settings.ActivityIntensity,
            settings.ActivityFrequency,
            settings.ActivityGeometryScale,
            settings.ActivityThicknessScale);

        ApplySurfaceTransform();

    }

    private void ApplyCoronaTransform()
    {
        // Continuous primary rotation.
        Quaternion primaryRotation = new(
            _coronaRotationAxis,
            _coronaAnimationAngle);

        // Two smaller, independently oscillating rotations.
        double wobbleAngleA =
            Math.Sin(
                _coronaElapsedTime * _coronaWobbleFrequencyA * Math.PI * 2.0
                + _coronaWobblePhaseA)
            * _coronaWobbleAmplitudeA;

        double wobbleAngleB =
            Math.Sin(
                _coronaElapsedTime * _coronaWobbleFrequencyB * Math.PI * 2.0
                + _coronaWobblePhaseB)
            * _coronaWobbleAmplitudeB;

        Quaternion wobbleRotationA = new(
            _coronaWobbleAxisA,
            wobbleAngleA);

        Quaternion wobbleRotationB = new(
            _coronaWobbleAxisB,
            wobbleAngleB);

        // Combine the rotations.
        Quaternion coronaRotation = Quaternion.Multiply(
            _baseCoronaRotation,
            Quaternion.Multiply(
                primaryRotation,
                Quaternion.Multiply(
                    wobbleRotationA,
                    wobbleRotationB)));

        _corona.Transform = CreateStarSurfaceTransform(
            _coronaRadius,
            coronaRotation);
    }

    private static Vector3D CreateRandomRotationAxis()
    {
        Vector3D axis;

        do
        {
            axis = new Vector3D(
                Random.Shared.NextDouble() * 2.0 - 1.0,
                Random.Shared.NextDouble() * 2.0 - 1.0,
                Random.Shared.NextDouble() * 2.0 - 1.0);
        }
        while (axis.LengthSquared < 0.0001);

        axis.Normalize();

        return axis;
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

    public void UpdateCamera(Camera? camera)
    {
        _camera = camera;

        if (camera is not ProjectionCamera projectionCamera)
            return;

        Point3D cameraPosition = projectionCamera.Position;

        if (projectionCamera.Transform is Transform3D cameraTransform)
            cameraPosition = cameraTransform.Transform(cameraPosition);

        try
        {
            if (_locationTransform.Value.HasInverse)
            {
                cameraPosition =
                    _locationTransform.Inverse.Transform(cameraPosition);
            }
        }
        catch (InvalidOperationException)
        {
            // Fall back to the untransformed camera position.
        }

        Vector3D directionToCamera = cameraPosition - _starCenter;

        if (directionToCamera.LengthSquared < 1e-12)
            return;

        directionToCamera.Normalize();

        Vector3D localNormal = new(0, 0, 1);

        double dot = Math.Clamp(
            Vector3D.DotProduct(localNormal, directionToCamera),
            -1.0,
            1.0);

        Vector3D axis = Vector3D.CrossProduct(
            localNormal,
            directionToCamera);

        if (axis.LengthSquared < 1e-12)
        {
            _billboardRotation.Quaternion = dot >= 0.0
                ? Quaternion.Identity
                : new Quaternion(new Vector3D(0, 1, 0), 180.0);

            return;
        }

        axis.Normalize();

        double angleDegrees =
            Math.Acos(dot) * 180.0 / Math.PI;

        _billboardRotation.Quaternion =
            new Quaternion(axis, angleDegrees);
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
        if (frame.DeltaSeconds <= 0.0)
            return;

        _solarActivity.AdvanceAnimation(frame);

        if (_rotationRate > 0)
        {
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

        // Animate the corona independently of the Sun's surface.
        _coronaElapsedTime += frame.DeltaSeconds;

        _coronaAnimationAngle +=
            _coronaRotationDirection *
            _coronaRotationRate *
            frame.DeltaSeconds;

        _coronaAnimationAngle %= 360.0;

        if (_coronaAnimationAngle < 0.0)
            _coronaAnimationAngle += 360.0;

        ApplyCoronaTransform();

    }

    public void ResetAnimation()
    {
        _solarActivity.ResetAnimation();

        _surfaceAnimationAngle = 0.0;
        _coronaAnimationAngle = 0.0;

        ApplySurfaceTransform();
        ApplyCoronaTransform();
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

    private static double Clamp(
    double value,
    double fallback,
    double minimum,
    double maximum) =>
    double.IsFinite(value)
        ? Math.Clamp(value, minimum, maximum)
        : fallback;
}