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

        public EditorState State => _editorState;

        private MapScene? _scene;

        private ToolFactory? _toolFactory;
        private IToolEditor? _activeTool;

        private SKSize _viewportSize;

        private ISelectable? _selectedShape;
        public ISelectable? SelectedShape => _selectedShape;

        private SKPoint? _lastClickWorld;
        private int _selectionCycleIndex;

        // -------------------------------------------------
        // PolylineEditor and TransformWidget handle dragging undo/redo support
        // -------------------------------------------------

        private Cmd_ModifyWaterBodies? _activeModifyWaterBodyCommand;
        private Cmd_ModifyMapPaths? _activeModifyMapPathCommand;
        private Cmd_ModifySymbol? _activeModifyMapSymbolCommand;
        private Cmd_ModifyLabel? _activeModifyLabelCommand;

        private bool _isTransforming;
        private SKRect _lastSymbolBounds;

        // -------------------------------------------------
        // Selection filter
        // -------------------------------------------------

        private SelectionFilterState _selectionFilter = new([]);

        // -------------------------------------------------
        // Shape dragging
        // -------------------------------------------------

        private MapComponent2D? _dragShape;
        private SKPoint _dragStartWorld;
        private SKPath? _dragOriginalGeometry;
        private bool _isDragging;

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
                if (SelectedShape is River river && river.Editor.IsEditing)
                {
                    _activeModifyWaterBodyCommand = new(Scene!.Map);
                    _activeModifyWaterBodyCommand.CaptureBefore(river);

                    river.Editor.OnMouseDown(state.WorldPoint, 5);
                }
                else if (SelectedShape is MapPath mp && mp.Editor.IsEditing)
                {
                    MapLayer pathLayer = MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.PATHLOWERLAYER);

                    //if (_pathMediator!.DrawOverSymbols)
                    //{
                    //    pathLayer = MapBuilder.GetMapLayerByIndex(Scene!.Map, MapBuilder.PATHUPPERLAYER);
                    //}

                    _activeModifyMapPathCommand = new(Scene!.Map, pathLayer);

                    _activeModifyMapPathCommand.CaptureBefore(mp);

                    mp.Editor.OnMouseDown(state.WorldPoint, 5);
                }
                else if (SelectedShape is MapSymbol ms && _editorState.CurrentDrawingMode == MapDrawingMode.ShapeSelect)
                {
                    //SelectedMapSymbolMouseDown(ms, world);
                }
                else if (SelectedShape is MapLabel ml && _editorState.CurrentDrawingMode == MapDrawingMode.ShapeSelect)
                {
                    //SelectedMapLabelMouseDown(ml, world);
                }

                if (_editorState.CurrentDrawingMode == MapDrawingMode.ShapeSelect && !_isTransforming)
                {
                    HandleSelection(state.WorldPoint);

                    if (SelectedShape != null)
                    {
                        if (SelectedShape is Landform lf)
                        {
                            if (lf.HitPath.Contains(state.WorldPoint.X, state.WorldPoint.Y))
                            {
                                _dragShape = lf;
                                _dragStartWorld = state.WorldPoint;
                                _dragOriginalGeometry = lf.CloneGeometry();
                                _isDragging = true;
                                lf.BeginInteractive();
                            }
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
                if (SelectedShape != null)
                {
                    if (SelectedShape is River river && river.Editor.IsEditing)
                    {
                        river.Editor.OnMouseMove(state.WorldPoint, 5);
                        return;
                    }

                    if (SelectedShape is MapPath mp && mp.Editor.IsEditing)
                    {
                        mp.Editor.OnMouseMove(state.WorldPoint, 5);
                        return;
                    }

                    if (SelectedShape is MapSymbol ms && !_isTransforming)
                    {
                        //SelectedSymbolNoButtonMove(ms, state.WorldPoint);
                        return;
                    }

                    if (SelectedShape is MapLabel ml && !_isTransforming)
                    {
                        //SelectedLabelNoButtonMove(ml, state.WorldPoint);
                        return;
                    }
                }
            }

            if (state.Button == EditorMouseButton.Left)
            {
                if (SelectedShape is River river && river.Editor.IsEditing)
                {
                    river.Editor.OnMouseMove(state.WorldPoint, 5);

                    RequestRedraw();

                    return;
                }

                if (SelectedShape is MapPath mp && mp.Editor.IsEditing)
                {
                    mp.Editor.OnMouseMove(state.WorldPoint, 5);

                    RequestRedraw();

                    return;
                }

                if (SelectedShape is MapSymbol ms && _isTransforming && _editorState.CurrentDrawingMode == MapDrawingMode.ShapeSelect)
                {
                    //SelectedSymbolLeftButtonMove(ms, state.WorldPoint);
                    return;
                }

                if (SelectedShape is MapLabel ml && _isTransforming && _editorState.CurrentDrawingMode == MapDrawingMode.ShapeSelect)
                {
                    //SelectedLabelLeftButtonMove(ml, state.WorldPoint);
                    return;
                }

                if (_isDragging
                    && _dragShape != null
                    && _dragOriginalGeometry != null)
                {
                    // drag selected shape
                    float dx = state.WorldPoint.X - _dragStartWorld.X;
                    float dy = state.WorldPoint.Y - _dragStartWorld.Y;

                    if (_dragShape is Landform lf)
                    {
                        lf.RestoreGeometry(_dragOriginalGeometry);

                        lf.Translate(dx, dy);
                    }
                }
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

            if (SelectedShape is River river && river.Editor.IsEditing)
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

            if (SelectedShape is MapPath mp && mp.Editor.IsEditing)
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

            if (SelectedShape is MapSymbol ms && _isTransforming)
            {
                //SelectedSymbolMouseUp(ms);
                return;
            }

            if (SelectedShape is MapLabel ml && _isTransforming)
            {
                //SelectedLabelMouseUp(ml);
                return;
            }

            if (state.Button == EditorMouseButton.Left)
            {
                if (_isDragging && _dragShape != null)
                {
                    if (_dragShape is Shape2D)
                    {
                        if (_dragShape is Landform lf)
                        {
                            lf.EndInteractive();
                        }

                        var cmd = new Cmd_ModifyShapeGeometry(
                            (Shape2D)_dragShape,
                            _dragOriginalGeometry!,
                            new SKPath(((Shape2D)_dragShape).HitPath));

                        _commands.Execute(cmd);

                        RequestRedraw();
                    }

                    _dragOriginalGeometry?.Dispose();
                    _dragOriginalGeometry = null;
                    _dragShape = null;
                    _isDragging = false;
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
            if (SelectedShape is MapLabel ml && _editorState.CurrentDrawingMode == MapDrawingMode.ShapeSelect)
            {
                /*
                if (ActiveEditorTool is LabelTool labelTool)
                {
                    _isTransforming = false;

                    labelTool.BeginEdit(ml, state.WorldPoint);
                    RequestRedraw();
                    return;
                }
                */
            }

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

        // ---------------------------------------------
        // Selection
        // ---------------------------------------------

        public void HandleSelection(SKPoint worldPoint)
        {
            const float tolerance = 3f;

            bool sameLocation =
                _lastClickWorld.HasValue &&
                SKPoint.Distance((SKPoint)_lastClickWorld, worldPoint) < tolerance;

            var hits = Scene!.HitTestAll(worldPoint);

            bool overrideFilter = (Control.ModifierKeys & Keys.Control) == Keys.Control;

            if (!overrideFilter)
            {
                hits = [.. hits.Where(shape => _selectionFilter.Allows(shape))];
            }

            if (hits.Count == 0)
            {
                // clicking off of any landform or other ISelectable component deselects everything
                DeselectAllMapComponents(Scene!, null);
                _selectedShape = null;
                _selectionCycleIndex = 0;
                return;
            }

            if (!sameLocation)
            {
                // New click location → reset cycle
                _selectionCycleIndex = 0;
            }
            else
            {
                // Cycle deeper
                _selectionCycleIndex++;
            }

            if (_selectionCycleIndex >= hits.Count)
            {
                _selectionCycleIndex = 0;
            }

            _selectedShape = hits[_selectionCycleIndex];

            // only one ISelectable object (Shape2D or WaterSystem) can be selected at any time
            if (_selectedShape is Shape2D shape2d && _selectedShape is not WaterBody)
            {
                DeselectAllMapComponents(Scene!, _selectedShape);

                if (_selectedShape is Landform lf)
                {
                    lf.IsSelected = true;
                    _editorState.StatusMessage = "Landform Selected " + (!string.IsNullOrEmpty(lf.LandformName) ? ": " + lf.LandformName : "");
                }
                else if (_selectedShape is MapPath mp)
                {
                    mp.IsSelected = true;
                    _editorState.StatusMessage = "Path Selected " + (!string.IsNullOrEmpty(mp.MapPathName) ? ": " + mp.MapPathName : "");
                    //mp.Editor.IsEditing = PathMediator!.EditPathPoints;

                    if (!mp.Editor.IsEditing)
                    {
                        mp.Editor.OnChanged!();
                    }

                }
            }
            else if (_selectedShape is MapSymbol symbol)
            {
                DeselectAllMapComponents(Scene!, symbol);
                symbol.IsSelected = true;
                _editorState.StatusMessage = "Symbol Selected " + (!string.IsNullOrEmpty(symbol.Name) ? ": " + symbol.Name : "");
                _selectedShape = symbol;
            }
            else if (_selectedShape is MapLabel label)
            {
                DeselectAllMapComponents(Scene!, label);
                label.IsSelected = true;
                _editorState.StatusMessage = "Label Selected ";
                _selectedShape = label;
            }
            else if (_selectedShape is WaterSystem waterSystem)
            {
                DeselectAllMapComponents(Scene!, waterSystem);
                waterSystem.IsSelected = true;
                _editorState.StatusMessage = "Water System Selected ";
            }
            else if (_selectedShape is WaterBody waterBody)
            {
                DeselectAllMapComponents(Scene!, waterBody);
                waterBody.IsSelected = true;
                _selectedShape = waterBody;

                if (_selectedShape is PaintedWaterBody pwb)
                {
                    _editorState.StatusMessage = "Painted Water Body Selected " + (!string.IsNullOrEmpty(pwb.Name) ? ": " + pwb.Name : "");
                }
                else if (_selectedShape is Lake l)
                {
                    _editorState.StatusMessage = "Lake Selected " + (!string.IsNullOrEmpty(l.Name) ? ": " + l.Name : "");
                }
                else if (_selectedShape is River river)
                {
                    _editorState.StatusMessage = "River Selected " + (!string.IsNullOrEmpty(river.Name) ? ": " + river.Name : "");
                }

                if (ActiveEditorTool is WaterBodyTool wbt)
                {
                    if (_selectedShape is River r)
                    {
                        r.Editor.IsEditing = wbt.IsRiverEditing;

                        if (r.Editor.IsEditing)
                        {
                            r.WaterSystem?.BeginInteractive();
                            r.BeginInteractive();
                        }
                        else
                        {
                            r.WaterSystem?.EndInteractive();
                            r.EndInteractive();

                            r.Editor.OnChanged!();
                        }
                    }
                }
            }

            _lastClickWorld = worldPoint;

            RequestRedraw();
        }

        public static void DeselectAllMapComponents(MapScene scene, ISelectable? selectedComponent)
        {
            for (int i = 0; i < scene.Map.MapLayers.Count; i++)
            {
                for (int j = 0; j < scene.Map.MapLayers[i].Shapes.Count; j++)
                {
                    if (scene.Map.MapLayers[i].Shapes[j] != selectedComponent)
                    {
                        scene.Map.MapLayers[i].Shapes[j].IsSelected = false;
                    }
                }
            }

            for (int i = 0; i < scene.Map.WaterSystems.Count; i++)
            {
                if (scene.Map.WaterSystems[i] != selectedComponent)
                {
                    scene.Map.WaterSystems[i].IsSelected = false;
                }

                for (int j = 0; j < scene.Map.WaterSystems[i].WaterBodies.Count; j++)
                {
                    if (scene.Map.WaterSystems[i].WaterBodies.ElementAt(j) != selectedComponent)
                    {
                        scene.Map.WaterSystems[i].WaterBodies.ElementAt(j).IsSelected = false;

                        if (scene.Map.WaterSystems[i].WaterBodies.ElementAt(j) is River r)
                        {
                            r.EndInteractive();
                            r.Editor.EndDraw();
                            r.Editor.IsEditing = false;
                            r.Editor.OnChanged!();
                        }
                    }
                }
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

    public sealed class SelectionFilterState(HashSet<Type> allowedTypes)
    {
        public HashSet<Type> AllowedTypes { get; } = [.. allowedTypes];

        public bool CurrentLayerOnly { get; }
        public bool VisibleLayersOnly { get; }

        public bool Allows(ISelectable shape)
        {
            return AllowedTypes.Count == 0 || AllowedTypes.Contains(shape.GetType());
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
