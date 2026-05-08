using System.Windows.Input;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    public class InputRouter
    {
        private readonly EditorController _editor;

        public InputRouter(EditorController editor)
        {
            _editor = editor;
        }

        public void HandleKeyDown(Key key, ModifierKeys modifiers)
        {
            if (_editor.ActiveEditorTool is IKeyHandler tool &&                
                tool.OnKeyDown(key))
            {
                return;
            }

            KeyHandler.HandleKey(_editor, key, modifiers);
        }

        public void HandleKeyUp(Key key, ModifierKeys modifiers)
        {
            _editor.CommitSymbolNudge();
            _editor.CommitLabelNudge();
        }

        public void HandleTextInput(char ch)
        {
            if (_editor.ActiveEditorTool is IKeyHandler tool)
            {
                tool.OnKeyPress(ch);
            }
        }
    }
}
