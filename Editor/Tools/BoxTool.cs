using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.ViewModels.Panels;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace RealmStudioX.WPF.Editor.Tools
{
    internal class BoxTool(
            CommandManager commands,
            IAssetProvider assets,
            MapLayer targetLayer,
            MapScene scene,
            EditorState editorState,
            FontManager fontManager,
            IRedrawRequester redraw,
            IBoxSettings settings) : IToolEditor, IDisposable
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
        private readonly IBoxSettings _settings = settings;

        private PlacedMapBox? _currentPlacedBox = null;

        SKPoint _dragStart = SKPoint.Empty;
        private bool _isDraggingBox;

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
            if (_settings.SelectedBox == null || _settings.SelectedBox.BoxDefinition.BoxBitmap == null)
            {
                return;
            }

            _dragStart = state.WorldPoint;
            _isDraggingBox = true;

            _currentPlacedBox = new()
            {
                TopLeft = _dragStart,
                BottomRight = _dragStart,

                BaseBox =
                    _settings.SelectedBox.BoxDefinition,

                BoxBitmap =
                    _settings.SelectedBox
                        .BoxDefinition
                        .BoxBitmap
                        .Copy(),

                BoxCenterLeft =
                    _settings.SelectedBox
                        .BoxDefinition
                        .BoxCenterLeft,

                BoxCenterTop =
                    _settings.SelectedBox
                        .BoxDefinition
                        .BoxCenterTop,

                BoxCenterRight =
                    _settings.SelectedBox
                        .BoxDefinition
                        .BoxCenterRight,

                BoxCenterBottom =
                    _settings.SelectedBox
                        .BoxDefinition
                        .BoxCenterBottom,

                BoxTint =
                    _settings.BoxTint.ToSKColor()
            };
        }

        public void OnMouseMove(PointerState state)
        {
            if (_currentPlacedBox == null || _currentPlacedBox.BoxBitmap == null || !_isDraggingBox)
            {
                return;
            }

            _currentPlacedBox.BottomRight = state.WorldPoint;

            _redraw.RequestRedraw();
        }

        public void OnMouseUp(PointerState state)
        {
            if (!_isDraggingBox || _currentPlacedBox == null || _currentPlacedBox.BoxBitmap == null)
                return;

            _isDraggingBox = false;

            if (_currentPlacedBox.Size.Width > 4 && _currentPlacedBox.Size.Height > 4)
            {
                MapLayer boxLayer = MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.BOXLAYER);
                PlacedMapBox box = new(_currentPlacedBox);

                Cmd_ModifyBoxes cmd = new(boxLayer);
                cmd.RegisterNewBox(box);

                _commands.Execute(cmd);
            }

            _currentPlacedBox = null;

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
                // the box center can be outside the bounds of the bitmap if
                // the box is drawn to be very narrow in height or width
                if (_currentPlacedBox != null && _currentPlacedBox.BoxBitmap != null)
                {
                    SKPaint boxPaint = new()
                    {
                        Style = SKPaintStyle.Fill,
                        ColorFilter = SKColorFilter.CreateBlendMode(_currentPlacedBox.BoxTint,
                            SKBlendMode.Modulate) // combine the tint with the bitmap color
                    };

                    canvas.DrawBitmapNinePatch(
                        _currentPlacedBox.BoxBitmap,

                        new SKRectI(
                            (int)_currentPlacedBox.BoxCenterLeft,
                            (int)_currentPlacedBox.BoxCenterTop,
                            (int)_currentPlacedBox.BoxCenterRight,
                            (int)_currentPlacedBox.BoxCenterBottom),

                        new SKRect(
                            _currentPlacedBox.TopLeft.X,
                            _currentPlacedBox.TopLeft.Y,
                            _currentPlacedBox.BottomRight.X,
                            _currentPlacedBox.BottomRight.Y),

                        boxPaint);
                }
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
        // ~BoxTool()
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
