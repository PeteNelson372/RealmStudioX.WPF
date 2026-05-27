using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
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
        private readonly FontManager _fontManager;
        private readonly RenderContext _renderContext;

        public ToolFactory(
            CommandManager commands,
            IAssetProvider assets,
            MapScene scene,
            EditorState editorState,
            EditorController editor,
            FontManager fontManager,
            RenderContext renderContext)
        {
            _commands = commands;
            _assets = assets;
            _scene = scene;
            _editorState = editorState;
            _editor = editor;
            _fontManager = fontManager;
            _renderContext = renderContext;
        }

        public IToolEditor? Create(EditorToolType type, object? context)
        {
            IToolEditor tool;

            // TODO: figure out how to reduce the number of parameters needed to construct tools

            switch (type)
            {
                case EditorToolType.LandformTool:
                    {
                        if (_editor.ActiveEditorTool is not LandformTool)
                        {
                            if (context is null) return null;

                            _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.LANDFORMLAYER));

                            tool = new LandformTool(_commands, _assets,
                                MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.LANDFORMLAYER),
                                _scene, _editorState, (ILandformSettings)context);

                            return tool;
                        }
                        else
                        {
                            return _editor.ActiveEditorTool;
                        }
                    }
                case EditorToolType.WaterBodyTool:
                    {
                        if (_editor.ActiveEditorTool is not WaterBodyTool)
                        {
                            if (context is null) return null;

                            _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.WATERLAYER));

                            tool = new WaterBodyTool(_commands, _assets,
                                MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.WATERLAYER),
                                _scene, _editorState, (IWaterBodySettings)context);

                            return tool;
                        }
                        else
                        {
                            return _editor.ActiveEditorTool;
                        }
                    }
                case EditorToolType.MapPathTool:
                    {
                        if (_editor.ActiveEditorTool is not MapPathTool)
                        {
                            if (context is null) return null;

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
                        else
                        {
                            return _editor.ActiveEditorTool;
                        }
                    }
                case EditorToolType.SymbolTool:
                    {
                        if (_editor.ActiveEditorTool is not SymbolTool)
                        {
                            if (context is null) return null;

                            _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.SYMBOLLAYER));

                            _editor.SymbolSelectionService.SetPrimarySelectedSymbol(null);
                            _editor.SymbolSelectionService.ClearSecondary();

                            tool = new SymbolTool(_commands, _assets,
                                MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.SYMBOLLAYER),
                                _scene, _editor.SymbolSelectionService, ((AssetManager)_assets).SymbolImageCache, _editorState, (ISymbolSettings)context)
                            {
                                RenderContext = _renderContext
                            };

                            return tool;
                        }
                        else
                        {
                            return _editor.ActiveEditorTool;
                        }
                    }
                case EditorToolType.LabelTool:
                    {
                        if (_editor.ActiveEditorTool is not LabelTool)
                        {
                            if (context is null) return null;

                            _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.LABELLAYER));

                            tool = new LabelTool(_commands, _assets,
                                MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.LABELLAYER),
                                _scene, _editorState, _fontManager, _editor, (ILabelSettings)context);

                            return tool;
                        }
                        else
                        {
                            return _editor.ActiveEditorTool;
                        }
                    }
                case EditorToolType.BoxTool:
                    {
                        if (_editor.ActiveEditorTool is not BoxTool)
                        {
                            if (context is null) return null;

                            _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.BOXLAYER));

                            tool = new BoxTool(_commands, _assets,
                                MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.BOXLAYER),
                                _scene, _editorState, _fontManager, _editor, (IBoxSettings)context);

                            return tool;
                        }
                        else
                        {
                            return _editor.ActiveEditorTool;
                        }
                    }
                case EditorToolType.WindroseTool:
                    {
                        if (_editor.ActiveEditorTool is not WindroseTool)
                        {
                            if (context is null) return null;

                            _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.WINDROSELAYER));

                            tool = new WindroseTool(_commands, _assets,
                                MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.WINDROSELAYER),
                                _scene, _editorState, _fontManager, _editor, (IWindroseSettings)context);

                            return tool;
                        }
                        else
                        {
                            return _editor.ActiveEditorTool;
                        }
                    }
                case EditorToolType.MeasureTool:
                    {
                        if (_editor.ActiveEditorTool is not MeasureTool)
                        {
                            if (context is null) return null;

                            _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.MEASURELAYER));

                            tool = new MeasureTool(_commands, _assets,
                                MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.MEASURELAYER),
                                _scene, _editorState, _fontManager, _editor, (IMeasureSettings)context);

                            return tool;
                        }
                        else
                        {
                            return _editor.ActiveEditorTool;
                        }
                    }
                case EditorToolType.RegionTool:
                    {
                        if (_editor.ActiveEditorTool is not RegionTool)
                        {
                            if (context is null) return null;

                            _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.REGIONLAYER));

                            tool = new RegionTool(_commands, _assets,
                                MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.REGIONLAYER), _editor,
                                _scene, _editorState, _fontManager, _editor, (IRegionSettings)context);

                            return tool;
                        }
                        else
                        {
                            return _editor.ActiveEditorTool;
                        }
                    }
                case EditorToolType.DrawingTool:
                    {
                        if (_editor.ActiveEditorTool is not DrawingTool)
                        {
                            if (context is null) return null;


                            tool = new DrawingTool(_commands, _assets, _editor, _editor.ActiveDrawingLayer!,
                                _scene, _editorState, (IDrawingSettings)context);

                            return tool;
                        }
                        else
                        {
                            return _editor.ActiveEditorTool;
                        }
                    }
            }

            return null;
        }
    }
}
