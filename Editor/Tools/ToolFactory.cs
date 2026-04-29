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
        private readonly EditorController _editor;

        public ToolFactory(
            CommandManager commands,
            IAssetProvider assets,
            MapScene scene,
            EditorState editorState,
            EditorController editor)
        {
            _commands = commands;
            _assets = assets;
            _scene = scene;
            _editorState = editorState;
            _editor = editor;
        }

        public IToolEditor? Create(EditorToolType type, object? context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            IToolEditor tool;

            switch (type)
            {
                case EditorToolType.LandformTool:
                    {
                        _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.LANDFORMLAYER));

                        tool = new LandformTool(_commands, _assets,
                            MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.LANDFORMLAYER),
                            _scene, _editorState, (ILandformSettings)context);

                        return tool;
                    }
                case EditorToolType.WaterBodyTool:
                    {
                        _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.WATERLAYER));

                        tool = new WaterBodyTool(_commands, _assets,
                            MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.WATERLAYER),
                            _scene, _editorState, (IWaterBodySettings)context);

                        return tool;
                    }
                case EditorToolType.MapPathTool:
                    {
                        MapLayer activeLayer;

                        IMapPathSettings settings = (IMapPathSettings)context;

                        if (settings.DrawOverSymbols)
                        {
                            activeLayer = MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.PATHUPPERLAYER);
                        }
                        else
                        {
                            activeLayer = MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.PATHLOWERLAYER);
                        }
                        
                        _editor.SetActiveDrawingLayer(activeLayer);

                        tool = new MapPathTool(_commands, _assets, activeLayer,
                            _scene, _editorState, (IMapPathSettings)context);

                        return tool;
                    }

            }

            return null;
        }
    }
}
