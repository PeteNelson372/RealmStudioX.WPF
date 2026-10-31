using RealmStudioShapeRenderingLib;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.WorldGlobe;

/// <summary>
/// Identifies a scene object whose presentation must be aligned to the current
/// viewport camera. The viewer should call UpdateCamera when the camera changes
/// (and during its render tick if the camera can move continuously).
/// </summary>
internal interface ICameraFacingSceneObject
{
    void UpdateCamera(Camera? camera);
}

/// <summary>
/// A round, opaque black sphere with a camera-facing eclipse corona and randomly
/// emitted prominences/flares around the apparent limb.
/// </summary>
internal sealed class CoronaLocalStarSceneObject :
    ILocalStarSceneObject, IWorldGlobeAnimatedObject, ICameraFacingSceneObject
{
    private readonly Model3DGroup _root = new();
    private readonly Model3DGroup _positionGroup = new();
    private readonly Model3DGroup _sphereOrientationGroup = new();
    private readonly Model3DGroup _sphereScaleGroup = new();
    private readonly Model3DGroup _billboardGroup = new();

    private readonly TranslateTransform3D _translation = new();
    private readonly QuaternionRotation3D _sphereOrientationRotation = new(Quaternion.Identity);
    private readonly QuaternionRotation3D _billboardRotation = new(Quaternion.Identity);
    private readonly ScaleTransform3D _sphereScale = new(1.0, 1.0, 1.0);
    private readonly ScaleTransform3D _billboardScale = new(1.0, 1.0, 1.0);

    private readonly GeometryModel3D _eclipsedDiskModel;
    private readonly SolarActivityEffect3D _solarActivity;
    private readonly PointLight _light;

    private Transform3D _locationTransform = Transform3D.Identity;
    private Point3D _starCenter;
    private Camera? _camera;

    public LocalStarType Type => LocalStarType.Corona;

    public Model3DGroup Root => _root;

    public CoronaLocalStarSceneObject(LocalStar star, MeshGeometry3D unitSphereMesh)
    {
        ArgumentNullException.ThrowIfNull(star);
        ArgumentNullException.ThrowIfNull(unitSphereMesh);

        _locationTransform = star.LocationTransform ?? Transform3D.Identity;
        _root.Transform = _locationTransform;
        _positionGroup.Transform = _translation;
        _sphereOrientationGroup.Transform = new RotateTransform3D(_sphereOrientationRotation);
        _sphereScaleGroup.Transform = _sphereScale;

        Transform3DGroup billboardTransform = new();
        billboardTransform.Children.Add(new RotateTransform3D(_billboardRotation));
        billboardTransform.Children.Add(_billboardScale);
        _billboardGroup.Transform = billboardTransform;

        _root.Children.Add(_positionGroup);
        _positionGroup.Children.Add(_sphereOrientationGroup);
        _positionGroup.Children.Add(_billboardGroup);

        _sphereOrientationGroup.Children.Add(_sphereScaleGroup);

        // A pure-black EmissiveMaterial contributes no emitted light and can be
        // discarded/handled unexpectedly by the 3D material pipeline. Use an
        // explicitly opaque diffuse material for the sphere itself.
        SolidColorBrush opaqueBlackBrush = new(Colors.Black)
        {
            Opacity = 1.0
        };
        opaqueBlackBrush.Freeze();
        Material blackMaterial = new DiffuseMaterial(opaqueBlackBrush);
        if (blackMaterial.CanFreeze)
            blackMaterial.Freeze();

        _eclipsedDiskModel = new GeometryModel3D
        {
            Geometry = unitSphereMesh,
            Material = blackMaterial,
            BackMaterial = blackMaterial
        };
        _sphereScaleGroup.Children.Add(_eclipsedDiskModel);

        // The effect is separate from the star's orientation transform. The viewer
        // continually orients its billboard parent toward the active camera.
        CoronaSettings settings =
            star.LocalStarDetailSettings as CoronaSettings ?? new CoronaSettings();

        _solarActivity = new SolarActivityEffect3D(
            star.StarColor,
            includeCoronaGlow: true,
            activityGeometryScale: settings.ActivityGeometryScale,
            activityThicknessScale: settings.ActivityThicknessScale);


        _billboardGroup.Children.Add(_solarActivity.Root);

        _light = new PointLight
        {
            Position = new Point3D(0, 0, 0),
            Color = ScaleColor(star.StarColor, 0.65),
            ConstantAttenuation = 1.0,
            LinearAttenuation = 0.0
        };

        _positionGroup.Children.Add(_light);

        Update(star);
    }

    public void Update(LocalStar star)
    {
        ArgumentNullException.ThrowIfNull(star);

        CoronaSettings settings =
            star.LocalStarDetailSettings as CoronaSettings ?? new CoronaSettings();

        _locationTransform = star.LocationTransform ?? Transform3D.Identity;
        _root.Transform = _locationTransform;
        _translation.OffsetX = star.Center.X;
        _translation.OffsetY = star.Center.Y;
        _translation.OffsetZ = star.Center.Z;
        _starCenter = star.Center;
        _sphereOrientationRotation.Quaternion = star.RotationQuaternion;

        double radius = double.IsFinite(star.Radius)
            ? Math.Clamp(star.Radius, 0.001, 1_000_000.0)
            : 1.0;
        _sphereScale.ScaleX = radius;
        _sphereScale.ScaleY = radius;
        _sphereScale.ScaleZ = radius;
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

        _light.Color = ScaleColor(star.StarColor, Clamp(star.LightIntensity, 0.65, 0.0, 5.0));
        double distance = (star.Center - new Point3D(0, 0, 0)).Length;
        double effectiveRange = Math.Max(Math.Max(0.1, star.LightRange), distance + 2.0);
        _light.QuadraticAttenuation = 1.0 / (effectiveRange * effectiveRange);

        if (_camera is not null)
            UpdateCamera(_camera);
    }

    public void UpdateCamera(Camera? camera)
    {
        _camera = camera;
        if (camera is not ProjectionCamera projectionCamera)
            return;

        Point3D cameraPosition = projectionCamera.Position;
        if (projectionCamera.Transform is Transform3D cameraTransform)
            cameraPosition = cameraTransform.Transform(cameraPosition);

        // Convert the camera location back into the coordinate system in which
        // LocalStar.Center is applied. The billboard is a sibling of the star's
        // orientation group, so it deliberately does not inherit RotationQuaternion.
        try
        {
            if (_locationTransform.Value.HasInverse)
                cameraPosition = _locationTransform.Inverse.Transform(cameraPosition);
        }
        catch (InvalidOperationException)
        {
            // If a user supplies a non-invertible location transform, fall back
            // to the raw camera position rather than breaking the viewport.
        }

        Vector3D directionToCamera = cameraPosition - _starCenter;
        if (directionToCamera.LengthSquared < 1e-12)
            return;
        directionToCamera.Normalize();

        Vector3D localNormal = new(0, 0, 1);
        double dot = Math.Clamp(Vector3D.DotProduct(localNormal, directionToCamera), -1.0, 1.0);
        Vector3D axis = Vector3D.CrossProduct(localNormal, directionToCamera);

        if (axis.LengthSquared < 1e-12)
        {
            _billboardRotation.Quaternion = dot >= 0.0
                ? Quaternion.Identity
                : new Quaternion(new Vector3D(0, 1, 0), 180.0);
            return;
        }

        axis.Normalize();
        double angleDegrees = Math.Acos(dot) * 180.0 / Math.PI;
        _billboardRotation.Quaternion = new Quaternion(axis, angleDegrees);
    }

    public void AdvanceAnimation(WorldGlobeAnimationFrame frame) =>
        _solarActivity.AdvanceAnimation(frame);

    public void ResetAnimation() => _solarActivity.ResetAnimation();

    private static Color ScaleColor(Color color, double scale) => Color.FromRgb(
        ToByte(color.R * scale),
        ToByte(color.G * scale),
        ToByte(color.B * scale));

    private static byte ToByte(double value) =>
        (byte)Math.Clamp((int)Math.Round(value), 0, 255);

    private static double Clamp(double value, double fallback, double minimum, double maximum) =>
        double.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : fallback;
}
