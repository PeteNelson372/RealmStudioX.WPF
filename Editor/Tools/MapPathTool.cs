using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Panels;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using Svg.Skia;

namespace RealmStudioX.WPF.Editor.Tools
{
    public sealed class MapPathTool(
        CommandManager commands,
        IAssetProvider assets,
        MapLayer targetLayer,
        MapScene scene,
        EditorState editorState,
        IMapPathSettings pathSettings) : IToolEditor, IDisposable
    {
        private readonly CommandManager _commands = commands;
        private MapLayer _layer = targetLayer;
        private readonly IAssetProvider _assets = assets;
        private readonly MapScene _scene = scene;
        private readonly EditorState _editorState = editorState;
        private readonly IMapPathSettings _pathSettings = pathSettings;

        private MapPath? _activeMapPath;
        private SKPoint _lastMouseWorld;
        private bool _drawOverSymbols;


        private bool disposedValue;

        public void Activate()
        {

        }

        public void Cancel()
        {

        }

        public void Deactivate()
        {

        }

        public void OnMouseDown(PointerState state)
        {
            if (_editorState.CurrentDrawingMode == MapDrawingMode.PathPaint)
            {
                PathRenderStyle renderStyle = new PathRenderStyle()
                {
                    Width = _pathSettings.PathWidth,
                    BorderColor = _pathSettings.PathBorderColor.ToSKColor(),
                    Color = _pathSettings.PathColor.ToSKColor(),
                    TextureId = (_pathSettings.PathTextureId != null) ? _pathSettings.PathTextureId : string.Empty,
                    DrawCrenelations = _pathSettings.ShowCrenelations,
                    MapPathType = _pathSettings.PathStyle,
                    TextureOpacity = _pathSettings.TextureOpacity,
                    TextureScale = _pathSettings.TextureScale,
                    TowerDistance = _pathSettings.TowerDistance,
                    TowerSize = _pathSettings.TowerSize,
                };

                IReadOnlyList<AssetDescriptor>? descriptors = null;

                switch (renderStyle.MapPathType)
                {
                    case PathType.FootprintsPath:
                        {
                            renderStyle.UseMarkers = true;
                            descriptors = ((AssetManager)_assets).GetByName(AssetType.Vector, "Foot Prints");
                            renderStyle.MarkerSpacing = 2f;
                        }
                        break;

                    case PathType.BearTracksPath:
                        {
                            renderStyle.UseMarkers = true;
                            descriptors = ((AssetManager)_assets).GetByName(AssetType.Vector, "Bear Tracks");
                            renderStyle.MarkerSpacing = 1.5f;
                        }
                        break;
                    case PathType.BirdTracksPath:
                        {
                            renderStyle.UseMarkers = true;
                            descriptors = ((AssetManager)_assets).GetByName(AssetType.Vector, "Bird Tracks");
                            renderStyle.MarkerSpacing = 1.2f;
                        }
                        break;
                    case PathType.TexturedPath:
                        {
                            renderStyle.UseTexture = true;
                        }
                        break;
                    case PathType.BorderAndTexturePath:
                        {
                            renderStyle.UseTexture = true;
                        }
                        break;
                }

                if (renderStyle.UseMarkers && descriptors != null && descriptors.Count > 0)
                {
                    AssetDescriptor marker = descriptors[0];
                    SKSvg svg = new();
                    renderStyle.Marker = svg.Load(marker.FilePath);
                }                

                _activeMapPath = new MapPath
                {
                    RenderStyle = renderStyle
                };

                _activeMapPath.ResolveAssets(_assets);

                _activeMapPath.Editor.BeginDraw(state.WorldPoint);
            }
        }

        public void OnMouseMove(PointerState state)
        {
            _lastMouseWorld = state.WorldPoint;
            bool ctrl = (state.Modifiers & InputModifiers.Control) == InputModifiers.Control;
            bool shift = (state.Modifiers & InputModifiers.Shift) == InputModifiers.Shift;

            if (state.Button == EditorMouseButton.Left)
            {
                if (_editorState.CurrentDrawingMode == MapDrawingMode.PathPaint)
                {
                    _activeMapPath?.Editor.ContinueDraw(state.WorldPoint, ctrl, shift);
                }
            }
        }

        public void OnMouseUp(PointerState state)
        {
            _lastMouseWorld = state.WorldPoint;

            if (state.Button == EditorMouseButton.Left)
            {
                if (_editorState.CurrentDrawingMode == MapDrawingMode.PathPaint)
                {
                    if (_activeMapPath != null)
                    {
                        CommitPath(_lastMouseWorld);

                        _activeMapPath = null;
                    }
                }
            }
        }

        public void OnMouseDoubleClick(PointerState state)
        {
            // no action
        }

        public void OnMouseWheel(PointerState state)
        {
            // no action
        }

        private void CommitPath(SKPoint worldPos)
        {
            if (_activeMapPath == null)
            {
                return;
            }

            var mapPath = _activeMapPath;

            mapPath.ControlPoints.Add(worldPos);

            mapPath.RestoreGeometry(Utilities.BuildPath(mapPath.ControlPoints));

            mapPath.Editor.EndDraw();

            if (_drawOverSymbols)
            {
                _layer = MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.PATHUPPERLAYER);
            }
            else
            {
                _layer = MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.PATHLOWERLAYER);
            }

            var cmd = new Cmd_ModifyMapPaths(_scene.Map, _layer);

            cmd.RegisterAddedMapPath(mapPath);

            _commands.Execute(cmd);
        }

        public void RenderOverlay(SKCanvas canvas, SKPoint world)
        {
            if (_activeMapPath != null)
            {
                if (_activeMapPath.Editor.IsDrawing)
                {
                    using (new SKAutoCanvasRestore(canvas))
                    {
                        canvas.ClipPath(_scene.GetLandClipPath());
                        _activeMapPath?.Render(canvas, null);
                    }
                }
            }

        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MapPathTool()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
