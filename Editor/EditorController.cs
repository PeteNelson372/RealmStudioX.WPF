using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor.Tools;
using SkiaSharp;
using CommandManager = RealmStudioX.Core.CommandManager;

namespace RealmStudioX.WPF.Editor
{
    public class EditorController
    {
        public event Action<MapDrawingMode>? DrawingModeChanged;
        public event Action<ColorPaintBrush>? ColorPaintBrushChanged;
        public event Action<MapLayer>? ActiveDrawingLayerChanged;

        public CommandManager _commands { get; } = new();
        private readonly AssetManager _assetManager;
        private readonly EditorState _editorState = new();

        private MapScene? _scene;

        private ToolFactory? _toolFactory;
        private IToolEditor? _activeTool;

        private SKSize _viewportSize;

        public EditorController(AssetManager assetManager)
        {
            _assetManager = assetManager;
        }

        public void ActivateTool(EditorToolType type, object? context = null)
        {
            if (_toolFactory == null)
            {
                return;
            }
          
            ActiveEditorTool = _toolFactory.Create(type, context);
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

            _toolFactory = new(_commands, _assetManager, _scene, _editorState);

            // Subscribe to new scene
            _scene.SceneChanged += OnSceneChanged;
        }

        private void OnSceneChanged()
        {
            if (_scene == null)
            {
                return;
            }

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

        public MapDrawingMode CurrentDrawingMode
        {
            get { return _editorState.CurrentDrawingMode; }
            private set
            {
                if (_editorState.CurrentDrawingMode != value)
                {
                    _editorState.CurrentDrawingMode = value;
                    DrawingModeChanged?.Invoke(_editorState.CurrentDrawingMode);
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

            if (state.Button == EditorMouseButton.Left)
            {

            }

            if (state.Button == EditorMouseButton.Middle)
            {
                BeginPan(state.ScreenPoint);
                return;
            }

            if (state.Button == EditorMouseButton.Right)
            {

            }

            ActiveEditorTool?.OnMouseDown(state);
        }

        internal void OnMouseMove(PointerState state)
        {
            if (Scene == null)
            {
                return;
            }

            Scene.Camera.CurrentMouseLocation = state.ScreenPoint;
            Scene.Camera.CurrentCursorPoint = state.WorldPoint;

            if (state.Button == EditorMouseButton.Left)
            {

            }

            if (state.Button == EditorMouseButton.Middle)
            {
                UpdatePan(state.ScreenPoint);
                return;
            }

            if (state.Button == EditorMouseButton.Right)
            {

            }

            ActiveEditorTool?.OnMouseMove(state);
        }

        internal void OnMouseUp(PointerState state)
        {
            if (Scene == null)
            {
                return;
            }

            if (state.Button == EditorMouseButton.Left)
            {

            }

            if (state.Button == EditorMouseButton.Middle)
            {
                EndPan();
            }

            if (state.Button == EditorMouseButton.Right)
            {

            }

            ActiveEditorTool?.OnMouseUp(state);
        }

        internal void OnMouseDoubleClick(PointerState state)
        {
            ActiveEditorTool?.OnMouseDoubleClick(state);
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

    public enum EditorToolType
    {
        // TODO: add other tools as they are implemented
        OceanTool,
        LandformTool,
        LabelTool,
        MapPathTool,
        PaintedShapeTool,
        SymbolTool,
        WaterBodyTool
    }
}
