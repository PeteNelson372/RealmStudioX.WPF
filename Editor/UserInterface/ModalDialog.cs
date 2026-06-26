using System.Windows;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    public abstract class ModalDialog : RealmStudioWindow
    {
        public override RealmWindowType WindowType => RealmWindowType.ModalDialog;

        public override WindowAnimationProfile AnimationProfile => WindowAnimationProfiles.ModalDialog;

        protected ModalDialog()
        {
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
    }
}
