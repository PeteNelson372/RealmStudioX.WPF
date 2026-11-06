namespace RealmStudioX.WPF.Views.Celestial
{
    public interface ICelestialAnimatedObject
    {
        void AdvanceAnimation(CelestialAnimationFrame frame);

        void ResetAnimation();
    }
}