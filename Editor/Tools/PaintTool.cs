using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.Editor.Services;
using SkiaSharp;

namespace RealmStudioX.WPF.Editor.Tools
{
    internal class PaintTool(EditorController editor, PaintService paintService, IRedrawRequester redraw) : IToolEditor, IDisposable
    {

        private bool disposedValue;

        // -------------------------------------------------
        // Dependencies
        // -------------------------------------------------

        private readonly EditorController _editor = editor;
        private readonly PaintService _paintService = paintService;
        private readonly IRedrawRequester _redraw = redraw;

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
            if (_editor.CurrentDrawingMode == MapDrawingMode.OceanErase
                || _editor.CurrentDrawingMode == MapDrawingMode.LandErase
                || _editor.CurrentDrawingMode == MapDrawingMode.WaterColorErase)
            {
                _paintService.BeginErase(state.WorldPoint);
            }
            else
            {
                _paintService.BeginStroke(state.WorldPoint);
            }
        }

        public void OnMouseMove(PointerState state)
        {
            if (_editor.CurrentDrawingMode == MapDrawingMode.OceanErase
                || _editor.CurrentDrawingMode == MapDrawingMode.LandErase
                || _editor.CurrentDrawingMode == MapDrawingMode.WaterColorErase)
            {
                _paintService.ContinueErase(state.WorldPoint);
            }
            else
            {
                _paintService.ContinueStroke(state.WorldPoint);
            }
            _redraw.RequestRedraw();
        }

        public void OnMouseUp(PointerState state)
        {
            if (_editor.CurrentDrawingMode == MapDrawingMode.OceanErase
                || _editor.CurrentDrawingMode == MapDrawingMode.LandErase
                || _editor.CurrentDrawingMode == MapDrawingMode.WaterColorErase)
            {
                _paintService.EndErase(state.WorldPoint);
            }
            else
            {
                _paintService.EndStroke(state.WorldPoint);
            }

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
                _paintService.RenderCurrentLine(canvas);

                var brushRadius = _paintService.Settings.BrushSize / 2;

                canvas.DrawCircle(
                    world,
                    brushRadius,
                    PaintObjects.CursorCirclePaint);

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
