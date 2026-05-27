using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.ViewModels.Panels;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace RealmStudioX.WPF.Editor.Tools
{
    public sealed class DrawingTool(
        CommandManager commands,
        IAssetProvider assets,
        EditorController editor,
        MapLayer targetLayer,
        MapScene scene,
        EditorState editorState,
        IDrawingSettings drawingSettings) : IToolEditor, IDisposable
    {
        private readonly CommandManager _commands = commands;
        private MapLayer _layer = targetLayer;
        private readonly IAssetProvider _assets = assets;
        private readonly EditorController _editor = editor;
        private readonly MapScene _scene = scene;
        private readonly EditorState _editorState = editorState;
        private readonly IDrawingSettings _drawingSettings = drawingSettings;

        private SKPoint _lastMouseWorld;

        private DrawnLine? _currentDrawnline = null;
        private PaintedLine? _currentPaintedLine = null;

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
            _lastMouseWorld = state.WorldPoint;

            bool ctrl = (state.Modifiers & InputModifiers.Control) == InputModifiers.Control;
            bool shift = (state.Modifiers & InputModifiers.Shift) == InputModifiers.Shift;

            if (state.Button == EditorMouseButton.Left)
            {
                switch (_editorState.CurrentDrawingMode)
                {
                    case MapDrawingMode.DrawingLine:
                        {
                            _currentDrawnline = new DrawnLine
                            {
                                BrushSize = (int)_drawingSettings.LineBrushSize,
                                Color = _drawingSettings.DrawingColor.ToSKColor()
                            };

                            _currentDrawnline.Points.Add(state.WorldPoint);
                        }
                        break;
                    case MapDrawingMode.DrawingPaint:
                        {
                            _currentPaintedLine = new PaintedLine
                            {
                                Brush = _drawingSettings.SelectedBrushPattern?.BrushDefinition,
                                BrushSize = _drawingSettings.LineBrushSize,
                                Color = _drawingSettings.DrawingColor.ToSKColor(),
                                FillType = _drawingSettings.SelectedShapeFillType,
                                StrokeBitmap = _drawingSettings.SelectedBrushPattern?.BrushDefinition?.BrushBitmap
                            };

                            _currentPaintedLine.Points.Add(state.WorldPoint);
                        }
                        break;
                }
            }
        }

        public void OnMouseMove(PointerState state)
        {
            bool ctrl = (state.Modifiers & InputModifiers.Control) == InputModifiers.Control;
            bool shift = (state.Modifiers & InputModifiers.Shift) == InputModifiers.Shift;

            if (state.Button == EditorMouseButton.Left)
            {
                switch (_editorState.CurrentDrawingMode)
                {
                    case MapDrawingMode.DrawingLine:
                        {
                            _currentDrawnline?.Points.Add(state.WorldPoint);
                        }
                        break;
                    case MapDrawingMode.DrawingPaint:
                        {
                            _currentPaintedLine?.Points.Add(state.WorldPoint);
                        }
                        break;
                }
            }

            _lastMouseWorld = state.WorldPoint;
        }

        public void OnMouseUp(PointerState state)
        {
            if (state.Button == EditorMouseButton.Left)
            {
                MapLayer drawLayer = _editor.ActiveDrawingLayer != null ?
                        _editor.ActiveDrawingLayer : MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.DRAWINGLAYER);

                switch (_editorState.CurrentDrawingMode)
                {
                    case MapDrawingMode.DrawingLine:
                        {
                            if (_currentDrawnline != null)
                            {
                                _currentDrawnline.Points.Add(state.WorldPoint);

                                drawLayer.Add(_currentDrawnline);

                                _currentDrawnline = null;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingPaint:
                        {
                            if (_currentPaintedLine != null)
                            {
                                _currentPaintedLine.Points.Add(state.WorldPoint);

                                drawLayer.Add(_currentPaintedLine);

                                _currentPaintedLine = null;
                            }
                        }
                        break;
                }
            }

            _lastMouseWorld = state.WorldPoint;
        }

        public void OnMouseDoubleClick(PointerState state)
        {
            // no action
        }

        public void OnMouseWheel(PointerState state)
        {
            // no action
        }



        public void RenderOverlay(SKCanvas canvas, SKPoint world)
        {
            if (_currentDrawnline != null)
            {
                _currentDrawnline.Render(canvas);
            }

            if (_currentPaintedLine != null)
            {
                _currentPaintedLine.Render(canvas);
            }

            if (_editorState.CurrentDrawingMode == MapDrawingMode.DrawingPaint)
            {
                var brushRadius = _drawingSettings.LineBrushSize / 2;

                canvas.DrawCircle(
                    world,
                    brushRadius,
                    PaintObjects.CursorCirclePaint);
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
