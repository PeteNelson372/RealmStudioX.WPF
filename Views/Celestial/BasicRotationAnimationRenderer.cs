using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.WorldGlobe;
using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.Views.Celestial;

internal sealed class BasicRotationAnimationRenderer
    : ICelestialBodyAnimationRenderer
{
    private const double SecondsPerRevolution = 60.0;

    private readonly AxisAngleRotation3D _rotation =
        new(new Vector3D(0, 1, 0), 0);

    private RotateTransform3D? _rotationTransform;
    private ICelestialBodyAnimationTarget? _target;

    private double _rotationAngle;
    private BasicRotationSettings? _settings;

    private bool _enabled;

    public CelestialBodyAnimationType AnimationType =>
        CelestialBodyAnimationType.BasicRotation;

    public void Update(
        CelestialBodyAnimation animation,
        ICelestialBodyAnimationTarget target)
    {
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(target);

        if (_target is not null &&
            !ReferenceEquals(_target, target) &&
            _rotationTransform is not null)
        {
            _target.AnimationTransform.Children.Remove(
                _rotationTransform);
        }

        _target = target;

        _rotationTransform ??= new RotateTransform3D(_rotation);

        if (!_target.AnimationTransform.Children.Contains(
                _rotationTransform))
        {
            _target.AnimationTransform.Children.Add(
                _rotationTransform);
        }

        BasicRotationSettings settings =
            animation.Settings as BasicRotationSettings
            ?? throw new InvalidOperationException(
                "BasicRotation animation requires BasicRotationSettings.");

        _enabled = animation.Enabled;

        _settings = settings;

        if (!_enabled)
            ResetAnimation();
    }

    public void AdvanceAnimation(WorldGlobeAnimationFrame frame)
    {
        if (!_enabled || _settings is null || frame.DeltaSeconds <= 0.0)
            return;

        double rotationRate = double.IsFinite(_settings.RotationRate)
            ? _settings.RotationRate
            : 1.0;

        double degreesPerSecond =
            360.0 / SecondsPerRevolution * rotationRate;

        if (_settings.RotationDirection ==
            WorldRotationDirection.Retrograde)
        {
            degreesPerSecond = -degreesPerSecond;
        }

        _rotationAngle =
            (_rotationAngle + degreesPerSecond * frame.DeltaSeconds) % 360.0;

        _rotation.Angle = _rotationAngle;
    }

    public void ResetAnimation()
    {
        _rotationAngle = 0.0;
        _rotation.Angle = 0.0;
    }
}