using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor.Services;
using RealmStudioX.WPF.Editor.Tools;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Controls;
using RealmStudioX.WPF.ViewModels.Panels;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using CommandManager = RealmStudioX.Core.CommandManager;

namespace RealmStudioX.WPF.Editor
{
    public class EditorController : IRedrawRequester
    {
        public event Action<MapDrawingMode>? DrawingModeChanged;
        public event Action<MapLayer>? ActiveDrawingLayerChanged;

        private CommandService? _commandService;
        private SelectionService? _selectionService;
        private PaintService? _paintService;

        private CommandManager _commands { get; } = new();
        public CommandManager Commands => _commands;

        public SelectionService? SelectionService => _selectionService;

        private readonly AssetManager _assetManager;
        private readonly FontManager _fontManager;
        
        private readonly EditorState _editorState = new();

        public EditorState State => _editorState;

        private MapScene? _scene;

        private ToolFactory? _toolFactory;
        private IToolEditor? _activeTool;

        private readonly SymbolSelectionService _symbolSelectionService = new();
        public SymbolSelectionService SymbolSelectionService => _symbolSelectionService;

        private SKSize _viewportSize;

        // -------------------------------------------------
        // PolylineEditor and TransformWidget handle dragging undo/redo support
        // -------------------------------------------------

        private Cmd_ModifyWaterBodies? _activeModifyWaterBodyCommand;
        private Cmd_ModifyMapPaths? _activeModifyMapPathCommand;
        private Cmd_ModifySymbol? _activeModifyMapSymbolCommand;
        private Cmd_ModifyLabel? _activeModifyLabelCommand;
        private Cmd_ModifyBox? _activeModifyBoxCommand;

        private bool _isTransforming;

        // -------------------------------------------------
        // MapSymbol arrow key nudging
        // -------------------------------------------------

        private Cmd_ModifySymbol? _activeSymbolNudgeCommand;
        private MapSymbol? _nudgeSymbol;
        private Keys _activeSymbolNudgeKey;

        // -------------------------------------------------
        // MapLabel arrow key nudging
        // -------------------------------------------------

        private Cmd_ModifyLabel? _activeLabelNudgeCommand;
        private MapLabel? _nudgeLabel;
        private Keys _activeLabelNudgeKey;

        // -------------------------------------------------
        // PlacedMapBox arrow key nudging
        // -------------------------------------------------

        private Cmd_ModifyBox? _activeBoxNudgeCommand;
        private PlacedMapBox? _nudgeBox;
        private Keys _activeBoxNudgeKey;

        // -------------------------------------------------
        // Shape dragging
        // -------------------------------------------------

        private MapComponent2D? _dragShape;
        private SKPoint _dragStartWorld;
        private SKPath? _dragOriginalGeometry;
        private bool _isDragging;

        private PointerState previousPointerState;

        public EditorController(AssetManager assetManager, FontManager fontManager)
        {
            _assetManager = assetManager;
            _fontManager = fontManager;

            _editorState.DrawingModeChanged += OnDrawingModeChanged;            
        }

        public void SetCommandService(CommandService commandService)
        {
            _commandService = commandService;
        }

        public void MarkMapModified()
        {
            _commandService?.MarkMapModified();
        }

        public void SetSelectionService(SelectionService selectionService)
        {
            _selectionService = selectionService;
        }

        public void SetPaintService(PaintService paintService)
        {
            _paintService = paintService;
        }

        public void Reset()
        {
            SetDrawingMode(MapDrawingMode.None);

            CommitSymbolNudge();
            CommitLabelNudge();
            CommitBoxNudge();

            FinalizeOpenCommands();

            _isDragging = false;
            _dragShape = null;
            _dragStartWorld = SKPoint.Empty;
            _isTransforming = false;

            _activeSymbolNudgeCommand = null;
            _nudgeSymbol = null;
            _activeSymbolNudgeKey = Keys.None;

            _activeLabelNudgeCommand = null;
            _nudgeLabel = null;
            _activeLabelNudgeKey = Keys.None;

            ActiveEditorTool?.Deactivate();

            ActiveEditorTool = null;

            ResetCamera();

            SelectionService!.ClearSelection();

            if (Scene != null)
            {
                Scene.TransformWidget.Target = null;
            }
        }

        public IToolEditor? ActivateTool(EditorToolType type, object? context = null)
        {
            if (_toolFactory == null)
            {
                return null;
            }

            if (ActiveEditorTool != null)
            {
                ActiveEditorTool.Deactivate();
            }

            ActiveEditorTool = _toolFactory.Create(type, context);

            ActiveEditorTool?.Activate();

            return ActiveEditorTool;
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

            _toolFactory = new(_commands, _assetManager, _scene, _editorState, this, _fontManager, _selectionService!, _paintService!, _scene.RenderContext);

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

        private void OnDrawingModeChanged(MapDrawingMode previous, MapDrawingMode current)
        {
            // Example:
            // - switch active tool
            // - update UI

            CommitSymbolNudge();
            CommitLabelNudge();

            if (ActiveEditorTool is LabelTool tool)
            {
                tool.EnsureEditCommitted();
            }

            FinalizeOpenCommands();

            SelectionService!.ClearSelection();
        }

        private void FinalizeOpenCommands()
        {
            if (_activeModifyMapSymbolCommand != null)
            {
                _activeModifyMapSymbolCommand.CaptureAfter();
                _commands.Execute(_activeModifyMapSymbolCommand);

                _activeModifyMapSymbolCommand = null;
                _isTransforming = false;
                _isDragging = false;
            }

            if (_activeModifyLabelCommand != null)
            {
                _activeModifyLabelCommand.CaptureAfter();
                _commands.Execute(_activeModifyLabelCommand);

                _activeModifyLabelCommand = null;
                _isTransforming = false;
                _isDragging = false;
            }

            if (_activeModifyBoxCommand != null)
            {
                _activeModifyBoxCommand.CaptureAfter();
                _commands.Execute(_activeModifyBoxCommand);

                _activeModifyBoxCommand = null;
                _isTransforming = false;
                _isDragging = false;
            }

            // if other active commands are left open (not null when editor state changes)
            // discard the command
            _activeModifyWaterBodyCommand = null;
            _activeModifyMapPathCommand = null;
        }

        public void SetDrawingMode(MapDrawingMode mode)
        {
            CurrentDrawingMode = mode;
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

            previousPointerState = state;

            if (state.Button == EditorMouseButton.Left)
            {
                if ((_editorState.CurrentDrawingMode == MapDrawingMode.RealmAreaSelect
                    || _editorState.CurrentDrawingMode == MapDrawingMode.RealmLassoSelect)
                    && ActiveEditorTool is SelectionTool st)
                {
                    st.OnMouseDown(state);
                    return;
                }

                if (_selectionService!.PrimarySelection is MapSymbol ms
                    && _editorState.CurrentDrawingMode == MapDrawingMode.ShapeSelect)
                {
                    SelectedMapSymbolMouseDown(ms, state.WorldPoint);
                    return;
                }

                if (_selectionService!.PrimarySelection is MapLabel ml
                    && _editorState.CurrentDrawingMode == MapDrawingMode.ShapeSelect)
                {
                    SelectedMapLabelMouseDown(ml, state.WorldPoint);
                    return;
                }

                if (_selectionService!.PrimarySelection is PlacedMapBox pmb
                    && _editorState.CurrentDrawingMode == MapDrawingMode.ShapeSelect)
                {
                    SelectedMapBoxMouseDown(pmb, state.WorldPoint);
                    return;
                }

                if (_selectionService!.PrimarySelection is MapRegion region)
                {
                    ActivateTool(EditorToolType.RegionTool);
                    ActiveEditorTool?.OnMouseDown(state);

                    return;
                }

                if (_selectionService!.PrimarySelection is River river && river.Editor.IsEditing)
                {
                    _activeModifyWaterBodyCommand = new(Scene);
                    _activeModifyWaterBodyCommand.CaptureBefore(river);

                    river.Editor.OnMouseDown(state.WorldPoint, 5);
                    return;
                }

                if (_selectionService!.PrimarySelection is MapPath mp && mp.Editor.IsEditing)
                {
                    MapLayer pathLayer = MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.PATHLOWERLAYER);

                    if (mp.DrawOverSymbols)
                    {
                        pathLayer = MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.PATHUPPERLAYER);
                    }

                    _activeModifyMapPathCommand = new(Scene!.Map, pathLayer);

                    _activeModifyMapPathCommand.CaptureBefore(mp);

                    mp.Editor.OnMouseDown(state.WorldPoint, 5);

                    return;
                }


                if (_editorState.CurrentDrawingMode == MapDrawingMode.ShapeSelect)
                {
                    if (ActiveEditorTool is SelectionTool selectionTool)
                    {
                        selectionTool.OnMouseDown(state);
                    }

                    if (_selectionService!.PrimarySelection != null)
                    {
                        if (_selectionService!.PrimarySelection is Landform lf)
                        {
                            if (lf.HitPath.Contains(state.WorldPoint.X, state.WorldPoint.Y))
                            {
                                _dragShape = lf;
                                _dragStartWorld = state.WorldPoint;
                                _dragOriginalGeometry = lf.CloneGeometry();
                                _isDragging = true;
                                lf.BeginInteractive();
                            }

                            return;
                        }

                        if (_selectionService!.PrimarySelection is MapScale scale)
                        {
                            _dragShape = scale;
                            _dragStartWorld = new SKPoint(scale.Location.X + scale.ScaleWidth / 2, scale.Location.Y + scale.ScaleHeight / 2);
                            _isDragging = true;

                            return;
                        }

                        if (_selectionService!.PrimarySelection is IDrawnMapComponent dmc)
                        {
                            _dragShape = (MapComponent2D)dmc;
                            _dragStartWorld = new SKPoint(_dragShape.Bounds.MidX, _dragShape.Bounds.MidY);
                            _isDragging = true;

                            return;
                        }
                    }
                    else
                    {
                        if (ActiveEditorTool is RegionTool regionTool)
                        {
                            regionTool.SelectedRegion = null;
                        }
                    }

                    return;
                }
            }

            if (state.Button == EditorMouseButton.Middle)
            {
                BeginPan(state.ScreenPoint);
                return;
            }

            if (state.Button == EditorMouseButton.Right)
            {
                _selectionService?.ClearSelection();
            }

            ActiveEditorTool?.OnMouseDown(state);
            RequestRedraw();
        }

        internal void OnMouseMove(PointerState state)
        {
            if (Scene == null)
            {
                return;
            }

            Scene.Camera.CurrentMouseLocation = state.ScreenPoint;
            Scene.Camera.CurrentCursorPoint = state.WorldPoint;

            if (state.Button == EditorMouseButton.None)
            {
                if (_selectionService!.PrimarySelection != null)
                {
                    if (_selectionService!.PrimarySelection is River river && river.Editor.IsEditing)
                    {
                        river.Editor.OnMouseMove(state.WorldPoint, 5);
                        return;
                    }

                    if (_selectionService!.PrimarySelection is MapPath mp && mp.Editor.IsEditing)
                    {
                        mp.Editor.OnMouseMove(state.WorldPoint, 5);
                        return;
                    }

                    if (_selectionService!.PrimarySelection is MapSymbol ms && !_isTransforming)
                    {
                        SelectedSymbolNoButtonMove(ms, state.WorldPoint);
                        return;
                    }

                    if (_selectionService!.PrimarySelection is MapLabel ml && !_isTransforming)
                    {
                        SelectedLabelNoButtonMove(ml, state.WorldPoint);
                        return;
                    }

                    if (_selectionService!.PrimarySelection is PlacedMapBox pmb && !_isTransforming)
                    {
                        SelectedBoxNoButtonMove(pmb, state.WorldPoint);
                        return;
                    }
                }
            }

            if (state.Button == EditorMouseButton.Left)
            {
                if ((_editorState.CurrentDrawingMode == MapDrawingMode.RealmAreaSelect
                    || _editorState.CurrentDrawingMode == MapDrawingMode.RealmLassoSelect)
                    && ActiveEditorTool is SelectionTool st)
                {
                    st.OnMouseMove(state);
                    return;
                }

                if (_selectionService!.PrimarySelection is River river && river.Editor.IsEditing)
                {
                    river.Editor.OnMouseMove(state.WorldPoint, 5);

                    RequestRedraw();

                    return;
                }

                if (_selectionService!.PrimarySelection is MapPath mp && mp.Editor.IsEditing)
                {
                    mp.Editor.OnMouseMove(state.WorldPoint, 5);

                    RequestRedraw();

                    return;
                }

                if (_selectionService!.PrimarySelection is MapSymbol ms && _isTransforming && _editorState.CurrentDrawingMode == MapDrawingMode.ShapeSelect)
                {
                    SelectedSymbolLeftButtonMove(ms, state.WorldPoint);
                    return;
                }

                if (_selectionService!.PrimarySelection is MapLabel ml && _isTransforming && _editorState.CurrentDrawingMode == MapDrawingMode.ShapeSelect)
                {
                    SelectedLabelLeftButtonMove(ml, state.WorldPoint);
                    return;
                }

                if (_selectionService!.PrimarySelection is PlacedMapBox pmb && _isTransforming && _editorState.CurrentDrawingMode == MapDrawingMode.ShapeSelect)
                {
                    SelectedBoxLeftButtonMove(pmb, state.WorldPoint);
                    return;
                }

                if (_isDragging
                    && _dragShape != null)
                {
                    // drag selected shape
                    float dx = state.WorldPoint.X - _dragStartWorld.X;
                    float dy = state.WorldPoint.Y - _dragStartWorld.Y;

                    if (_dragShape is Landform lf && _dragOriginalGeometry != null)
                    {
                        lf.RestoreGeometry(_dragOriginalGeometry);
                        lf.Translate(dx, dy);
                    }

                    if (_dragShape is MapScale scale)
                    {
                        float newX = _dragStartWorld.X + dx;
                        float newY = _dragStartWorld.Y + dy;

                        newX = Math.Clamp(newX, 0, Scene.Map.MapWidth);
                        newY = Math.Clamp(newY, 0, Scene.Map.MapHeight);

                        SKPoint newLocation = new(newX - scale.ScaleWidth / 2, newY - scale.ScaleHeight / 2);

                        scale.Location = newLocation;

                        SKRect scaleBounds = new()
                        {
                            Left = scale.Location.X,
                            Top = scale.Location.Y
                        };

                        scaleBounds.Right = scaleBounds.Left + scale.ScaleWidth;
                        scaleBounds.Bottom = scaleBounds.Top + scale.ScaleHeight;

                        scale.Bounds = scaleBounds;
                    }

                    if (_dragShape is IDrawnMapComponent dmc)
                    {
                        int deltaX = (int)(state.WorldPoint.X - previousPointerState.WorldPoint.X);
                        int deltaY = (int)(state.WorldPoint.Y - previousPointerState.WorldPoint.Y);

                        NudgeDrawnMapComponent(dmc, Keys.Up, deltaX, deltaY);
                    }
                }

                previousPointerState = state;
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
            RequestRedraw();
        }

        internal void OnMouseUp(PointerState state)
        {
            if (Scene == null)
            {
                return;
            }

            if (_selectionService!.PrimarySelection is River river && river.Editor.IsEditing)
            {
                river.Editor.OnMouseUp();

                river.WaterSystem!.EndInteractive();
                river.EndInteractive();

                if (_activeModifyWaterBodyCommand != null)
                {
                    _activeModifyWaterBodyCommand.CaptureAfter(river);
                    _commands.Execute(_activeModifyWaterBodyCommand);

                    _activeModifyWaterBodyCommand = null;
                }

                return;
            }

            if (_selectionService!.PrimarySelection is MapPath mp && mp.Editor.IsEditing)
            {
                mp.Editor.OnMouseUp();

                if (_activeModifyMapPathCommand != null)
                {
                    _activeModifyMapPathCommand.CaptureAfter(mp);
                    _commands.Execute(_activeModifyMapPathCommand);

                    _activeModifyMapPathCommand = null;
                }

                return;
            }

            if (_selectionService!.PrimarySelection is MapSymbol ms && _isTransforming)
            {
                SelectedSymbolMouseUp(ms);
                return;
            }

            if (_selectionService!.PrimarySelection is MapLabel ml && _isTransforming)
            {
                SelectedLabelMouseUp(ml);
                return;
            }

            if (_selectionService!.PrimarySelection is PlacedMapBox pmb && _isTransforming)
            {
                SelectedBoxMouseUp(pmb);
                return;
            }

            if (state.Button == EditorMouseButton.Left)
            {
                if ((_editorState.CurrentDrawingMode == MapDrawingMode.RealmAreaSelect
                    || _editorState.CurrentDrawingMode == MapDrawingMode.RealmLassoSelect)
                    && ActiveEditorTool is SelectionTool st)
                {
                    st.OnMouseUp(state);
                    return;
                }

                if (_isDragging && _dragShape != null)
                {
                    if (_dragShape is Landform lf)
                    {
                        lf.EndInteractive();

                        var cmd = new Cmd_ModifyShapeGeometry(
                            (Shape2D)_dragShape,
                            _dragOriginalGeometry!,
                            new SKPath(((Shape2D)_dragShape).HitPath));

                        _commands.Execute(cmd);
                    }

                    _dragOriginalGeometry?.Dispose();
                    _dragOriginalGeometry = null;
                    _dragShape = null;
                    _isDragging = false;

                    RequestRedraw();
                }
            }

            if (state.Button == EditorMouseButton.Middle)
            {
                EndPan();
            }

            if (state.Button == EditorMouseButton.Right)
            {
                // no action
            }

            ActiveEditorTool?.OnMouseUp(state);

            RequestRedraw();
        }

        internal void OnMouseDoubleClick(PointerState state)
        {
            if (_selectionService!.PrimarySelection is MapLabel ml && _editorState.CurrentDrawingMode == MapDrawingMode.ShapeSelect)
            {
                if (ActiveEditorTool is LabelTool labelTool)
                {
                    _isTransforming = false;

                    labelTool.BeginEdit(ml, state.WorldPoint);
                    RequestRedraw();
                    return;
                }

            }

            ActiveEditorTool?.OnMouseDoubleClick(state);
        }

        internal void OnMouseWheel(PointerState state)
        {
            if (Scene?.Camera == null)
                return;

            // 1. Navigation (highest priority)
            if (state.Modifiers == InputModifiers.Control)
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

        // -------------------------------------------------
        // Ocean
        // -------------------------------------------------

        public void ApplyOceanTexture(TextureFillRequest request)
        {
            ArgumentNullException.ThrowIfNull(Scene, nameof(Scene));

            if (request.TextureId == null)
            {
                return;
            }

            Scene.Map.Ocean.TextureId = request.TextureId;
            Scene.Map.Ocean.TextureOpacity = request.Opacity;
            Scene.Map.Ocean.Scale = request.Scale;
            Scene.Map.Ocean.Mirror = request.Mirror;

            Scene.Map.Ocean.OverlayColor = request.Color;
            Scene.Map.Ocean.ColorOverlayEnabled = (request.Color != SKColors.Empty && request.Color != SKColors.Transparent && request.Color != SKColors.White);

            var image = (_assetManager).GetImage(request.TextureId);
            Scene.SetOceanTexture(image);
            Scene.MarkOceanTextureModified();

            RequestRedraw();
        }

        public void ClearOceanTexture()
        {
            ArgumentNullException.ThrowIfNull(Scene, nameof(Scene));

            Scene.Map.Ocean.TextureId = null;
            Scene.SetOceanTexture(null);
            Scene.MarkOceanTextureModified();

            RequestRedraw();
        }

        public void FillOceanColor(TextureFillRequest request)
        {
            ArgumentNullException.ThrowIfNull(Scene, nameof(Scene));

            Scene.Map.Ocean.OverlayColor = request.Color;
            Scene.Map.Ocean.ColorOverlayEnabled = (request.Color != SKColors.Empty && request.Color != SKColors.Transparent && request.Color != SKColors.White);

            Scene.MarkOceanTextureModified();

            RequestRedraw();
        }

        public void ClearOceanColor()
        {
            ArgumentNullException.ThrowIfNull(Scene, nameof(Scene));

            Scene.Map.Ocean.OverlayColor = SKColor.Empty;
            Scene.Map.Ocean.ColorOverlayEnabled = false;

            Scene.MarkOceanTextureModified();

            RequestRedraw();
        }

        public void UpdateOceanPreview(TextureFillRequest request)
        {
            if (Scene?.Map == null || request.TextureId == null)
                return;

            Scene.Map.Ocean.TextureId = request.TextureId;
            Scene.Map.Ocean.TextureOpacity = request.Opacity;
            Scene.Map.Ocean.Scale = request.Scale;
            Scene.Map.Ocean.Mirror = request.Mirror;

            Scene.Map.Ocean.OverlayColor = request.Color;
            Scene.Map.Ocean.ColorOverlayEnabled = (request.Color != SKColors.Empty && request.Color != SKColors.Transparent && request.Color != SKColors.White);

            var image = (_assetManager).GetImage(request.TextureId);
            Scene.SetOceanTexture(image);
            Scene.MarkOceanTextureModified();

            OnSceneChanged();
        }

        // -------------------------------------------------
        // Windroses
        // -------------------------------------------------

        public void ClearWindroses()
        {
            MapLayer windroseLayer = MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.WINDROSELAYER);

            Cmd_ModifyWindroses cmd = new(windroseLayer);

            foreach (var mapComponent in windroseLayer.Shapes)
            {
                if (mapComponent is MapWindrose mw)
                {
                    cmd.RegisterRemovedWindrose(mw);
                }
            }

            _commands.Execute(cmd);
        }


        // -------------------------------------------------
        // Landform
        // -------------------------------------------------

        public void UpdateSelectedLandform(LandformShadingSettings shading, CoastlineSettings coastlineSettings)
        {
            if (_selectionService!.PrimarySelection is Landform lf)
            {
                _commands.Execute(
                    new Cmd_UpdateLandformProperties(
                        lf,
                        shading,
                        coastlineSettings,
                        _assetManager));
            }
        }

        // -------------------------------------------------
        // Water Body
        // -------------------------------------------------


        // -------------------------------------------------
        // Map Path
        // -------------------------------------------------

        public void UpdateSelectedPath(PathRenderStyle renderStyle)
        {
            if (_selectionService!.PrimarySelection is MapPath mp)
            {
                mp.ResolveAssets(_assetManager);

                _commands.Execute(
                    new Cmd_UpdateMapPathProperties(
                        mp,
                        renderStyle,
                        _assetManager));
            }
        }

        // -------------------------------------------------
        // Symbols
        // -------------------------------------------------

        public void UpdateSelectedSymbol(ISymbolSettings settings)
        {
            if (_selectionService!.PrimarySelection is MapSymbol symbol)
            {
                MapLayer symbolsLayer = MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.SYMBOLLAYER);

                Cmd_ModifySymbol cmd = new(symbolsLayer, symbol);

                // TODO: use capture state/restore state?

                symbol.Scale = (float) settings.SymbolScale;
                symbol.CustomSymbolColors[0] = settings.SymbolColor1.ToSKColor();
                symbol.CustomSymbolColors[1] = settings.SymbolColor2.ToSKColor();
                symbol.CustomSymbolColors[2] = settings.SymbolColor3.ToSKColor();
                symbol.Mirror = settings.MirrorSymbol;
                symbol.Rotation = settings.SymbolRotation;

                cmd.CaptureAfter();

                _commands.Execute(cmd);
            }
        }

        public void PaintSelectedSymbol(SKColor newColor, ISymbolSettings settings)
        {
            if (_selectionService!.PrimarySelection is MapSymbol ms)
            {
                MapLayer symbolLayer = MapBuilder.GetMapLayerByIndex(_scene!.Map, MapBuilder.SYMBOLLAYER);

                Cmd_ModifySymbols paintCommand = new(symbolLayer);

                // --- CAPTURE BEFORE ---
                var before = (MapSymbolState)ms.CaptureState();

                // --- APPLY MODIFICATION ---

                if (ms.SymbolDefinition.BaseColorType == MapSymbolBaseColorType.GrayScale)
                {
                    if (settings.RandomizeSymbolColors)
                    {
                        newColor = ColorHelper.Randomize(newColor);
                    }

                    ms.TintColor = newColor;
                }
                else if (ms.SymbolDefinition.BaseColorType == MapSymbolBaseColorType.RGBMask)
                {
                    // RGB Mask symbols are colored with the values set in the tool, which are
                    // updated by the SymbolMediator
                    ms.CustomSymbolColors[0] = settings.RandomizeSymbolColors ? ColorHelper.Randomize(settings.SymbolColor1.ToSKColor()) : settings.SymbolColor1.ToSKColor();
                    ms.CustomSymbolColors[1] = settings.RandomizeSymbolColors ? ColorHelper.Randomize(settings.SymbolColor2.ToSKColor()) : settings.SymbolColor2.ToSKColor();
                    ms.CustomSymbolColors[2] = settings.RandomizeSymbolColors ? ColorHelper.Randomize(settings.SymbolColor3.ToSKColor()) : settings.SymbolColor3.ToSKColor();
                }

                // --- CAPTURE AFTER ---
                var after = (MapSymbolState)ms.CaptureState();

                paintCommand.RegisterModifiedSymbol(ms, before, after);

                symbolLayer.InvalidateSymbol(ms);

                _commands.Execute(paintCommand!);
            }
        }

        private void SelectedMapSymbolMouseDown(MapSymbol ms, SKPoint worldPoint)
        {
            CommitSymbolNudge();

            MapLayer symbolLayer = MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.SYMBOLLAYER);

            Scene.TransformWidget.Target = ms;

            var handle = Scene.TransformWidget.OnMouseDown(worldPoint);

            switch (handle)
            {
                case TransformHandle.Rotate:
                case TransformHandle.TopLeft:
                case TransformHandle.BottomRight:
                case TransformHandle.TopRight:
                case TransformHandle.BottomLeft:
                case TransformHandle.Left:
                case TransformHandle.Right:
                case TransformHandle.Top:
                case TransformHandle.Bottom:
                case TransformHandle.Move:

                    _isTransforming = true;
                    // create command (capture BEFORE)
                    _activeModifyMapSymbolCommand = new Cmd_ModifySymbol(symbolLayer, ms);
                    return;
                case TransformHandle.ZTop:
                    symbolLayer.MoveMapComponentZOrder(ms, ZOrderMoveType.ToTop);
                    RequestRedraw();
                    return;
                case TransformHandle.ZForward:
                    symbolLayer.MoveMapComponentZOrder(ms, ZOrderMoveType.AboveAllOverlaps);
                    RequestRedraw();
                    return;
                case TransformHandle.ZBackward:
                    symbolLayer.MoveMapComponentZOrder(ms, ZOrderMoveType.BelowAllOverlaps);
                    RequestRedraw();
                    return;
                case TransformHandle.ZBottom:
                    symbolLayer.MoveMapComponentZOrder(ms, ZOrderMoveType.ToBottom);
                    RequestRedraw();
                    return;
                case TransformHandle.None:
                    break;
                default:
                    break;
            }
        }

        private void SelectedSymbolNoButtonMove(MapSymbol ms, SKPoint worldPosition)
        {
            Scene!.TransformWidget.Target = ms;
            HandleHoverUpdate(worldPosition);

            RequestRedraw();
        }


        private void SelectedSymbolLeftButtonMove(MapSymbol ms, SKPoint worldPosition)
        {
            Scene!.TransformWidget.Target = ms;
            Scene.TransformWidget.OnMouseMove(worldPosition);

            var oldBounds = ms.Bounds;

            ms.UpdateBounds();

            var newBounds = ms.Bounds;

            MapLayer symbolLayer = MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.SYMBOLLAYER);

            symbolLayer.UpdateShapeTiles(ms, oldBounds, newBounds);
            symbolLayer.InvalidateSymbol(ms);

            RequestRedraw();
        }

        private void SelectedSymbolMouseUp(MapSymbol ms)
        {
            Scene!.TransformWidget.OnMouseUp();

            // capture AFTER
            _activeModifyMapSymbolCommand?.CaptureAfter();

            // commit command
            if (_activeModifyMapSymbolCommand != null)
            {
                _commands.Execute(_activeModifyMapSymbolCommand);
            }

            _activeModifyMapSymbolCommand = null;
            _isTransforming = false;

            RequestRedraw();
        }

        public void NudgeSymbol(MapSymbol symbol, Keys key, int dx, int dy)
        {
            if (symbol == null)
                return;

            // If key changed commit previous command
            if (_activeSymbolNudgeCommand != null &&
                (_nudgeSymbol != symbol || _activeSymbolNudgeKey != key))
            {
                CommitSymbolNudge();
            }

            // Start new command if needed
            if (_activeSymbolNudgeCommand == null)
            {
                _nudgeSymbol = symbol;
                _activeSymbolNudgeKey = key;

                _activeSymbolNudgeCommand = new Cmd_ModifySymbol(
                    GetSymbolLayer(), symbol);
            }

            // Apply movement
            var loc = symbol.Location;
            loc.X = Math.Clamp(loc.X + dx, 0, Scene!.Map.MapWidth);
            loc.Y = Math.Clamp(loc.Y + dy, 0, Scene!.Map.MapHeight);

            symbol.Location = loc;

            symbol.UpdateBounds();

            var layer = GetSymbolLayer();
            layer.InvalidateSymbol(symbol);
        }

        public void CommitSymbolNudge()
        {
            if (_activeSymbolNudgeCommand == null || _nudgeSymbol == null)
                return;

            _activeSymbolNudgeCommand.CaptureAfter();

            if (_activeSymbolNudgeCommand.HasChange)
            {
                _commands.Execute(_activeSymbolNudgeCommand);
            }

            _activeSymbolNudgeCommand = null;
            _nudgeSymbol = null;
        }

        private MapLayer GetSymbolLayer()
        {
            return MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.SYMBOLLAYER);
        }

        private void HandleHoverUpdate(SKPoint worldPosition)
        {
            Scene!.TransformWidget.UpdateHover(worldPosition, Scene.Camera.Zoom);

            var handle = Scene.TransformWidget.HoverHandle;

            switch (handle)
            {
                case TransformHandle.Rotate:
                    Cursor.Current = Cursors.Cross;
                    break;

                case TransformHandle.TopLeft:
                case TransformHandle.BottomRight:
                    Cursor.Current = Cursors.SizeNWSE;
                    break;

                case TransformHandle.TopRight:
                case TransformHandle.BottomLeft:
                    Cursor.Current = Cursors.SizeNESW;
                    break;

                case TransformHandle.Left:
                case TransformHandle.Right:
                    Cursor.Current = Cursors.SizeWE;
                    break;

                case TransformHandle.Top:
                case TransformHandle.Bottom:
                    Cursor.Current = Cursors.SizeNS;
                    break;

                case TransformHandle.Move:
                    Cursor.Current = Cursors.SizeAll;
                    break;
                case TransformHandle.ZTop:
                case TransformHandle.ZForward:
                    Cursor.Current = Cursors.PanNorth;
                    break;

                case TransformHandle.ZBackward:
                case TransformHandle.ZBottom:
                    Cursor.Current = Cursors.PanSouth;
                    break;

                case TransformHandle.None:
                    break;
                default:
                    break;
            }
        }

        // -------------------------------------------------
        // Labels
        // -------------------------------------------------

        public void UpdateSelectedLabel(ILabelSettings settings)
        {
            if (_selectionService!.PrimarySelection is MapLabel label)
            {
                MapLayer labelLayer = MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.LABELLAYER);

                Cmd_ModifyLabel cmd = new(labelLayer, label);

                label.FontStyle = settings.FontStyle;
                label.FontColor = settings.LabelColor.ToSKColor();
                label.OutlineColor = settings.OutlineColor.ToSKColor();
                label.OutlineWidth = settings.OutlineWidth;
                label.GlowColor = settings.GlowColor.ToSKColor();
                label.GlowStrength = settings.GlowStrength;
                label.Rotation = settings.Rotation;

                cmd.CaptureAfter();

                _commands.Execute(cmd);
            }
        }

        private void SelectedMapLabelMouseDown(MapLabel ml, SKPoint worldPoint)
        {
            if (ActiveEditorTool is LabelTool tool)
            {
                tool.EnsureEditCommitted();
            }

            CommitLabelNudge();

            MapLayer labelLayer = GetLabelLayer();

            Scene!.TransformWidget.Target = ml;

            var handle = Scene.TransformWidget.OnMouseDown(worldPoint);

            switch (handle)
            {
                case TransformHandle.Rotate:
                case TransformHandle.TopLeft:
                case TransformHandle.BottomRight:
                case TransformHandle.TopRight:
                case TransformHandle.BottomLeft:
                case TransformHandle.Left:
                case TransformHandle.Right:
                case TransformHandle.Top:
                case TransformHandle.Bottom:
                case TransformHandle.Move:
                    _isTransforming = true;
                    // create command (capture BEFORE)
                    _activeModifyLabelCommand = new Cmd_ModifyLabel(labelLayer, ml);
                    return;
                case TransformHandle.ZTop:
                    _isTransforming = true;
                    labelLayer.MoveMapComponentZOrder(ml, ZOrderMoveType.ToTop);
                    RequestRedraw();
                    return;
                case TransformHandle.ZForward:
                    _isTransforming = true;
                    labelLayer.MoveMapComponentZOrder(ml, ZOrderMoveType.AboveAllOverlaps);
                    RequestRedraw();
                    return;
                case TransformHandle.ZBackward:
                    _isTransforming = true;
                    labelLayer.MoveMapComponentZOrder(ml, ZOrderMoveType.BelowAllOverlaps);
                    RequestRedraw();
                    return;
                case TransformHandle.ZBottom:
                    _isTransforming = true;
                    labelLayer.MoveMapComponentZOrder(ml, ZOrderMoveType.ToBottom);
                    RequestRedraw();
                    return;
                case TransformHandle.None:
                    break;
                default:
                    break;
            }
        }

        private void SelectedLabelNoButtonMove(MapLabel ml, SKPoint worldPosition)
        {
            Scene!.TransformWidget.Target = ml;
            HandleHoverUpdate(worldPosition);

            RequestRedraw();
        }

        private void SelectedLabelLeftButtonMove(MapLabel ml, SKPoint worldPosition)
        {
            Scene!.TransformWidget.Target = ml;
            Scene.TransformWidget.OnMouseMove(worldPosition);

            ml.BoundsModified = true;

            RequestRedraw();
        }

        private void SelectedLabelMouseUp(MapLabel ml)
        {
            Scene!.TransformWidget.OnMouseUp();

            // capture AFTER
            _activeModifyLabelCommand?.CaptureAfter();

            // commit command
            if (_activeModifyLabelCommand != null)
            {
                Commands.Execute(_activeModifyLabelCommand);
            }

            _activeModifyLabelCommand = null;
            _isTransforming = false;

            RequestRedraw();
        }

        public void NudgeLabel(MapLabel label, Keys key, int dx, int dy)
        {
            if (label == null)
                return;

            // If key changed → commit previous command
            if (_activeLabelNudgeCommand != null &&
                (_nudgeLabel != label || _activeLabelNudgeKey != key))
            {
                CommitLabelNudge();
            }

            // Start new command if needed
            if (_activeLabelNudgeCommand == null)
            {
                _nudgeLabel = label;
                _activeLabelNudgeKey = key;

                _activeLabelNudgeCommand = new Cmd_ModifyLabel(
                    GetLabelLayer(), label);
            }

            // Apply movement
            var loc = label.Location;
            loc.X = Math.Clamp(loc.X + dx, 0, Scene!.Map.MapWidth);
            loc.Y = Math.Clamp(loc.Y + dy, 0, Scene!.Map.MapHeight);

            label.Location = loc;

            label.BoundsModified = true;

            RequestRedraw();
        }

        public void CommitLabelNudge()
        {
            if (_activeLabelNudgeCommand == null || _nudgeLabel == null)
                return;

            _activeLabelNudgeCommand.CaptureAfter();

            if (_activeLabelNudgeCommand.HasChange)
            {
                _commands.Execute(_activeLabelNudgeCommand);
            }

            _activeLabelNudgeCommand = null;
            _nudgeLabel = null;
        }

        private MapLayer GetLabelLayer()
        {
            return MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.LABELLAYER);
        }

        // -------------------------------------------------
        // Boxes
        // -------------------------------------------------
        public void UpdateSelectedBox(IBoxSettings settings)
        {
            if (_selectionService!.PrimarySelection is PlacedMapBox box)
            {
                Cmd_ModifyBox cmd = new(GetBoxLayer(), box);

                box.BaseBox = settings.SelectedBox?.BoxDefinition;

                box.BoxTint = settings.BoxTint.ToSKColor();
                box.Rotation = settings.Rotation;

                cmd.CaptureAfter();

                _commands.Execute(cmd);
            }
        }

        private void SelectedBoxNoButtonMove(PlacedMapBox pmb, SKPoint worldPosition)
        {
            Scene!.TransformWidget.Target = pmb;
            HandleHoverUpdate(worldPosition);

            RequestRedraw();
        }

        private void SelectedBoxLeftButtonMove(PlacedMapBox pmb, SKPoint worldPosition)
        {
            Scene!.TransformWidget.Target = pmb;
            Scene.TransformWidget.OnMouseMove(worldPosition);

            RequestRedraw();
        }

        private void SelectedBoxMouseUp(PlacedMapBox pmb)
        {
            Scene!.TransformWidget.OnMouseUp();

            // capture AFTER
            _activeModifyBoxCommand?.CaptureAfter();

            // commit command
            if (_activeModifyBoxCommand != null)
            {
                Commands.Execute(_activeModifyBoxCommand);
            }

            _activeModifyBoxCommand = null;
            _isTransforming = false;

            RequestRedraw();
        }

        private void SelectedMapBoxMouseDown(PlacedMapBox pmb, SKPoint worldPoint)
        {
            MapLayer boxLayer = GetBoxLayer();

            Scene!.TransformWidget.Target = pmb;

            var handle = Scene.TransformWidget.OnMouseDown(worldPoint);

            switch (handle)
            {
                case TransformHandle.Rotate:
                case TransformHandle.TopLeft:
                case TransformHandle.BottomRight:
                case TransformHandle.TopRight:
                case TransformHandle.BottomLeft:
                case TransformHandle.Left:
                case TransformHandle.Right:
                case TransformHandle.Top:
                case TransformHandle.Bottom:
                case TransformHandle.Move:
                    _isTransforming = true;
                    // create command (capture BEFORE)
                    _activeModifyBoxCommand = new Cmd_ModifyBox(boxLayer, pmb);
                    return;
                case TransformHandle.ZTop:
                    _isTransforming = true;
                    boxLayer.MoveMapComponentZOrder(pmb, ZOrderMoveType.ToTop);
                    RequestRedraw();
                    return;
                case TransformHandle.ZForward:
                    _isTransforming = true;
                    boxLayer.MoveMapComponentZOrder(pmb, ZOrderMoveType.AboveAllOverlaps);
                    RequestRedraw();
                    return;
                case TransformHandle.ZBackward:
                    _isTransforming = true;
                    boxLayer.MoveMapComponentZOrder(pmb, ZOrderMoveType.BelowAllOverlaps);
                    RequestRedraw();
                    return;
                case TransformHandle.ZBottom:
                    _isTransforming = true;
                    boxLayer.MoveMapComponentZOrder(pmb, ZOrderMoveType.ToBottom);
                    RequestRedraw();
                    return;
                case TransformHandle.None:
                    break;
                default:
                    break;
            }
        }

        public void NudgeBox(PlacedMapBox box, Keys key, int dx, int dy)
        {
            if (box == null)
                return;

            // If key changed commit previous command
            if (_activeBoxNudgeCommand != null &&
                (_nudgeBox != box || _activeBoxNudgeKey != key))
            {
                CommitBoxNudge();
            }

            // Start new command if needed
            if (_activeBoxNudgeCommand == null)
            {
                _nudgeBox = box;
                _activeBoxNudgeKey = key;

                _activeBoxNudgeCommand = new Cmd_ModifyBox(
                    GetBoxLayer(), box);
            }

            // Apply movement
            var loc = box.Location;
            loc.X = Math.Clamp(loc.X + dx, 0, Scene!.Map.MapWidth);
            loc.Y = Math.Clamp(loc.Y + dy, 0, Scene!.Map.MapHeight);

            box.Location = loc;

            RequestRedraw();
        }

        public void CommitBoxNudge()
        {
            if (_activeBoxNudgeCommand == null || _nudgeBox == null)
                return;

            _activeBoxNudgeCommand.CaptureAfter();

            if (_activeBoxNudgeCommand.HasChange)
            {
                _commands.Execute(_activeBoxNudgeCommand);
            }

            _activeBoxNudgeCommand = null;
            _nudgeBox = null;
        }

        private MapLayer GetBoxLayer()
        {
            return MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.BOXLAYER);
        }

        // -------------------------------------------------
        // Vignette
        // -------------------------------------------------

        public void SetVignette(VignetteShapeType vignetteType, float vignetteStrength, SKColor vignetteColor)
        {
            MapLayer vignetteLayer = MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.VIGNETTELAYER);

            foreach (var shape in vignetteLayer.Shapes)
            {
                if (shape is MapVignette)
                {
                    // only one vignette can be set
                    return;
                }
            }

            MapVignette vignette = new()
            {
                VignetteShape = vignetteType,
                VignetteStrength = vignetteStrength,
                VignetteColor = vignetteColor
            };

            vignetteLayer.Add(vignette);
        }

        public void ClearVignette()
        {
            MapLayer vignetteLayer = MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.VIGNETTELAYER);
            vignetteLayer.Clear();
        }

        public void UpdateVignette(VignetteShapeType vignetteType, float vignetteStrength, SKColor vignetteColor)
        {
            MapLayer vignetteLayer = MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.VIGNETTELAYER);

            foreach (var component in vignetteLayer.Shapes)
            {
                if (component is MapVignette existingVignette)
                {
                    existingVignette.VignetteShape = vignetteType;
                    existingVignette.VignetteStrength = vignetteStrength;
                    existingVignette.VignetteColor = vignetteColor;
                    break;
                }
            }

            RequestRedraw();
        }

        // -------------------------------------------------
        // Frame
        // -------------------------------------------------

        public void SetFrame(MapFrame frame, SKColor frameColor, float frameScale)
        {
            MapLayer frameLayer = MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.FRAMELAYER);

            // Clear existing frame
            frameLayer.Clear();

            if (frame != null)
            {
                if (frame.FrameBitmap == null)
                {
                    if (!string.IsNullOrEmpty(frame.FrameBitmapPath))
                    {
                        frame.FrameBitmap = SKBitmap.Decode(frame.FrameBitmapPath);
                    }
                }

                PlacedMapFrame placedFrame = new()
                {
                    FrameDefinition = frame,
                    FrameTint = frameColor,
                    FrameScale = frameScale,
                    Bounds = Scene.WorldBounds,
                };

                CompletePlacedFrame(placedFrame);

                frameLayer.Add(placedFrame);
            }
        }

        internal static void CompletePlacedFrame(PlacedMapFrame mapFrame)
        {
            if (mapFrame.FrameDefinition?.FrameBitmap == null)
            {
                return;
            }

            SKBitmap bitmap =
                mapFrame.FrameDefinition.FrameBitmap;

            SKRectI center = new(
                (int)mapFrame.FrameDefinition.FrameCenterLeft,
                (int)mapFrame.FrameDefinition.FrameCenterTop,
                bitmap.Width -
                    (int)mapFrame.FrameDefinition.FrameCenterRight,
                bitmap.Height -
                    (int)mapFrame.FrameDefinition.FrameCenterBottom);

            if (center.IsEmpty)
            {
                return;
            }

            // Fix inverted rects
            if (center.Right < center.Left)
            {
                (center.Left, center.Right) =
                    (center.Right, center.Left);
            }

            if (center.Bottom < center.Top)
            {
                (center.Top, center.Bottom) =
                    (center.Bottom, center.Top);
            }

            SKBitmap[] slices = UserInterfaceUtilities.SliceNinePatchBitmap(bitmap, center);

            mapFrame.PatchA = slices[0];
            mapFrame.PatchB = slices[1];
            mapFrame.PatchC = slices[2];
            mapFrame.PatchD = slices[3];
            mapFrame.PatchE = slices[4];
            mapFrame.PatchF = slices[5];
            mapFrame.PatchG = slices[6];
            mapFrame.PatchH = slices[7];
            mapFrame.PatchI = slices[8];
        }

        // -------------------------------------------------
        // Grid
        // -------------------------------------------------

        internal void SetGrid(IGridSettings settings)
        {
            // find existing grid, if any
            MapLayer defaultGridLayer = MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.DEFAULTGRIDLAYER);
            MapLayer aboveOceanGridLayer = MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.ABOVEOCEANGRIDLAYER);
            MapLayer belowSymbolsGridLayer = MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.BELOWSYMBOLSGRIDLAYER);

            defaultGridLayer.Clear();
            aboveOceanGridLayer.Clear();
            belowSymbolsGridLayer.Clear();

            if (settings.GridEnabled)
            {
                MapGrid newGrid = new()
                {
                    GridEnabled = settings.GridEnabled,
                    GridType = settings.GridType,
                    GridLayerIndex = settings.GridLayer,
                    GridSize = settings.GridSize,
                    GridLineWidth = settings.GridLineWidth,
                    GridColor = settings.GridColor.ToSKColor(),
                    ShowGridSize = settings.ShowGridSize,
                    MapAreaWidth = Scene!.Map.MapAreaWidth,
                    MapAreaHeight = Scene!.Map.MapAreaHeight,
                    MapAreaUnits = Scene!.Map.MapAreaUnits
                };

                // create new grid and add to appropriate layer
                MapLayer? targetLayer = newGrid.GridLayerIndex switch
                {
                    MapBuilder.DEFAULTGRIDLAYER => defaultGridLayer,
                    MapBuilder.ABOVEOCEANGRIDLAYER => aboveOceanGridLayer,
                    MapBuilder.BELOWSYMBOLSGRIDLAYER => belowSymbolsGridLayer,
                    _ => null
                };

                if (targetLayer != null)
                {
                    SetActiveDrawingLayer(targetLayer);
                    newGrid.Bounds = Scene.WorldBounds;
                    targetLayer.Add(newGrid);
                }
            }
        }

        internal void UpdateGrid(IGridSettings settings)
        {
            SetGrid(settings);
            RequestRedraw();
        }

        // -------------------------------------------------
        // Map Measures
        // -------------------------------------------------

        internal void ClearMapMeasures()
        {
            MapLayer measureLayer = MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.MEASURELAYER);
            measureLayer.Clear();
        }

        // -------------------------------------------------
        // Map Scale
        // -------------------------------------------------

        internal void CreateMapScale(IMapScaleSettings mapScaleSettings)
        {
            MapScale scale = new()
            {
                ScaleWidth = mapScaleSettings.ScaleWidth,
                ScaleHeight = mapScaleSettings.ScaleHeight,
                ScaleSegmentCount = mapScaleSettings.ScaleSegments,
                ScaleLineWidth = mapScaleSettings.ScaleLineWidth,
                ScaleColor1 = mapScaleSettings.SegmentColor1.ToSKColor(),
                ScaleColor2 = mapScaleSettings.SegmentColor2.ToSKColor(),
                ScaleColor3 = mapScaleSettings.SegmentColor3.ToSKColor(),
                ScaleDistance = mapScaleSettings.SegmentDistance,
                ScaleDistanceUnit = mapScaleSettings.UnitLabel,
                ScaleNumbersDisplayType = mapScaleSettings.ScaleNumbersDisplayLocation,
                ScaleFont = mapScaleSettings.FontStyle,
                ScaleFontColor = mapScaleSettings.FontColor.ToSKColor(),
                ScaleOutlineColor = mapScaleSettings.NumbersOutlineColor.ToSKColor(),
                ScaleOutlineWidth = mapScaleSettings.NumbersOutlineWidth
            };


            SKRect scaleBounds = new()
            {
                Left = _scene!.WorldBounds.Left + 100, // 100 pixels from the left edge
                Top = _scene!.WorldBounds.Bottom - 100 // 100 pixels from the bottom edge
            };

            scale.Location = new SKPoint(scaleBounds.Left, scaleBounds.Top);

            scaleBounds.Right = scaleBounds.Left + mapScaleSettings.ScaleWidth;
            scaleBounds.Bottom = scaleBounds.Top + mapScaleSettings.ScaleHeight;

            scale.Bounds = scaleBounds;
            scale.LocalBounds = new SKRect(0, 0, mapScaleSettings.ScaleWidth, mapScaleSettings.ScaleHeight);

            MapLayer overlayLayer = MapBuilder.GetMapLayerByIndex(_scene!.Map, MapBuilder.OVERLAYLAYER);
            overlayLayer.Clear(); // Clear existing scale if any
            overlayLayer.Add(scale);
        }

        internal void RemoveMapScale()
        {
            MapLayer overlayLayer = MapBuilder.GetMapLayerByIndex(_scene!.Map, MapBuilder.OVERLAYLAYER);
            overlayLayer.Clear(); // Clear existing scale if any
        }

        // -------------------------------------------------
        // Map Region
        // -------------------------------------------------

        internal void UpdateSelectedRegion(IRegionSettings regionSettings)
        {
            if (ActiveEditorTool is RegionTool tool)
            {
                tool.UpdatedSelectedRegion(regionSettings);
            }
        }

        // -------------------------------------------------
        // Drawn Map Component
        // -------------------------------------------------

        internal void NudgeDrawnMapComponent(IDrawnMapComponent dmc, Keys up, int dx, int dy)
        {
            if (dmc == null)
            {
                return;
            }

            // Apply movement
            switch (dmc)
            {
                case DrawnArrow da:
                    {
                        da.TopLeft = new SKPoint(da.TopLeft.X + dx, da.TopLeft.Y + dy);
                        da.BottomRight = new SKPoint(da.BottomRight.X + dx, da.BottomRight.Y + dy);
                    }
                    break;
                case DrawnDiamond dd:
                    {
                        dd.TopLeft = new SKPoint(dd.TopLeft.X + dx, dd.TopLeft.Y + dy);
                        dd.BottomRight = new SKPoint(dd.BottomRight.X + dx, dd.BottomRight.Y + dy);
                    }
                    break;
                case DrawnEllipse de:
                    {
                        de.TopLeft = new SKPoint(de.TopLeft.X + dx, de.TopLeft.Y + dy);
                        de.BottomRight = new SKPoint(de.BottomRight.X + dx, de.BottomRight.Y + dy);
                    }
                    break;
                case DrawnFivePointStar dfps:
                    {
                        dfps.Center = new SKPoint(dfps.Center.X + dx, dfps.Center.Y + dy);
                    }
                    break;
                case DrawnLine dl:
                    {
                        for (int i = 0; i < dl.Points.Count; i++)
                        {
                            SKPoint np = dl.Points[i];
                            SKPoint p = new(np.X + dx, np.Y + dy);
                            dl.Points[i] = p;
                        }
                    }
                    break;
                case PaintedLine pl:
                    {
                        for (int i = 0; i < pl.Points.Count; i++)
                        {
                            SKPoint np = pl.Points[i];
                            SKPoint p = new(np.X + dx, np.Y + dy);
                            pl.Points[i] = p;
                        }

                        // this will cause the PaintedLine cached image to be rebuilt so that it moves on the screen
                        pl.FinalizeShapeGeometry(Scene!.Map);
                    }
                    break;
                case DrawnPolygon dp:
                    {
                        for (int i = 0; i < dp.Points.Count; i++)
                        {
                            SKPoint np = dp.Points[i];
                            SKPoint p = new(np.X + dx, np.Y + dy);
                            dp.Points[i] = p;
                        }
                    }
                    break;
                case DrawnRectangle dr:
                    {
                        dr.TopLeft = new SKPoint(dr.TopLeft.X + dx, dr.TopLeft.Y + dy);
                        dr.BottomRight = new SKPoint(dr.BottomRight.X + dx, dr.BottomRight.Y + dy);
                    }
                    break;
                case DrawnRegularPolygon drp:
                    {
                        drp.TopLeft = new SKPoint(drp.TopLeft.X + dx, drp.TopLeft.Y + dy);
                        drp.BottomRight = new SKPoint(drp.BottomRight.X + dx, drp.BottomRight.Y + dy);
                    }
                    break;
                case DrawnSixPointStar dsps:
                    {
                        dsps.Center = new SKPoint(dsps.Center.X + dx, dsps.Center.Y + dy);
                    }
                    break;
                case DrawnTriangle dt:
                    {
                        dt.TopLeft = new SKPoint(dt.TopLeft.X + dx, dt.TopLeft.Y + dy);
                        dt.BottomRight = new SKPoint(dt.BottomRight.X + dx, dt.BottomRight.Y + dy);
                    }
                    break;

            }

            // rather than rebuilding the entire map layer index
            // here, removing the drawn map component and re-adding
            // it is much faster; then only the tiles and spatial index
            // containing that component are re-built
            foreach (MapLayer layer in Scene!.Map.MapLayers)
            {
                if (layer.Shapes.Contains((MapComponent2D)dmc))
                {
                    layer.Remove((MapComponent2D)dmc);
                    layer.Add((MapComponent2D)dmc);
                    layer.ProcessPlacementQueue();
                }
            }

            _commandService!.MarkMapModified();

            RequestRedraw();
        }


        // -------------------------------------------------
        // End Class
        // -------------------------------------------------
    }

    // -------------------------------------------------
    // Editor Tool Type Enum
    // -------------------------------------------------

    public enum EditorToolType
    {
        // TODO: add other tools as they are implemented
        OceanTool,
        WindroseTool,
        LandformTool,
        LabelTool,
        MapPathTool,
        PaintedShapeTool,
        SymbolTool,
        WaterBodyTool,
        BoxTool,
        MeasureTool,
        RegionTool,
        DrawingTool,
        SelectionTool,
        PaintTool,
    }
}
