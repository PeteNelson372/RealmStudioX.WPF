namespace RealmStudioX.WPF.Editor.UserInterface
{
    public abstract class ModelessDialog : RealmStudioWindow
    {
        public override RealmWindowType WindowType => RealmWindowType.ModelessDialog;

        public override WindowAnimationProfile AnimationProfile => WindowAnimationProfiles.ModelessDialog;

        protected ModelessDialog()
        {
            ShowInTaskbar = false;
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
        }
    }
}