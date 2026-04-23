using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using SkiaSharp;
using CommandManager = RealmStudioX.Core.CommandManager;

namespace RealmStudioX.WPF.Editor
{
    public class EditorController
    {
        public event Action<MapDrawingMode>? DrawingModeChanged;
        public event Action<ColorPaintBrush>? ColorPaintBrushChanged;
        public event Action<MapLayer>? ActiveDrawingLayerChanged;

        public CommandManager Commands { get; } = new();
        private readonly AssetManager _assetManager;

        private MapScene? _scene;

        private IToolEditor? _activeTool;

        private SKSize _viewportSize;

        public EditorController(AssetManager assetManager)
        {
            _assetManager = assetManager;
        }

        public MapScene? Scene => _scene;

        public void SetScene(MapScene scene)
        {
            // Unsubscribe from old scene (if any)
            if (_scene != null)
            {
                _scene.SceneChanged -= OnSceneChanged;
            }

            _scene = scene;

            // Subscribe to new scene
            _scene.SceneChanged += OnSceneChanged;
        }

        private void OnSceneChanged()
        {
            UpdateMapScene();
        }

        public event Action? MapSceneChanged;

        public void UpdateMapScene()
        {
            MapSceneChanged?.Invoke();
        }

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
        // Render Overlay
        // ---------------------------------------------

        public void RenderOverlay(SKCanvas canvas)
        {
            ArgumentNullException.ThrowIfNull(Scene, nameof(Scene));

            var world = Scene.Camera.CurrentCursorPoint;
            ActiveEditorTool?.RenderOverlay(canvas, world);

            canvas.DrawRect(new SKRect(0, 0, Scene.Map.MapWidth, Scene.Map.MapHeight), PaintObjects.MapBoundaryPaint);
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

        public void ResetCamera()
        {
            if (_scene == null)
                return;

            _scene.Camera.Reset(_viewportSize.Width, _viewportSize.Height);
            ClampCamera();

            RequestRedraw();
        }

        public void ClampCamera()
        {
            if (_scene == null)
                return;

            _scene.Camera.ClampToWorld(
                new SKRect(0, 0,
                    _scene.Map.MapWidth,
                    _scene.Map.MapHeight),
                _viewportSize);
        }

        public void ZoomAt(SKPoint screenPoint, int delta)
        {
            if (Scene == null)
            {
                return;
            }

            float factor = delta > 0 ? 1.1f : 0.9f;

            float newZoom = Scene.Camera.Zoom * factor;

            Scene.Camera.ZoomAtScreenPoint(
                newZoom,
                screenPoint,
                _viewportSize.Width,
                _viewportSize.Height);

            OnSceneChanged();
        }

        private SKPoint _lastPanScreen;

        private void BeginPan(SKPoint screen)
        {
            if (Scene == null)
            {
                return;
            }

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

        private void EndPan()
        {
            if (Scene == null)
            {
                return;
            }

            Scene.Camera.IsPanning = false;
        }

        // ---------------------------------------------
        // Coordinate transforms
        // ---------------------------------------------

        public SKPoint ScreenToWorld(SKPoint screen)
        {
            var cam = _scene?.Camera;

            if (cam == null)
                return screen;

            return new SKPoint(
                (screen.X - cam.Pan.X) / cam.Zoom,
                (screen.Y - cam.Pan.Y) / cam.Zoom);
        }


        // ---------------------------------------------
        // Mouse interaction
        // ---------------------------------------------

        internal void OnMouseDown(PointerState state)
        {
            if (Scene == null)
            {
                return;
            }

            if (state.Button == EditorMouseButton.Middle)
            {
                BeginPan(state.ScreenPoint);
                return;
            }
        }

        internal void OnMouseMove(PointerState state)
        {
            if (Scene == null)
            {
                return;
            }

            if (state.Button == EditorMouseButton.Middle)
            {
                UpdatePan(state.ScreenPoint);
                return;
            }
        }

        internal void OnMouseUp(PointerState state)
        {
            if (Scene == null)
            {
                return;
            }

            if (state.Button == EditorMouseButton.Middle)
            {
                EndPan();
            }
        }

        internal void OnMouseDoubleClick(PointerState state)
        {

        }

        internal void OnMouseWheel(PointerState state)
        {
            if (Scene?.Camera == null)
                return;

            // 1. Navigation (highest priority)
            if ((state.Modifiers & InputModifiers.Control) == InputModifiers.Control)
            {
                ZoomAt(state.ScreenPoint, state.WheelDelta);
                return;
            }

            // 2. Let active tool handle it
            if (ActiveEditorTool != null)
            {
                ActiveEditorTool.OnMouseWheel(state);
            }
        }

        // -------------------------------------------------
        // Background
        // -------------------------------------------------

        public void FillBackground(TextureFillRequest request)
        {
            ArgumentNullException.ThrowIfNull(Scene, nameof(Scene));

            if (request.TextureId == null)
            {
                return;
            }

            Scene.Map.Background.TextureId = request.TextureId;
            Scene.Map.Background.Scale = request.Scale;
            Scene.Map.Background.Mirror = request.Mirror;

            var image = (_assetManager).GetImage(request.TextureId);
            Scene.SetBackgroundTexture(image);
            Scene.MarkBackgroundModified();

            RequestRedraw();
        }

        public void ClearBackground()
        {
            ArgumentNullException.ThrowIfNull(Scene, nameof(Scene));

            Scene.Map.Background.TextureId = null;
            Scene.SetBackgroundTexture(null);
            Scene.MarkBackgroundModified();

            RequestRedraw();
        }

        public void UpdateBackgroundPreview(TextureFillRequest request)
        {
            if (Scene?.Map == null || request.TextureId == null)
                return;

            Scene.Map.Background.TextureId = request.TextureId;
            Scene.Map.Background.Scale = request.Scale;
            Scene.Map.Background.Mirror = request.Mirror;

            var image = (_assetManager).GetImage(request.TextureId);
            Scene.SetBackgroundTexture(image);
            Scene.MarkBackgroundModified();

            OnSceneChanged();
        }
    }
}
