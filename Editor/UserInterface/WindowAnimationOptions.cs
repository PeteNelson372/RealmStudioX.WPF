using System.Windows.Media.Animation;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    public sealed class WindowAnimationOptions
    {
        public WindowAnimationStyle Style { get; init; }

        public TimeSpan Duration { get; init; }

        public IEasingFunction? Easing { get; init; } = null;

        public double ScaleFrom { get; init; }

        public double SlideX { get; init; }

        public double SlideY { get; init; }

        public double BlurRadius { get; init; }

        public bool AnimateOpacity { get; init; } = true;
    }
}
