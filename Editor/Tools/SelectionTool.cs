using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.Editor.Services;
using SkiaSharp;
using System.Windows.Input;

namespace RealmStudioX.WPF.Editor.Tools
{
    internal class SelectionTool(EditorController editor, SelectionService selectionService) : IToolEditor, IDisposable
    {
        private EditorController _editor = editor;
        private SelectionService _selectionService = selectionService;

        private SKPoint _initialMousePoint = SKPoint.Empty;
        
        private SKRect _selectedRealmArea = SKRect.Empty;

        private SKRect _selectedArea = SKRect.Empty;

        public SKRect SelectedArea
        {
            get { return _selectedArea; }
            set { _selectedArea = value; }
        }

        private List<SKPoint> _lassoPoints = [];
        private bool disposedValue;

        public void Activate()
        {
            _selectedArea = SKRect.Empty;
            _selectedRealmArea = SKRect.Empty;
        }

        public void Deactivate()
        {

        }

        public void OnMouseDown(PointerState state)
        {
            if (_editor.Scene != null)
            {
                Mouse.OverrideCursor = null;

                if (_editor.CurrentDrawingMode == MapDrawingMode.ShapeSelect)
                {
                    if (state.Modifiers == InputModifiers.Control)
                    {
                        _selectionService.SelectForLayout(_editor, state.WorldPoint, 4);
                        return;
                    }

                    _selectionService.SelectAt(_editor.Scene.Map, state.WorldPoint, 4, state.Modifiers == InputModifiers.Shift);
                }
                else if (_editor.CurrentDrawingMode == MapDrawingMode.RealmAreaSelect && state.Button == EditorMouseButton.Left)
                {
                    _initialMousePoint = state.WorldPoint;
                }
                else if (_editor.CurrentDrawingMode == MapDrawingMode.RealmLassoSelect && state.Button == EditorMouseButton.Left)
                {
                    _lassoPoints.Add(state.WorldPoint);
                }
                else if (_editor.CurrentDrawingMode == MapDrawingMode.AreaSelection && state.Button == EditorMouseButton.Left)
                {
                    _initialMousePoint = state.WorldPoint;
                }
            }            
        }

        public void OnMouseMove(PointerState state)
        {
            if (_editor.CurrentDrawingMode == MapDrawingMode.RealmAreaSelect && state.Button == EditorMouseButton.Left)
            {
                _selectedRealmArea = new(_initialMousePoint.X, _initialMousePoint.Y, state.WorldPoint.X, state.WorldPoint.Y);                
            }
            else if (_editor.CurrentDrawingMode == MapDrawingMode.RealmLassoSelect && state.Button == EditorMouseButton.Left)
            {
                _lassoPoints.Add(state.WorldPoint);
            }
            else if (_editor.CurrentDrawingMode == MapDrawingMode.AreaSelection && state.Button == EditorMouseButton.Left)
            {
                _selectedArea = new(_initialMousePoint.X, _initialMousePoint.Y, state.WorldPoint.X, state.WorldPoint.Y);
            }

            _editor.RequestRedraw();
        }

        public void OnMouseUp(PointerState state)
        {
            if (_editor.CurrentDrawingMode == MapDrawingMode.RealmAreaSelect && state.Button == EditorMouseButton.Left)
            {
                _selectedRealmArea = new(_initialMousePoint.X, _initialMousePoint.Y, state.WorldPoint.X, state.WorldPoint.Y);

                _selectionService.SelectObjectsInArea(_editor.Scene!.Map, _selectedRealmArea);

                _selectedRealmArea = SKRect.Empty;

                _initialMousePoint = SKPoint.Empty;

                _editor.RequestRedraw();
            }

            if (_editor.CurrentDrawingMode == MapDrawingMode.RealmLassoSelect && state.Button == EditorMouseButton.Left)
            {
                using SKPath lassoPath = Utilities.BuildClosedPath(_lassoPoints);

                _selectionService.SelectObjectsInPath(_editor.Scene!.Map, lassoPath);

                _lassoPoints.Clear();

                _editor.RequestRedraw();
            }

            if (_editor.CurrentDrawingMode == MapDrawingMode.AreaSelection && state.Button == EditorMouseButton.Left)
            {
                _selectedArea = new(_initialMousePoint.X, _initialMousePoint.Y, state.WorldPoint.X, state.WorldPoint.Y);

                _initialMousePoint = SKPoint.Empty;

                _editor.RequestRedraw();
            }
        }

        public void OnMouseDoubleClick(PointerState state)
        {
            if (_editor.Scene != null)
            {
                Mouse.OverrideCursor = null;

                if (_editor.CurrentDrawingMode == MapDrawingMode.ShapeSelect)
                {
                    _selectionService.SelectAt(_editor.Scene.Map, state.WorldPoint, 4, false);
                }
            }
        }

        public void OnMouseWheel(PointerState state)
        {

        }

        public void Cancel()
        {

        }

        public void RenderOverlay(SKCanvas canvas, SKPoint world)
        {
            if (_editor.CurrentDrawingMode == MapDrawingMode.RealmAreaSelect && _selectedRealmArea != SKRect.Empty)
            {
                canvas.DrawRect(_selectedRealmArea, PaintObjects.LandformAreaSelectPaint);
                return;
            }

            if (_editor.CurrentDrawingMode == MapDrawingMode.RealmLassoSelect && _lassoPoints.Count > 3)
            {
                using SKPath lassoPath = Utilities.BuildClosedPath(_lassoPoints);
                canvas.DrawPath(lassoPath, PaintObjects.LandformAreaSelectPaint);
                return;
            }

            if (_editor.CurrentDrawingMode == MapDrawingMode.AreaSelection && _selectedArea != SKRect.Empty)
            {
                canvas.DrawRect(_selectedArea, PaintObjects.AreaSelectionPaint);
                return;
            }
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
        // ~SelectionTool()
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
