using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.ViewModels.Panels;
using SkiaSharp;

namespace RealmStudioX.WPF.Editor.Tools
{
    public sealed class WaterBodyTool(
        CommandManager commands,
        IAssetProvider assets,
        MapLayer targetLayer,
        MapScene scene,
        EditorState editorState,
        IWaterBodySettings waterBodySettings) : IToolEditor, IDisposable
    {
        // -------------------------------------------------
        // Dependencies
        // -------------------------------------------------

        private readonly CommandManager _commands = commands;
        private readonly MapLayer _layer = targetLayer;
        private readonly IAssetProvider _assets = assets;
        private readonly MapScene _scene = scene;
        private readonly EditorState _editorState = editorState;
        private readonly IWaterBodySettings _waterBodySettings = waterBodySettings;

        private readonly HashSet<WaterBody> _modifiedWaterBodies = [];

        private Cmd_ModifyWaterBodies? _activeModifyCommand;
        private River? _activeRiver;
        private PaintedWaterBody? _activePaintedWaterBody;
        private SKPoint _lastMouseWorld;
        private bool _painting;
        private bool disposedValue;

        public float WaterBrushRadius { get; set; } = 12f;

        public float WaterEraserBrushRadius { get; set; } = 12f;

        public bool EraseMode { get; set; }

        public WaterRenderSettings RenderSettings { get; set; } = new();

        public bool LinkWaterColors { get; set; } = true;

        public void Activate()
        {
    
        }

        public void Cancel()
        {

        }

        public void Deactivate()
        {

        }

        public void OnMouseDown(SKPoint worldPos, MouseButtons button)
        {
            _lastMouseWorld = worldPos;

            if (button == MouseButtons.Left)
            {
                if (_editorState.CurrentDrawingMode == MapDrawingMode.LakePaint)
                {
                    _activeModifyCommand = new Cmd_ModifyWaterBodies(_scene.Map);

                    CreateLake(worldPos);

                    _commands.Execute(_activeModifyCommand!);

                    _activeModifyCommand = null;
                    _modifiedWaterBodies.Clear();
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.RiverPaint)
                {
                    _activeRiver = new River
                    {
                        RenderSettings = WaterRenderSettings.Clone(RenderSettings)
                    };

                    _activeRiver.Editor.BeginDraw(worldPos);

                    _activeRiver.BeginInteractive();
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.WaterPaint)
                {
                    BeginPaint(worldPos);
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.WaterErase)
                {
                    _activeModifyCommand = new Cmd_ModifyWaterBodies(_scene.Map);
                }
            }

        }

        public void OnMouseMove(SKPoint worldPos, MouseButtons button)
        {
            _lastMouseWorld = worldPos;
            bool ctrl = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            bool shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

            if (button == MouseButtons.Left)
            {
                if (_editorState.CurrentDrawingMode == MapDrawingMode.RiverPaint)
                {
                    _activeRiver?.Editor.ContinueDraw(worldPos, ctrl, shift);
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.WaterPaint)
                {
                    if (_activePaintedWaterBody != null) 
                    {
                        ContinuePaint(worldPos);
                    }
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.WaterErase)
                {
                    _activeModifyCommand ??= new Cmd_ModifyWaterBodies(_scene.Map);

                    ApplyWaterErase(worldPos);
                }
            }
        }

        public void OnMouseUp(SKPoint worldPos, MouseButtons button)
        {
            _lastMouseWorld = worldPos;

            if (button == MouseButtons.Left)
            {
                if (_editorState.CurrentDrawingMode == MapDrawingMode.RiverPaint)
                {
                    if (_activeRiver != null)
                    {
                        _activeRiver.EndInteractive();
                        CommitRiver(_lastMouseWorld);

                        _activeRiver = null;
                    }
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.WaterPaint)
                {
                    if (_activePaintedWaterBody != null)
                    {
                        _activePaintedWaterBody.EndInteractive();
                        CommitPaintedWaterBody(_lastMouseWorld);

                        _activeModifyCommand = null;
                        _modifiedWaterBodies.Clear();

                        _activePaintedWaterBody = null;
                    }
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.WaterErase)
                {
                    if (_activeModifyCommand != null)
                    {
                        foreach (var body in _modifiedWaterBodies)
                        {
                            body.EndInteractive();
                            body.WaterSystem?.EndInteractive();
                            _activeModifyCommand!.CaptureAfter(body);
                        }

                        _commands.Execute(_activeModifyCommand);

                    }

                    _activeModifyCommand = null;
                    _modifiedWaterBodies.Clear();
                }
            }
        }

        public void OnMouseDown(PointerState state)
        {
            _lastMouseWorld = state.WorldPoint;

            if (state.Button == EditorMouseButton.Left)
            {
                if (_editorState.CurrentDrawingMode == MapDrawingMode.LakePaint)
                {
                    _activeModifyCommand = new Cmd_ModifyWaterBodies(_scene.Map);

                    CreateLake(state.WorldPoint);

                    _commands.Execute(_activeModifyCommand!);

                    _activeModifyCommand = null;
                    _modifiedWaterBodies.Clear();
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.RiverPaint)
                {
                    _activeRiver = new River
                    {
                        RenderSettings = WaterRenderSettings.Clone(RenderSettings)
                    };

                    _activeRiver.Editor.BeginDraw(state.WorldPoint);

                    _activeRiver.BeginInteractive();
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.WaterPaint)
                {
                    BeginPaint(state.WorldPoint);
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.WaterErase)
                {
                    _activeModifyCommand = new Cmd_ModifyWaterBodies(_scene.Map);
                }
            }
        }

        public void OnMouseMove(PointerState state)
        {
            _lastMouseWorld = state.WorldPoint;

            bool ctrl = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            bool shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

            if (state.Button == EditorMouseButton.Left)
            {
                if (_editorState.CurrentDrawingMode == MapDrawingMode.RiverPaint)
                {
                    _activeRiver?.Editor.ContinueDraw(state.WorldPoint, ctrl, shift);
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.WaterPaint)
                {
                    if (_activePaintedWaterBody != null)
                    {
                        ContinuePaint(state.WorldPoint);
                    }
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.WaterErase)
                {
                    _activeModifyCommand ??= new Cmd_ModifyWaterBodies(_scene.Map);

                    ApplyWaterErase(state.WorldPoint);
                }
            }
        }

        public void OnMouseUp(PointerState state)
        {
            _lastMouseWorld = state.WorldPoint;

            if (state.Button == EditorMouseButton.Left)
            {
                if (_editorState.CurrentDrawingMode == MapDrawingMode.RiverPaint)
                {
                    if (_activeRiver != null)
                    {
                        _activeRiver.EndInteractive();
                        CommitRiver(_lastMouseWorld);

                        _activeRiver = null;
                    }
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.WaterPaint)
                {
                    if (_activePaintedWaterBody != null)
                    {
                        _activePaintedWaterBody.EndInteractive();
                        CommitPaintedWaterBody(_lastMouseWorld);

                        _activeModifyCommand = null;
                        _modifiedWaterBodies.Clear();

                        _activePaintedWaterBody = null;
                    }
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.WaterErase)
                {
                    if (_activeModifyCommand != null)
                    {
                        foreach (var body in _modifiedWaterBodies)
                        {
                            body.EndInteractive();
                            body.WaterSystem?.EndInteractive();
                            _activeModifyCommand!.CaptureAfter(body);
                        }

                        _commands.Execute(_activeModifyCommand);

                    }

                    _activeModifyCommand = null;
                    _modifiedWaterBodies.Clear();
                }
            }
        }

        public void OnMouseDoubleClick(PointerState state)
        {
            // no action
        }

        public void OnMouseWheel(PointerState state)
        {
            // no action
        }

        public void BeginPaint(SKPoint worldPos)
        {

            _activePaintedWaterBody = new()
            {
                RenderSettings = WaterRenderSettings.Clone(RenderSettings),
                BrushRadius = WaterBrushRadius
            };

            _activePaintedWaterBody.ControlPoints.Add(worldPos);

            _activePaintedWaterBody.BeginStroke(worldPos);
            _activePaintedWaterBody.BeginInteractive();

            _painting = true;
        }

        public void ContinuePaint(SKPoint worldPos)
        {
            if (!_painting || _activePaintedWaterBody == null)
                return;

            _activePaintedWaterBody.AddStrokePoint(worldPos);
        }

        public void EndPaint(SKPoint worldPos)
        {
            if (!_painting || _activePaintedWaterBody == null)
            {
                return;
            }

            _activePaintedWaterBody.EndStroke();
            _activePaintedWaterBody.EndInteractive();
            _painting = false;

            CommitPaintedWaterBody(worldPos);
        }


        private void CreateLake(SKPoint worldPos)
        {
            if (_scene == null)
            {
                return;
            }

            // TODO: does clipping have to happen here, or is the clipping done during rendering sufficient?
            // Find landform under cursor

            // landforms are on the landform layer
            MapLayer landformLayer = MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.LANDFORMLAYER);

            var landform = landformLayer
                .Shapes
                .OfType<Landform>()
                .Reverse()
                .FirstOrDefault(lf =>
                    lf.HitPath.Contains(worldPos.X, worldPos.Y));

            if (landform == null)
                return;

            using var lakePath =
                WaterGeometryBuilder.GenerateRandomLakePath(
                    worldPos,
                    WaterBrushRadius * 2);

            // Clip lake to landform
            using var clipped =
                lakePath.Op(landform.HitPath, SKPathOp.Intersect);

            if (clipped == null || clipped.IsEmpty)
                return;

            var newLake = new Lake();

            newLake.RestoreGeometry(new SKPath(clipped));
            newLake.RenderSettings = WaterRenderSettings.Clone(RenderSettings);

            // Merge with existing lakes
            var intersectingLakes = FindIntersectingLakes(newLake.HitPath);

            if (intersectingLakes.Count > 0)
            {
                // Capture BEFORE merge destroys geometry
                foreach (var l in intersectingLakes)
                {
                    _activeModifyCommand?.CaptureBefore(l);
                    _activeModifyCommand?.RegisterRemovedWaterBody(l);
                }

                var all = intersectingLakes.Append(newLake);

                var mergedLake = MergeOverlappingLakes(all);

                newLake = mergedLake;
            }

            var intersectingSystems = FindIntersectingSystems(newLake.HitPath);

            WaterSystem system;

            if (intersectingSystems.Count == 0)
            {
                system = new WaterSystem
                {
                    RenderSettings = WaterRenderSettings.Clone(RenderSettings)
                };
                _activeModifyCommand?.RegisterAddedWaterSystem(system);
            }
            else if (intersectingSystems.Count == 1)
            {
                system = intersectingSystems[0];
            }
            else
            {
                system = MergeWaterSystems(intersectingSystems, _activeModifyCommand!);
            }

            system.Add(newLake);
            _activeModifyCommand?.RegisterAddedWaterBody(newLake);
            _activeModifyCommand?.CaptureAfter(newLake);
        }

        private void CommitRiver(SKPoint worldPos)
        {
            if (_activeRiver == null)
            {
                return;
            }

            var river = _activeRiver;

            if (river.HitPath == null || river.HitPath.IsEmpty)
            {
                return;
            }

            river.ControlPoints.Add(worldPos);

            if (river.Bounds.Width < 3 && river.Bounds.Height < 3)
            {
                return;
            }

            river.Editor.EndDraw();

            var cmd = new Cmd_ModifyWaterBodies(_scene.Map);

            var intersectingSystems = FindIntersectingSystems(river.HitPath);

            WaterSystem system;

            if (intersectingSystems.Count == 0)
            {
                system = new WaterSystem
                {
                    RenderSettings = WaterRenderSettings.Clone(RenderSettings)
                };
                cmd.RegisterAddedWaterSystem(system);
            }
            else if (intersectingSystems.Count == 1)
            {
                system = intersectingSystems[0];
            }
            else
            {
                system = MergeWaterSystems(intersectingSystems, cmd);
            }

            system.Add(river);

            cmd.RegisterAddedWaterBody(river);

            _commands.Execute(cmd);
        }

        private void CommitPaintedWaterBody(SKPoint worldPos)
        {
            if (_activePaintedWaterBody == null)
            {
                return;
            }

            var paintedWaterBody = _activePaintedWaterBody;

            if (paintedWaterBody.HitPath == null || paintedWaterBody.HitPath.IsEmpty)
            {
                return;
            }

            paintedWaterBody.ControlPoints.Add(worldPos);

            if (paintedWaterBody.Bounds.Width < 3 && paintedWaterBody.Bounds.Height < 3)
            {
                return;
            }

            _activeModifyCommand = new Cmd_ModifyWaterBodies(_scene.Map);

            var intersectingSystems = FindIntersectingSystems(paintedWaterBody.HitPath);

            WaterSystem system;

            if (intersectingSystems.Count == 0)
            {
                system = new WaterSystem
                {
                    RenderSettings = WaterRenderSettings.Clone(RenderSettings)
                };
                _activeModifyCommand.RegisterAddedWaterSystem(system);
            }
            else if (intersectingSystems.Count == 1)
            {
                system = intersectingSystems[0];
            }
            else
            {
                system = MergeWaterSystems(intersectingSystems, _activeModifyCommand);
            }

            foreach (var existing in system.WaterBodies)
            {
                _activeModifyCommand!.CaptureBefore(existing);
            }

            system.Add(paintedWaterBody);

            _activeModifyCommand.RegisterAddedWaterBody(paintedWaterBody);

            _commands.Execute(_activeModifyCommand);
        }

        private static Lake MergeOverlappingLakes(IEnumerable<Lake> lakes)
        {
            SKPath merged = new();

            foreach (var lake in lakes)
            {
                merged = merged.IsEmpty ? new SKPath(lake.HitPath) : merged.Op(lake.HitPath, SKPathOp.Union);
            }

            var result = new Lake();

            result.RestoreGeometry(merged);

            return result;
        }

        private List<Lake> FindIntersectingLakes(SKPath newLakePath)
        {
            var result = new List<Lake>();

            var bounds = newLakePath.Bounds;

            foreach (var system in _scene.Map.WaterSystems)
            {
                foreach (var body in system.WaterBodies)
                {
                    if (body is not Lake lake)
                        continue;

                    // Fast rejection
                    if (!lake.Bounds.IntersectsWith(bounds))
                        continue;

                    using var intersection = lake.HitPath.Op(newLakePath, SKPathOp.Intersect);

                    if (intersection != null && !intersection.IsEmpty)
                    {
                        result.Add(lake);
                    }
                }
            }

            return result;
        }

        private List<WaterSystem> FindIntersectingSystems(SKPath path)
        {
            var result = new List<WaterSystem>();
            var bounds = path.Bounds;

            foreach (var system in _scene.Map.WaterSystems)
            {
                var merged = system.MergedGeometry;

                if (merged == null || merged.IsEmpty)
                    continue;

                // Fast rejection
                if (!system.Bounds.IntersectsWith(bounds))
                    continue;

                using var intersection = merged.Op(path, SKPathOp.Intersect);

                if (intersection != null && !intersection.IsEmpty)
                {
                    result.Add(system);
                }
            }

            return result;
        }

        private static WaterSystem MergeWaterSystems(List<WaterSystem> systems, Cmd_ModifyWaterBodies cmd)
        {
            var main = systems[0];

            for (int i = 1; i < systems.Count; i++)
            {
                var other = systems[i];

                foreach (WaterBody body in other.WaterBodies)
                {
                    main.Add(body);
                }

                cmd.RegisterRemovedWaterSystem(other);
            }

            return main;
        }

        // -------------------------------------------------
        // Erasing
        // -------------------------------------------------

        private void ApplyWaterErase(SKPoint worldPos)
        {
            float r = WaterEraserBrushRadius;

            using var erasePath = new SKPath();
            erasePath.AddCircle(worldPos.X, worldPos.Y, r);

            foreach (var system in _scene.Map.WaterSystems)
            {
                bool systemModified = false;

                foreach (var body in system.WaterBodies.ToList())
                {
                    if (!body.Bounds.IntersectsWith(erasePath.Bounds))
                    {
                        continue;
                    }

                    using var intersection = body.HitPath.Op(erasePath, SKPathOp.Intersect);

                    if (intersection == null || intersection.IsEmpty)
                    {
                        continue;
                    }

                    if (!_modifiedWaterBodies.Contains(body))
                    {
                        _activeModifyCommand!.CaptureBefore(body);

                        body.BeginInteractive();
                        body.WaterSystem?.BeginInteractive();

                        _modifiedWaterBodies.Add(body);
                    }

                    //-------------------------------------------------
                    // River erase (endpoint trimming only)
                    //-------------------------------------------------

                    if (body is River river)
                    {
                        var pts = river.ControlPoints;

                        if (pts.Count < 2)
                            continue;

                        bool modified = false;

                        float effectiveRadius = r + river.RenderSettings.RiverWidth * 0.5f;

                        // upstream trim
                        while (pts.Count > 2 &&
                               SKPoint.Distance(pts[0], worldPos) < effectiveRadius)
                        {
                            pts.RemoveAt(0);
                            modified = true;
                        }

                        // downstream trim
                        while (pts.Count > 2 &&
                               SKPoint.Distance(pts[^1], worldPos) < effectiveRadius)
                        {
                            pts.RemoveAt(pts.Count - 1);
                            modified = true;
                        }

                        if (pts.Count < 2)
                        {
                            system.Remove(river);
                            _activeModifyCommand!.RegisterRemovedWaterBody(river);
                            continue;
                        }

                        if (modified)
                        {
                            river.RebuildGeometry();
                            systemModified = true;
                        }

                        continue;
                    }

                    //-------------------------------------------------
                    // Lake / PaintedWaterBody erase
                    //-------------------------------------------------

                    var newPath = body.HitPath.Op(erasePath, SKPathOp.Difference);

                    if (newPath == null || newPath.IsEmpty)
                    {
                        system.Remove(body);
                        _activeModifyCommand!.RegisterRemovedWaterBody(body);
                        continue;
                    }

                    body.ReplaceGeometry(newPath);
                    systemModified = true;
                }

                if (systemModified)
                {
                    system.InvalidateRenderCache();
                }
            }
        }

        public void RenderOverlay(SKCanvas canvas, SKPoint world)
        {
            using (new SKAutoCanvasRestore(canvas))
            {
                if (_activeRiver != null)
                {
                    canvas.ClipPath(_scene.GetLandClipPath());
                    _activeRiver?.RenderInteractive(canvas);
                }
                else if (_activePaintedWaterBody != null)
                {
                    canvas.ClipPath(_scene.GetLandClipPath());
                    _activePaintedWaterBody?.RenderInteractive(canvas);
                }
            }

            if (_editorState.CurrentDrawingMode == MapDrawingMode.LakePaint
                || _editorState.CurrentDrawingMode == MapDrawingMode.WaterPaint)
            {
                canvas.DrawCircle(
                    world,
                    WaterBrushRadius,
                    PaintObjects.CursorCirclePaint);
            }
            else if (_editorState.CurrentDrawingMode == MapDrawingMode.WaterErase)
            {
                canvas.DrawCircle(
                    world,
                    WaterEraserBrushRadius,
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
        // ~WaterFeatureTool()
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
