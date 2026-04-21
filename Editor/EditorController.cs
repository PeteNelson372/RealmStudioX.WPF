using RealmStudioX.Core;

namespace RealmStudioX.WPF.Editor
{
    public class EditorController
    {
        public CommandManager Commands { get; } = new();

        public MapScene? Scene { get; set; }

        private IToolEditor? _activeTool;

        public IToolEditor? ActiveEditorTool
        {
            get { return _activeTool; }
            set { _activeTool = value; }
        }


    }
}
