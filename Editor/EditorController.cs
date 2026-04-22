using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using SkiaSharp;
using static RealmStudioX.WPF.MainWindow;

namespace RealmStudioX.WPF.Editor
{
    public class EditorController
    {
        public event Action<MapDrawingMode>? DrawingModeChanged;
        public event Action<ColorPaintBrush>? ColorPaintBrushChanged;
        public event Action<MapLayer>? ActiveDrawingLayerChanged;

        public CommandManager Commands { get; } = new();

        public MapScene? Scene { get; set; }

        private IToolEditor? _activeTool;

        private SKSize _viewportSize;

        public event Action? RedrawRequested;

        public void RequestRedraw()
        {
            RedrawRequested?.Invoke();
        }

        public IToolEditor? ActiveEditorTool
        {
            get { return _activeTool; }
            set { _activeTool = value; }
        }

        private MapDrawingMode _currentDrawingMode = MapDrawingMode.None;

        public MapDrawingMode CurrentDrawingMode
        {
            get { return _currentDrawingMode; }
            private set
            {
                if (_currentDrawingMode != value)
                {
                    _currentDrawingMode = value;
                    DrawingModeChanged?.Invoke(_currentDrawingMode);
                }
            }
        }

        private ColorPaintBrush _colorPaintBrush = ColorPaintBrush.None;

        public ColorPaintBrush SelectedColorPaintBrush
        {
            get { return _colorPaintBrush; }
            private set
            {
                if (_colorPaintBrush != value)
                {
                    _colorPaintBrush = value;
                    ColorPaintBrushChanged?.Invoke(_colorPaintBrush);
                }
            }
        }

        public void SetDrawingMode(MapDrawingMode mode)
        {
            CurrentDrawingMode = mode;
        }

        public void SetColorPaintBrush(ColorPaintBrush brush)
        {
            SelectedColorPaintBrush = brush;
        }

        private MapLayer? _activeDrawingLayer;

        public MapLayer? ActiveDrawingLayer
        {
            get { return _activeDrawingLayer; }
            private set
            {
                if (_activeDrawingLayer != value)
                {
                    _activeDrawingLayer = value;
                    ActiveDrawingLayerChanged?.Invoke(_activeDrawingLayer!);
                }
            }
        }

        public void SetActiveDrawingLayer(MapLayer layer)
        {
            ActiveDrawingLayer = layer;
        }

        public event Action<PointerState>? MouseMoved;

        public void NotifyMouseMoved(PointerState state)
        {
            MouseMoved?.Invoke(state);
        }

        public event Action<PointerState>? MouseDown;

        public void NotifyMouseDown(PointerState state)
        {
            MouseDown?.Invoke(state);
        }

        public event Action<PointerState>? MouseUp;

        public void NotifyMouseUp(PointerState state)
        {
            MouseUp?.Invoke(state);
        }

        public event Action<PointerState>? MouseDoubleClick;

        public void NotifyMouseDoubleClick(PointerState state)
        {
            MouseDoubleClick?.Invoke(state);
        }

        // ---------------------------------------------
        // Camera helpers
        // ---------------------------------------------

        public void SetViewportSize(SKSize size)
        {
            _viewportSize = size;

            // Camera constraints depend on viewport size
            ClampCamera();
            RequestRedraw();
        }

        private SKPoint _lastPanScreen;

        private void BeginPan(SKPoint screen)
        {
            if (Scene == null)
                return;

            Scene.Camera.IsPanning = true;
            _lastPanScreen = screen;
            Scene.Camera.LastMouseMoveTime = DateTime.UtcNow;
        }

        private void UpdatePan(SKPoint screen)
        {
            if (Scene == null || !Scene.Camera.IsPanning)
                return;

            var delta = new SKPoint(
                screen.X - _lastPanScreen.X,
                screen.Y - _lastPanScreen.Y);

            Scene.Camera.PanBy(delta, _viewportSize.Width, _viewportSize.Height);

            var now = DateTime.UtcNow;
            float dt = (float)(now - Scene.Camera.LastMouseMoveTime).TotalSeconds;
            if (dt > 0)
            {
                Scene.Camera.AddVelocity(
                    new SKPoint(delta.X / dt, delta.Y / dt));
            }

            Scene.Camera.LastMouseMoveTime = now;
            _lastPanScreen = screen;
        }

        private void EndPan(MouseButtons button)
        {
            if (Scene == null)
                return;

            if (button == MouseButtons.Middle)
            {
                Scene.Camera.IsPanning = false;
            }
        }

        public void ResetCamera()
        {
            if (Scene == null)
                return;

            Scene.Camera.Reset(_viewportSize.Width, _viewportSize.Height);
            ClampCamera();

            RequestRedraw();
        }

        public void ClampCamera()
        {
            if (Scene == null)
                return;

            Scene.Camera.ClampToWorld(
                new SKRect(0, 0,
                    Scene.Map.MapWidth,
                    Scene.Map.MapHeight),
                _viewportSize);
        }

        // ---------------------------------------------
        // Coordinate transforms
        // ---------------------------------------------

        public SKPoint ScreenToWorld(SKPoint screen)
        {
            var cam = Scene?.Camera;

            if (cam == null)
                return screen;

            return new SKPoint(
                (screen.X - cam.Pan.X) / cam.Zoom,
                (screen.Y - cam.Pan.Y) / cam.Zoom);
        }
    }
}
