using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Panels;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Windows.Input;
using CommandManager = RealmStudioX.Core.CommandManager;

namespace RealmStudioX.WPF.Editor.Tools
{
    internal class RegionTool(
            CommandManager commands,
            IAssetProvider assets,
            MapLayer targetLayer,
            EditorController editor,
            MapScene scene,
            EditorState editorState,
            FontManager fontManager,
            IRedrawRequester redraw,
            IRegionSettings settings) : IToolEditor, IKeyHandler, IDisposable
    {

        private const int _pointCircleRadius = 5;
        private const int _maxPointToCoastlineDistance = 15;
        private bool disposedValue;
        // -------------------------------------------------
        // Dependencies
        // -------------------------------------------------

        private readonly CommandManager _commands = commands;
        private readonly MapLayer _layer = targetLayer;
        private readonly EditorController _editor = editor;
        private readonly IAssetProvider _assets = assets;
        private readonly MapScene _scene = scene;
        private readonly EditorState _editorState = editorState;
        private readonly FontManager _fontManager = fontManager;
        private readonly IRedrawRequester _redraw = redraw;
        private readonly IRegionSettings _settings = settings;

        private MapLayer? _regionLayer;
        private MapRegion? _currentRegion = null;
        private MapRegion? _selectedRegion = null;
        public MapRegion? SelectedRegion
        {
            get => _selectedRegion;
            set
            {
                _selectedRegion = value;
            }
        }

        private SKPoint _prevMouseWorldPoint;

        private int _previousRegionPointIndex = -1;
        private int _nextRegionPointIndex = -1;

        private SKPoint _snappedStartPoint = SKPoint.Empty;

        private MapRegionPoint? _selectedRegionPoint = null;
        private SKPoint _newRegionPoint = SKPoint.Empty;

        private Landform? _snappedLandform = null;

        private bool _editingRegion = false;

        private Cmd_ModifyRegions? _activeModifyCommand;

        public void Activate()
        {
            _regionLayer = MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.REGIONLAYER);
        }

        public void Cancel()
        {

        }

        public void Deactivate()
        {

        }

        public void Reset()
        {
            _snappedStartPoint = SKPoint.Empty;
            _currentRegion = null;
            _prevMouseWorldPoint = SKPoint.Empty;
            _selectedRegion = null;
            _selectedRegionPoint = null;
            _newRegionPoint = SKPoint.Empty;
            _snappedLandform = null;
            _editingRegion = false;

            _previousRegionPointIndex = -1;
            _nextRegionPointIndex = -1;

            _editorState.CurrentDrawingMode = MapDrawingMode.None;
        }
        internal void UpdatedSelectedRegion(IRegionSettings regionSettings)
        {
            if (_selectedRegion != null)
            {
                MapRegionState beforeState = (MapRegionState)_selectedRegion.CaptureState();

                _selectedRegion.RegionBorderType = regionSettings.RegionStyle;
                _selectedRegion.RegionBorderColor = regionSettings.RegionColor.ToSKColor();
                _selectedRegion.RegionBorderWidth = regionSettings.RegionBorderWidth;
                _selectedRegion.RegionBorderSmoothing = regionSettings.Smoothing;
                _selectedRegion.RegionInnerOpacity = regionSettings.InnerOpacity;

                MapRegionState afterState = (MapRegionState)_selectedRegion.CaptureState();

                _activeModifyCommand = new(_regionLayer!);
                _activeModifyCommand.RegisterModifiedRegion(_selectedRegion, beforeState, afterState);

                _commands.Execute(_activeModifyCommand);
            }
        }


        public void OnMouseDoubleClick(PointerState state)
        {
            // no action
        }

        public void OnMouseDown(PointerState state)
        {
            _prevMouseWorldPoint = state.WorldPoint;

            if (_editorState.CurrentDrawingMode == MapDrawingMode.RegionPaint)
            {
                if (state.Button is EditorMouseButton.Left)
                {
                    _currentRegion ??= new()
                    {
                        RegionBorderType = _settings.RegionStyle,
                        RegionBorderColor = _settings.RegionColor.ToSKColor(),
                        RegionBorderWidth = _settings.RegionBorderWidth,
                        RegionBorderSmoothing = _settings.Smoothing,
                        RegionInnerOpacity = _settings.InnerOpacity,
                    };

                    _currentRegion.ConstructRegionPaint();

                    _snappedStartPoint = SKPoint.Empty;
                    _newRegionPoint = SKPoint.Empty;
                    _selectedRegion = null;

                    MapRegionState beforeState = (MapRegionState)_currentRegion.CaptureState();

                    // set a point of the current region to the current mouse position
                    _currentRegion.AddRegionPoint(state.WorldPoint);
                    
                    MapRegionState afterState = (MapRegionState)_currentRegion.CaptureState();

                    _activeModifyCommand = new(_regionLayer!);
                    _activeModifyCommand.RegisterModifiedRegion(_currentRegion, beforeState, afterState);
                    
                    _commands.Execute(_activeModifyCommand);

                    _activeModifyCommand?.Dispose();
                    _activeModifyCommand = null;
                }

                if (state.Button is EditorMouseButton.Right)
                {
                    if (_currentRegion != null)
                    {
                        // commit the new region to the map and reset the tool state
                        _currentRegion.AddRegionPoint(state.WorldPoint);

                        // undo/redo support for region creation
                        _activeModifyCommand = new(_regionLayer!);
                        _activeModifyCommand.RegisterNewRegion(_currentRegion);
                        _commands.Execute(_activeModifyCommand);

                        _activeModifyCommand?.Dispose();
                        _activeModifyCommand = null;

                        Reset();
                    }
                }
            }

            if (_editorState.CurrentDrawingMode == MapDrawingMode.ShapeSelect || _editorState.CurrentDrawingMode == MapDrawingMode.RegionSelect)
            {
                if (state.Button is EditorMouseButton.Left)
                {
                    if (_editor.SelectedShape is MapRegion mr)
                    {
                        _selectedRegion = mr;
                    }
                    else
                    {
                        _selectedRegion = null;
                        return;
                    }

                    if (state.Modifiers == InputModifiers.Control)
                    {
                        // if control key is held down, add _newRegionPoint to the selected region
                        if (_newRegionPoint != SKPoint.Empty && _selectedRegion != null && _nextRegionPointIndex >= 0)
                        {
                            // undo/redo support for inserting a region point
                            MapRegionState beforeState = (MapRegionState)_selectedRegion.CaptureState();

                            _selectedRegion.InsertRegionPoint(_nextRegionPointIndex, _newRegionPoint);

                            MapRegionState afterState = (MapRegionState)_selectedRegion.CaptureState();

                            _activeModifyCommand = new(_regionLayer!);
                            _activeModifyCommand.RegisterModifiedRegion(_selectedRegion, beforeState, afterState);
                            _commands.Execute(_activeModifyCommand);

                            _activeModifyCommand?.Dispose();
                            _activeModifyCommand = null;

                            // reset
                            _newRegionPoint = SKPoint.Empty;
                            _nextRegionPointIndex = -1;
                            _previousRegionPointIndex = -1;
                        }
                    }
                }
            }

            _redraw.RequestRedraw();
        }

        public void OnMouseMove(PointerState state)
        {
            if (_currentRegion == null)
            {
                if (state.Button is EditorMouseButton.None)
                {
                    if (state.Modifiers == InputModifiers.Shift)
                    {
                        // find the closest landform point to the current mouse position and snap to it
                        SKPoint closestPoint = GetClosestLandformStartPoint(state.WorldPoint);
                        _snappedStartPoint = closestPoint;  // may be SKPoint.Empty if no landform point is close enough to snap to
                    }
                }
            }
            else if (_currentRegion != null)    // _current region is set
            {
                if (!_currentRegion.IsSelected)     // _currentRegion is not selected
                {
                    if (state.Button is EditorMouseButton.Left) // left mouse button is held down
                    {
                        if (state.Modifiers == InputModifiers.Shift)    // shift button is held down
                        {
                            // snap to the closest landform point while drawing the region
                            SKPoint closestPoint = GetClosestLandformPoint(state.WorldPoint);
                            
                            if (closestPoint != SKPoint.Empty)
                            {
                                int _minSegmentDistance = 10;

                                // check if the closest landform point is
                                // a reasonable distance from the last point of the
                                // current region to avoid creating very small segments
                                // in the region when snapping to landform points that are close to the current region vertices

                                // undo/redo support for adding a snapped region point

                                if (SKPoint.Distance(closestPoint, _currentRegion.MapRegionPoints[^1].RegionPoint) > _minSegmentDistance)
                                {
                                    MapRegionState beforeState = (MapRegionState)_currentRegion.CaptureState();

                                    _currentRegion.AddRegionPoint(closestPoint);

                                    MapRegionState afterState = (MapRegionState)_currentRegion.CaptureState();

                                    _activeModifyCommand = new(_regionLayer!);
                                    _activeModifyCommand.RegisterModifiedRegion(_currentRegion, beforeState, afterState);
                                    _commands.Execute(_activeModifyCommand);

                                    _activeModifyCommand?.Dispose();
                                    _activeModifyCommand = null;
                                }
                            }
                        }
                    }
                }
            }

            if (_selectedRegion != null) // _currentRegion is selected
            {
                if (state.Button == EditorMouseButton.None)
                {
                    // select the region point under the mouse cursor
                    _selectedRegionPoint = GetSelectedMapRegionPoint(_selectedRegion, state.WorldPoint);

                    if (_selectedRegionPoint != null)
                    {
                        _selectedRegion.IsEditing = true;
                    }
                    else
                    {
                        _newRegionPoint = PointOnRegionSegment(_selectedRegion, state.WorldPoint);
                        _selectedRegion.IsEditing = _newRegionPoint != SKPoint.Empty;
                    }
                }

                if (state.Button is EditorMouseButton.Left && _selectedRegionPoint != null)
                {
                    if (_selectedRegionPoint != null)    // a region point is selected
                    {
                        _selectedRegion.IsEditing = true;

                        // undo/redo is not supported for region point movement
                        // move the selected region point to the current mouse position
                        _selectedRegion.MoveRegionPoint(_selectedRegionPoint, state.WorldPoint);
                        _editor.MarkMapModified();
                    }
                }

                if (state.Button is EditorMouseButton.Left && _selectedRegionPoint == null)
                {
                    float dx = state.WorldPoint.X - _prevMouseWorldPoint.X;
                    float dy = state.WorldPoint.Y - _prevMouseWorldPoint.Y;

                    // undo/redo is not supported for region movement
                    _selectedRegion.Move(dx, dy);
                    _editor.MarkMapModified();
                }
            }

            _prevMouseWorldPoint = state.WorldPoint;

            // draw the region (causes RenderOverlay to be called)
            _redraw.RequestRedraw();
        }

        public void OnMouseUp(PointerState state)
        {
            if (_selectedRegionPoint != null)
            {
                _selectedRegionPoint.IsSelected = false;
                _selectedRegionPoint = null;
            }

            if (_selectedRegion != null)
            {
                _selectedRegion.IsEditing = false;
            }

            _snappedStartPoint = SKPoint.Empty;
        }

        public void OnMouseWheel(PointerState state)
        {
            // no action
        }

        public bool OnKeyDown(Key key)
        {
            if (key == Key.Escape)
            {
                Reset();
                return true;
            }

            if (key == Key.Delete)
            {
                if (_selectedRegion != null)
                {
                    if (_selectedRegionPoint != null)
                    {
                        // undo/redo support for region point deletion
                        // delete the selected region point
                        _activeModifyCommand = new(_regionLayer!);
                        
                        MapRegionState beforeState = (MapRegionState)_selectedRegion.CaptureState();
                        
                        _selectedRegion.RemoveRegionPoint(_selectedRegionPoint);
                        
                        MapRegionState afterState = (MapRegionState)_selectedRegion.CaptureState();
                        
                        _activeModifyCommand.RegisterModifiedRegion(_selectedRegion, beforeState, afterState);
                        
                        _commands.Execute(_activeModifyCommand);
                        
                        _activeModifyCommand?.Dispose();
                        _activeModifyCommand = null;
                        _selectedRegionPoint = null;

                        _redraw.RequestRedraw();
                        return true;
                    }
                    else
                    {
                        // undo/redo support for region deletion
                        // delete the selected region
                        _activeModifyCommand = new(_regionLayer!);

                        _activeModifyCommand.RegisterRemovedRegion(_selectedRegion);

                        _commands.Execute(_activeModifyCommand);

                        _activeModifyCommand?.Dispose();
                        _activeModifyCommand= null;

                        _selectedRegion = null;
                        _redraw.RequestRedraw();

                        return true;
                    }
                }
            }

            return false;
        }

        public bool OnKeyPress(char c)
        {
            return false;
        }

        public SKPoint PointOnRegionSegment(MapRegion region, SKPoint worldPoint)
        {
            SKPoint segmentPoint = SKPoint.Empty;

            // is the cursor on a line segment between 2 region vertices? if so, draw a circle at the cursor location
            for (int i = 0; i < region.MapRegionPoints.Count - 1; i++)
            {
                if (RealmStudioShapeRenderingLib.Utilities.LineContainsPoint(worldPoint,
                    region.MapRegionPoints[i].RegionPoint, region.MapRegionPoints[i + 1].RegionPoint))
                {
                    _editingRegion = true;

                    _previousRegionPointIndex = i;
                    _nextRegionPointIndex = i + 1;

                    segmentPoint = worldPoint;

                    break;
                }
            }

            // is the cursor on the segment between the first and last region vertices?
            if (RealmStudioShapeRenderingLib.Utilities.LineContainsPoint(worldPoint, region.MapRegionPoints[0].RegionPoint,
                region.MapRegionPoints[^1].RegionPoint))
            {
                _editingRegion = true;

                _previousRegionPointIndex = 0;
                _nextRegionPointIndex = region.MapRegionPoints.Count;

                segmentPoint = worldPoint;
            }

            return segmentPoint;
        }

        internal MapRegionPoint? GetSelectedMapRegionPoint(MapRegion mapRegion, SKPoint worldPoint)
        {
            MapRegionPoint? selectedMapRegionPoint = null;

            foreach (MapRegionPoint p in mapRegion.MapRegionPoints)
            {
                using SKPath path = new();
                path.AddCircle(p.RegionPoint.X, p.RegionPoint.Y, 5);

                if (path.Contains(worldPoint.X, worldPoint.Y))
                {
                    // editing (moving) the selected point
                    p.IsSelected = true;
                    selectedMapRegionPoint = p;
                }
                else
                {
                    p.IsSelected = false;
                }
            }

            return selectedMapRegionPoint;
        }

        private SKPoint GetClosestLandformStartPoint(SKPoint worldPoint)
        {
            SKPoint closestPoint = SKPoint.Empty;
            MapLayer landformLayer = MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.LANDFORMLAYER);

            int coastlinePointIndex = -1;
            float currentDistance = float.MaxValue;

            // get the distance from the cursor point to the contour points of all landforms
            foreach (Shape2D s2d in landformLayer.Shapes)
            {
                if (s2d is Landform lf)
                {
                    for (int i = 0; i < lf.PerimeterPath.PointCount; i++)
                    {
                        SKPoint p = lf.PerimeterPath[i];
                        float distance = SKPoint.Distance(worldPoint, p);

                        if (distance < currentDistance && distance < _maxPointToCoastlineDistance)
                        {
                            coastlinePointIndex = i;
                            closestPoint = p;
                            currentDistance = distance;
                        }
                    }

                    if (coastlinePointIndex >= 0)
                    {
                        _snappedLandform = lf;
                        break;
                    }
                }
            }

            return closestPoint;
        }

        private SKPoint GetClosestLandformPoint(SKPoint worldPoint)
        {
            SKPoint closestPoint = SKPoint.Empty;
            float currentDistance = float.MaxValue;

            // get the distance from the cursor point to the contour points of all landforms
            if (_snappedLandform != null)
            {
                for (int i = 0; i < _snappedLandform.PerimeterPath.PointCount; i++)
                {
                    SKPoint p = _snappedLandform.PerimeterPath[i];
                    float distance = SKPoint.Distance(worldPoint, p);

                    if (distance < currentDistance && distance < _maxPointToCoastlineDistance)
                    {
                        closestPoint = p;
                        currentDistance = distance;
                    }
                }
            }

            return closestPoint;
        }

        public void RenderOverlay(SKCanvas canvas, SKPoint world)
        {
            try
            {
                if (_snappedStartPoint != SKPoint.Empty && _currentRegion == null)
                {
                    canvas.DrawCircle(_snappedStartPoint, _pointCircleRadius, PaintObjects.RegionPointSnapStartPaint);
                    canvas.DrawCircle(_snappedStartPoint, _pointCircleRadius, PaintObjects.RegionPointOutlinePaint);
                }
                else if (_newRegionPoint != SKPoint.Empty && _selectedRegion != null)
                {
                    canvas.DrawCircle(_newRegionPoint, _pointCircleRadius, PaintObjects.RegionNewPointFillPaint);
                    canvas.DrawCircle(_newRegionPoint, _pointCircleRadius, PaintObjects.RegionPointOutlinePaint);
                }
                else if (_currentRegion != null)
                {
                    if (_currentRegion.RegionPointCount == 1)
                    {
                        canvas.DrawLine(_currentRegion.MapRegionPoints[0].RegionPoint, world, _currentRegion.RegionBorderPaint);
                    }
                    else
                    {
                        _currentRegion.AddRegionPoint(world);
                        _currentRegion.Render(canvas);
                        _currentRegion.RemoveRegionPointAt(_currentRegion.RegionPointCount - 1);
                    }
                }
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
        // ~WindroseTool()
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
