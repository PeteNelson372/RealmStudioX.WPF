namespace RealmStudioX.WPF.WorldGlobe;

internal interface IWorldGlobeAnimatedObject
{
    void AdvanceAnimation(WorldGlobeAnimationFrame frame);

    void ResetAnimation();
}