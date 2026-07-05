using System.Windows;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    public abstract class RealmStudioMainWindow : RealmStudioWindow
    {
        public override RealmWindowType WindowType => RealmWindowType.MainWindow;

        protected RealmStudioMainWindow()
        {
        }
    }
}
