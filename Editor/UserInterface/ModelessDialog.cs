using System.Windows;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    public abstract class ModelessDialog : RealmStudioWindow
    {
        public override RealmWindowType WindowType => RealmWindowType.ModelessDialog;

        protected ModelessDialog()
        {
            ShowInTaskbar = false;
        }
    }
}