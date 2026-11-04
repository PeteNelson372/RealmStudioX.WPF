using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.WorldGlobe;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.Views.Celestial;

internal sealed class SolarActivityAnimationRenderer :
    ICelestialBodyAnimationRenderer,
    ICameraFacingCelestialObject
{
    private readonly SolarActivityEffect3D _effect;

    private readonly Model3DGroup _billboardGroup = new();

    private readonly QuaternionRotation3D _billboardRotation =
        new(Quaternion.Identity);

    private ICelestialBodyVisualTarget? _target;

    private bool _enabled;

    public CelestialBodyAnimationType AnimationType => CelestialBodyAnimationType.SolarActivity;

    public SolarActivityAnimationRenderer()
    {
        Transform3DGroup billboardTransform = new();

        billboardTransform.Children.Add(
            new RotateTransform3D(_billboardRotation));

        _billboardGroup.Transform = billboardTransform;

        _effect = new SolarActivityEffect3D(
            Colors.White,
            includeCoronaGlow: false);

        _billboardGroup.Children.Add(_effect.Root);
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

        if (_target is not null &&
            !ReferenceEquals(_target, visualTarget))
        {
            _target.BodyVisualRoot.Children.Remove(_billboardGroup);
        }

        _target = visualTarget;

        if (!_target.BodyVisualRoot.Children.Contains(_billboardGroup))
        {
            _target.BodyVisualRoot.Children.Add(_billboardGroup);
        }

        SunAnimationSettings settings =
            animation.Settings as SunAnimationSettings
            ?? throw new InvalidOperationException(
                "SolarActivity requires SunAnimationSettings.");

        _enabled = animation.Enabled;

        _effect.Update(
            visualTarget.BodyColor,
            settings.EnableProminences && _enabled,
            settings.EnableFlares && _enabled,
            settings.ActivityIntensity,
            settings.ActivityFrequency,
            settings.ActivityGeometryScale,
            settings.ActivityThicknessScale);
    }

    public void UpdateCamera(Camera? camera)
    {
        if (camera is not ProjectionCamera projectionCamera ||
            _target is null)
        {
            return;
        }

        Point3D cameraPosition = projectionCamera.Position;

        if (projectionCamera.Transform is Transform3D cameraTransform)
        {
            cameraPosition = cameraTransform.Transform(cameraPosition);
        }

        // Convert the camera position into BodyVisualRoot's local
        // coordinate system, compensating for the star's scale
        // and animation transforms.
        Transform3D? bodyTransform = _target.BodyVisualRoot.Transform;

        if (bodyTransform is not null)
        {
            Matrix3D inverse = bodyTransform.Value;

            if (inverse.HasInverse)
            {
                inverse.Invert();
                cameraPosition = inverse.Transform(cameraPosition);
            }
        }

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

        _effect.AdvanceAnimation(frame);
    }

    public void ResetAnimation()
    {
        _effect.ResetAnimation();
    }
}