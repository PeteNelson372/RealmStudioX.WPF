
using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.WorldGlobe;
using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.Views.Celestial;

internal sealed class SolarActivityAnimationRenderer : ICelestialBodyAnimationRenderer, ICameraFacingCelestialObject
{
    private SolarActivityEffect3D? _effect;

    private readonly Model3DGroup _billboardGroup = new();

    private readonly QuaternionRotation3D _billboardRotation =
        new(Quaternion.Identity);

    private ICelestialBodyVisualTarget? _target;

    private bool _enabled;
    private bool _includeCoronaGlow;

    public CelestialBodyAnimationType AnimationType => CelestialBodyAnimationType.SolarActivity;

    public SolarActivityAnimationRenderer()
    {
        _billboardGroup.Transform = new RotateTransform3D(_billboardRotation);
    }

    public void Update(
        CelestialBodyAnimation animation,
        ICelestialBodyAnimationTarget target)
    {
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(target);

        if (target is not ICelestialBodyVisualTarget visualTarget)
        {
            throw new InvalidOperationException(
                "SolarActivity requires a celestial body visual target.");
        }

        bool includeCoronaGlow;
        bool enableProminences;
        bool enableFlares;
        double activityIntensity;
        double activityFrequency;
        double activityGeometryScale;
        double activityThicknessScale;

        switch (animation.Settings)
        {
            case SunAnimationSettings settings:
                includeCoronaGlow = false;
                enableProminences = settings.EnableProminences;
                enableFlares = settings.EnableFlares;
                activityIntensity = settings.ActivityIntensity;
                activityFrequency = settings.ActivityFrequency;
                activityGeometryScale = settings.ActivityGeometryScale;
                activityThicknessScale = settings.ActivityThicknessScale;
                break;

            case CoronaAnimationSettings settings:
                includeCoronaGlow = true;
                enableProminences = settings.EnableProminences;
                enableFlares = settings.EnableFlares;
                activityIntensity = settings.ActivityIntensity;
                activityFrequency = settings.ActivityFrequency;
                activityGeometryScale = settings.ActivityGeometryScale;
                activityThicknessScale = settings.ActivityThicknessScale;
                break;

            default:
                throw new InvalidOperationException(
                    "SolarActivity requires either SunAnimationSettings " +
                    "or CoronaAnimationSettings.");
        }

        if (_target is not null &&
            !ReferenceEquals(_target, visualTarget))
        {
            _target.ActivityVisualRoot.Children.Remove(_billboardGroup);
        }

        _target = visualTarget;

        if (!_target.ActivityVisualRoot.Children.Contains(_billboardGroup))
        {
            _target.ActivityVisualRoot.Children.Add(_billboardGroup);
        }

        // The effect's construction mode determines whether it contains
        // the corona glow, central occlusion disk, and bright limb.
        if (_effect is null || _includeCoronaGlow != includeCoronaGlow)
        {
            _effect?.ResetAnimation();

            _billboardGroup.Children.Clear();

            _includeCoronaGlow = includeCoronaGlow;

            _effect = new SolarActivityEffect3D(
                visualTarget.BodyColor,
                includeCoronaGlow: includeCoronaGlow);

            _billboardGroup.Children.Add(_effect.Root);
        }

        _enabled = animation.Enabled;

        _effect.Update(
            visualTarget.BodyColor,
            enableProminences && _enabled,
            enableFlares && _enabled,
            activityIntensity,
            activityFrequency,
            activityGeometryScale,
            activityThicknessScale);
    }

    public void UpdateCamera(Camera? camera)
    {
        if (camera is not ProjectionCamera projectionCamera)
            return;

        Point3D cameraPosition = projectionCamera.Position;

        Vector3D directionToCamera =
            cameraPosition - new Point3D(0, 0, 0);

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

    public void AdvanceAnimation(WorldGlobeAnimationFrame frame)
    {
        if (!_enabled || frame.DeltaSeconds <= 0.0)
            return;

        _effect?.AdvanceAnimation(frame);
    }

    public void ResetAnimation()
    {
        _effect?.ResetAnimation();
    }
}
