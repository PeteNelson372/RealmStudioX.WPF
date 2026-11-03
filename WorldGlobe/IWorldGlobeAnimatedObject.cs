namespace RealmStudioX.WPF.WorldGlobe;

public interface IWorldGlobeAnimatedObject
{
    void AdvanceAnimation(WorldGlobeAnimationFrame frame);

    void ResetAnimation();
}