using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    internal interface IKeyHandler
    {
        bool OnKeyDown(Key key);
        bool OnKeyPress(char c);
    }
}
