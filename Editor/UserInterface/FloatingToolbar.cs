using System.Windows;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    public abstract class FloatingToolbar : RealmStudioWindow
    {
        public override RealmWindowType WindowType => RealmWindowType.FloatingToolbar;
        public override WindowAnimationProfile AnimationProfile => WindowAnimationProfiles.FloatingToolbar;

        protected FloatingToolbar()
        {
            ShowInTaskbar = false;

            ResizeMode = ResizeMode.NoResize;

            SizeToContent = SizeToContent.WidthAndHeight;

            WindowStartupLocation = WindowStartupLocation.Manual;

            Focusable = false;

            ShowActivated = false;
        }
    }
}