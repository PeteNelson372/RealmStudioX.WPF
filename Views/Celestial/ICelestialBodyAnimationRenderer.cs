using RealmStudioShapeRenderingLib;

namespace RealmStudioX.WPF.Views.Celestial;

public interface ICelestialBodyAnimationRenderer : ICelestialAnimatedObject
{
    CelestialBodyAnimationType AnimationType { get; }

    void Update(CelestialBodyAnimation animation, ICelestialBodyAnimationTarget target);
}