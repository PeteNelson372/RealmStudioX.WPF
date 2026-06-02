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
        private  IDrawingSettings _drawingSettings = drawingSettings;

        private SKPoint _lastMouseWorld;
        private long _lastPaintTimestamp;

        private DrawnLine? _currentDrawnline = null;
        private PaintedLine? _currentPaintedLine = null;
        private PreparedBrush? _currentPreparedBrush = null;

        public PreparedBrush? CurrentPreparedBrush
        {
            get { return _currentPreparedBrush; } 
            set { _currentPreparedBrush = value; }
        }

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
                            if (_drawingSettings.SelectedBrushPattern != null
                                && _drawingSettings.SelectedBrushPattern.BrushDefinition != null)
                            {
                                if (_currentPreparedBrush == null)
                                {
                                    // this will only happen if the user starts painting
                                    // without changing brush type, size, or color
                                    _currentPreparedBrush = new PreparedBrush()
                                    {
                                        SourceBrush = _drawingSettings.SelectedBrushPattern.BrushDefinition,
                                        Color = _drawingSettings.DrawingColor.ToSKColor(),
                                        BrushSize = (int)_drawingSettings.LineBrushSize,
                                        BrushSpacing = _drawingSettings.BrushSpacing,
                                    };

                                    DrawingPanelViewModel.GetPreparedBrushBitmaps(_currentPreparedBrush);
                                    CurrentPreparedBrush = _currentPreparedBrush;
                                }

                                _currentPaintedLine = new PaintedLine
                                {
                                    Brush = _currentPreparedBrush,
                                    DefaultSpacing = _drawingSettings.SelectedBrushPattern.BrushDefinition.BrushSpacing,
                                    BrushSpacing = _drawingSettings.BrushSpacing,
                                    RandomRotation = _drawingSettings.SelectedBrushPattern.BrushDefinition.RandomRotation,
                                };

                                _currentPaintedLine.Initialize(_editor.Scene!.Map.MapWidth, _editor.Scene!.Map.MapHeight);

                                long now = Environment.TickCount64;

                                _lastPaintTimestamp = now;

                                _currentPaintedLine.AddPoint(state.WorldPoint);
                            }
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
                            if (_currentPaintedLine != null)
                            {
                                long now = Environment.TickCount64;

                                float deltaTime = (now - _lastPaintTimestamp) / 1000f;

                                _lastPaintTimestamp = now;

                                _currentPaintedLine.AddPoint(state.WorldPoint);
                            }
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

                                if (_editor.ActiveDrawingLayer != null)
                                {
                                    Cmd_AddDrawnLine cmd = new(_editor.ActiveDrawingLayer, _currentDrawnline);
                                    _commands.Execute(cmd);
                                }

                                _currentDrawnline = null;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingPaint:
                        {
                            if (_currentPaintedLine != null)
                            {
                                long now = Environment.TickCount64;

                                float deltaTime = (now - _lastPaintTimestamp) / 1000f;

                                _lastPaintTimestamp = now;

                                if (state.WorldPoint != _currentPaintedLine.Points[^1])
                                {
                                    _currentPaintedLine.AddPoint(state.WorldPoint);
                                }

                                _currentPaintedLine.FinalizeStroke();

                                if (_editor.ActiveDrawingLayer != null)
                                {
                                    Cmd_AddPaintedLine cmd = new(_editor.ActiveDrawingLayer, _currentPaintedLine);
                                    _commands.Execute(cmd);
                                }

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

        public void UpdateDrawingParameters(IDrawingSettings newSettings)
        {
            _drawingSettings = newSettings;
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
