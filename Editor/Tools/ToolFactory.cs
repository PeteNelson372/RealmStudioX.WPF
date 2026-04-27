using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.ViewModels.Panels;

namespace RealmStudioX.WPF.Editor.Tools
{
    public class ToolFactory
    {
        private readonly CommandManager _commands;
        private readonly IAssetProvider _assets;
        private readonly MapScene _scene;
        private readonly EditorState _editorState;

        public ToolFactory(
            CommandManager commands,
            IAssetProvider assets,
            MapScene scene,
            EditorState editorState)
        {
            _commands = commands;
            _assets = assets;
            _scene = scene;
            _editorState = editorState;
        }

        public IToolEditor? Create(EditorToolType type, object? context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            IToolEditor tool;

            switch (type)
            {
                case EditorToolType.LandformTool:
                    {
                        tool = new LandformTool(_commands, _assets,
                            MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.LANDFORMLAYER),
                            _scene, _editorState, (ILandformSettings)context);

                        return tool;
                    }
            }

            return null;
        }
    }
}
