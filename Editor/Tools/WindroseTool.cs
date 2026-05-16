using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.ViewModels.Panels;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace RealmStudioX.WPF.Editor.Tools
{
    internal class WindroseTool(
            CommandManager commands,
            IAssetProvider assets,
            MapLayer targetLayer,
            MapScene scene,
            EditorState editorState,
            FontManager fontManager,
            IRedrawRequester redraw,
            IWindroseSettings settings) : IToolEditor, IDisposable
        {

        private bool disposedValue;
        // -------------------------------------------------
        // Dependencies
        // -------------------------------------------------

        private readonly CommandManager _commands = commands;
        private readonly MapLayer _layer = targetLayer;
        private readonly IAssetProvider _assets = assets;
        private readonly MapScene _scene = scene;
        private readonly EditorState _editorState = editorState;
        private readonly FontManager _fontManager = fontManager;
        private readonly IRedrawRequester _redraw = redraw;
        private readonly IWindroseSettings _settings = settings;

        private MapWindrose? _currentWindrose = null;

        public void Activate()
        {

        }

        public void Cancel()
        {

        }

        public void Deactivate()
        {

        }

        public void OnMouseDoubleClick(PointerState state)
        {
            // no action
        }

        public void OnMouseDown(PointerState state)
        {
            _currentWindrose = new()
            {
                Bounds = _scene.WorldBounds,
                Location = state.WorldPoint,
                InnerCircles = _settings.WindroseCircles,
                InnerRadius = _settings.WindroseInnerRadius,
                FadeOut = _settings.WindroseFade,
                LineWidth = _settings.WindroseLineWidth,
                OuterRadius = _settings.WindroseOuterRadius,
                WindroseColor = _settings.WindroseColor.ToSKColor(),
                DirectionCount = _settings.WindroseDirections,
            };

            _redraw.RequestRedraw();
        }

        public void OnMouseMove(PointerState state)
        {
            if (_currentWindrose != null && state.Button is EditorMouseButton.Left)
            {
                _currentWindrose.Location = state.WorldPoint;
            }

            _redraw.RequestRedraw();
        }

        public void OnMouseUp(PointerState state)
        {
            if (_currentWindrose == null)
                return;

            MapLayer windroseLayer = MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.WINDROSELAYER);

            Cmd_ModifyWindroses cmd = new(windroseLayer);
            cmd.RegisterNewWindrose(_currentWindrose);

            _commands.Execute(cmd);

            _currentWindrose = null;

            _redraw.RequestRedraw();
        }

        public void OnMouseWheel(PointerState state)
        {
            // no action
        }

        public void RenderOverlay(SKCanvas canvas, SKPoint world)
        {
            try
            {
                _currentWindrose?.Render(canvas);
            }
            catch { }
        }

        protected virtual void Dispose(bool disposing)
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
        // ~WindroseTool()
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
