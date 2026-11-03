using RealmStudioShapeRenderingLib;

namespace RealmStudioX.WPF.Views.Celestial;

internal static class AnimationRendererFactory
{
    public static ICelestialBodyAnimationRenderer Create(
        CelestialBodyAnimation animation)
    {
        ArgumentNullException.ThrowIfNull(animation);

        return animation.AnimationType switch
        {
            CelestialBodyAnimationType.BasicRotation =>
                new BasicRotationAnimationRenderer(),

            CelestialBodyAnimationType.RingTurbulenceActivity =>
                new RingTurbulenceAnimationRenderer(),

            _ => throw new NotSupportedException(
                $"Animation type '{animation.AnimationType}' is not supported.")
        };
    }
}