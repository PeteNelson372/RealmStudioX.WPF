using System.Windows;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    public abstract class Palette : ToolWindow
    {
        public override RealmWindowType WindowType => RealmWindowType.Palette;

        protected Palette()
        {
            SizeToContent = SizeToContent.Manual;
        }
    }
}