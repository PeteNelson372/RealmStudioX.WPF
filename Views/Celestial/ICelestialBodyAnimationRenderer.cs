using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.WorldGlobe;

namespace RealmStudioX.WPF.Views.Celestial;

public interface ICelestialBodyAnimationRenderer : IWorldGlobeAnimatedObject
{
    CelestialBodyAnimationType AnimationType { get; }

    void Update(CelestialBodyAnimation animation, ICelestialBodyAnimationTarget target);
}