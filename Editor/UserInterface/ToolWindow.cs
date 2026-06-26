using System.Windows;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    public abstract class ToolWindow : RealmStudioWindow
    {
        public override RealmWindowType WindowType => RealmWindowType.ToolWindow;

        protected ToolWindow()
        {
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.CanResize;
        }
    }
}