using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using SkiaSharp;
using System.Windows.Input;

namespace RealmStudioX.WPF.Editor.Services
{
    public class SelectionService : ViewModelBase
    {
        private const float SelectionCycleDistance = 8f;

        public event EventHandler? SelectionChanged;

        private readonly SelectionFilter _selectionFilter = new();

        public SelectionFilter SelectionFilter => _selectionFilter;

        private readonly List<ISelectable> _selectedObjects = [];

        private SKPoint _lastClickPoint;

        private List<ISelectable> _lastCandidates = [];

        private int _candidateIndex;

        public List<ISelectable> SelectedObjects { get { return _selectedObjects; } }

        public ISelectable? PrimarySelection =>  _selectedObjects.Count > 0 ? _selectedObjects[0] : null;

        public bool HasSelection => _selectedObjects.Count > 0;

        public bool HasSingleSelection => _selectedObjects.Count == 1;

        public bool HasMultipleSelection => _selectedObjects.Count > 1;

        public int SelectionCount => _selectedObjects.Count;

        private bool _landformSelectionAllowed = true;

        public SelectionService()
        {
            // Initialize the selection filter with all types allowed
            _selectionFilter.SelectableTypes.Add(typeof(Landform));
            _selectionFilter.SelectableTypes.Add(typeof(WaterSystem));
            _selectionFilter.SelectableTypes.Add(typeof(Lake));
            _selectionFilter.SelectableTypes.Add(typeof(River));
            _selectionFilter.SelectableTypes.Add(typeof(PaintedWaterBody));
            _selectionFilter.SelectableTypes.Add(typeof(MapPath));
            _selectionFilter.SelectableTypes.Add(typeof(MapLabel));
            _selectionFilter.SelectableTypes.Add(typeof(PlacedMapBox));
            _selectionFilter.SelectableTypes.Add(typeof(MapRegion));
            _selectionFilter.SelectableTypes.Add(typeof(IDrawnMapComponent));
            _selectionFilter.SelectableTypes.Add(typeof(MapSymbol));

            _selectionFilter.AllowsStructures = true;
            _selectionFilter.AllowsVegetation = true;
            _selectionFilter.AllowsTerrain = true;
            _selectionFilter.AllowsMarkers = true;
        }

        private bool _objectPropertiesPopupSuppressed;

        public bool ObjectPropertiesPopupSuppressed
        {
            get => _objectPropertiesPopupSuppressed;
            set
            {
                if (_objectPropertiesPopupSuppressed == value)
                    return;

                _objectPropertiesPopupSuppressed = value;
                OnPropertyChanged(nameof(PropertiesPopupVisible));
            }
        }

        public bool PropertiesPopupVisible =>
            !_objectPropertiesPopupSuppressed &&
            PrimarySelection != null &&
            (PrimarySelection is Landform
             || PrimarySelection is WaterSystem
             || PrimarySelection is Lake
             || PrimarySelection is River
             || PrimarySelection is PaintedWaterBody
             || PrimarySelection is MapPath
             || PrimarySelection is MapRegion
             || PrimarySelection is MapSymbol);


        private float _propertiesPopupLeft = 0;
        public float PropertiesPopupLeft
        {
            get => _propertiesPopupLeft;
            set
            {
                if (_propertiesPopupLeft == value)
                    return;

                _propertiesPopupLeft = value;
                OnPropertyChanged();
            }
        }

        private float _propertiesPopupTop = 0;
        public float PropertiesPopupTop
        {
            get => _propertiesPopupTop;
            set
            {
                if (_propertiesPopupTop == value)
                    return;

                _propertiesPopupTop = value;
                OnPropertyChanged();
            }
        }

        public bool LandformSelectionAllowed
        {
            get { return _landformSelectionAllowed; }
            set
            {
                _landformSelectionAllowed = value;
                OnPropertyChanged(nameof(LandformSelectionAllowed));

                if (value)
                {
                    _selectionFilter.SelectableTypes.Add(typeof(Landform));
                }
                else
                {
                    _selectionFilter.SelectableTypes.Remove(typeof(Landform));
                }
            }
        }

        private bool _waterSystemSelectionAllowed = true;
        public bool WaterSystemSelectionAllowed
        {
            get { return _waterSystemSelectionAllowed; }
            set
            {
                _waterSystemSelectionAllowed = value;
                OnPropertyChanged(nameof(WaterSystemSelectionAllowed));
                if (value)
                {
                    _selectionFilter.SelectableTypes.Add(typeof(WaterSystem));
                }
                else
                {
                    _selectionFilter.SelectableTypes.Remove(typeof(WaterSystem));
                }
            }
        }

        private bool _lakeSelectionAllowed = true;

        public bool LakeSelectionAllowed
        {
            get { return _lakeSelectionAllowed; }
            set
            {
                _lakeSelectionAllowed = value;
                OnPropertyChanged(nameof(LakeSelectionAllowed));
                if (value)
                {
                    _selectionFilter.SelectableTypes.Add(typeof(Lake));
                }
                else
                {
                    _selectionFilter.SelectableTypes.Remove(typeof(Lake));
                }
            }
        }

        private bool _riverSelectionAllowed = true;

        public bool RiverSelectionAllowed
        {
            get { return _riverSelectionAllowed; }
            set
            {
                _riverSelectionAllowed = value;
                OnPropertyChanged(nameof(RiverSelectionAllowed));
                if (value)
                {
                    _selectionFilter.SelectableTypes.Add(typeof(River));
                }
                else
                {
                    _selectionFilter.SelectableTypes.Remove(typeof(River));
                }
            }
        }

        private bool _paintedWaterBodySelectionAllowed = true;

        public bool PaintedWaterBodySelectionAllowed
        {
            get { return _paintedWaterBodySelectionAllowed; }
            set
            {
                _paintedWaterBodySelectionAllowed = value;
                OnPropertyChanged(nameof(PaintedWaterBodySelectionAllowed));
                if (value)
                {
                    _selectionFilter.SelectableTypes.Add(typeof(PaintedWaterBody));
                }
                else
                {
                    _selectionFilter.SelectableTypes.Remove(typeof(PaintedWaterBody));
                }
            }
        }

        private bool _pathSelectionAllowed = true;

        public bool PathSelectionAllowed
        {
            get { return _pathSelectionAllowed; }
            set
            {
                _pathSelectionAllowed = value;
                OnPropertyChanged(nameof(PathSelectionAllowed));
                if (value)
                {
                    _selectionFilter.SelectableTypes.Add(typeof(MapPath));
                }
                else
                {
                    _selectionFilter.SelectableTypes.Remove(typeof(MapPath));
                }
            }
        }

        private bool _labelSelectionAllowed = true;

        public bool LabelSelectionAllowed
        {
            get { return _labelSelectionAllowed; }
            set
            {
                _labelSelectionAllowed = value;
                OnPropertyChanged(nameof(LabelSelectionAllowed));
                if (value)
                {
                    _selectionFilter.SelectableTypes.Add(typeof(MapLabel));
                }
                else
                {
                    _selectionFilter.SelectableTypes.Remove(typeof(MapLabel));
                }
            }
        }

        private bool _boxSelectionAllowed = true;

        public bool BoxSelectionAllowed
        {
            get { return _boxSelectionAllowed; }
            set
            {
                _boxSelectionAllowed = value;
                OnPropertyChanged(nameof(BoxSelectionAllowed));
                if (value)
                {
                    _selectionFilter.SelectableTypes.Add(typeof(PlacedMapBox));
                }
                else
                {
                    _selectionFilter.SelectableTypes.Remove(typeof(PlacedMapBox));
                }
            }
        }

        private bool _regionSelectionAllowed = true;

        public bool RegionSelectionAllowed
        {
            get { return _regionSelectionAllowed; }
            set
            {
                _regionSelectionAllowed = value;
                OnPropertyChanged(nameof(RegionSelectionAllowed));
                if (value)
                {
                    _selectionFilter.SelectableTypes.Add(typeof(MapRegion));
                }
                else
                {
                    _selectionFilter.SelectableTypes.Remove(typeof(MapRegion));
                }
            }
        }

        private bool _drawnShapeSelectionAllowed = true;

        public bool DrawnShapeSelectionAllowed
        {
            get { return _drawnShapeSelectionAllowed; }
            set
            {
                _drawnShapeSelectionAllowed = value;
                OnPropertyChanged(nameof(DrawnShapeSelectionAllowed));
                if (value)
                {
                    _selectionFilter.SelectableTypes.Add(typeof(IDrawnMapComponent));
                }
                else
                {
                    _selectionFilter.SelectableTypes.Remove(typeof(IDrawnMapComponent));
                }
            }
        }

        private bool _symbolSelectionAllowed = true;

        public bool SymbolSelectionAllowed
        {
            get { return _symbolSelectionAllowed; }
            set
            {
                _symbolSelectionAllowed = value;
                OnPropertyChanged(nameof(SymbolSelectionAllowed));

                if (value)
                {
                    _selectionFilter.SelectableTypes.Add(typeof(MapSymbol));
                }
                else
                {
                    _selectionFilter.SelectableTypes.Remove(typeof(MapSymbol));
                }

                StructureSelectionAllowed = value;
                OnPropertyChanged(nameof(StructureSelectionAllowed));

                VegetationSelectionAllowed = value;
                OnPropertyChanged(nameof(VegetationSelectionAllowed));

                TerrainSelectionAllowed = value;
                OnPropertyChanged(nameof(TerrainSelectionAllowed));

                MarkerSelectionAllowed = value;
                OnPropertyChanged(nameof(MarkerSelectionAllowed));

            }
        }

        private bool _structureSelectionAllowed = true;

        public bool StructureSelectionAllowed
        {
            get { return _structureSelectionAllowed; }
            set
            {
                _structureSelectionAllowed = value;
                OnPropertyChanged(nameof(StructureSelectionAllowed));
                if (value)
                {
                    _selectionFilter.AllowsStructures = true;
                }
                else
                {
                    _selectionFilter.AllowsStructures = false;
                }
            }
        }

        private bool _vegetationSelectionAllowed = true;

        public bool VegetationSelectionAllowed
        {
            get { return _vegetationSelectionAllowed; }
            set
            {
                _vegetationSelectionAllowed = value;
                OnPropertyChanged(nameof(VegetationSelectionAllowed));
                if (value)
                {
                    _selectionFilter.AllowsVegetation = true;
                }
                else
                {
                    _selectionFilter.AllowsVegetation = false;
                }
            }
        }

        private bool _terrainSelectionAllowed = true;

        public bool TerrainSelectionAllowed
        {
            get { return _terrainSelectionAllowed; }
            set
            {
                _terrainSelectionAllowed = value;
                OnPropertyChanged(nameof(TerrainSelectionAllowed));
                if (value)
                {
                    _selectionFilter.AllowsTerrain = true;
                }
                else
                {
                    _selectionFilter.AllowsTerrain = false;
                }
            }
        }

        private bool _markerSelectionAllowed = true;

        public bool MarkerSelectionAllowed
        {
            get { return _markerSelectionAllowed; }
            set
            {
                _markerSelectionAllowed = value;
                OnPropertyChanged(nameof(MarkerSelectionAllowed));
                if (value)
                {
                    _selectionFilter.AllowsMarkers = true;
                }
                else
                {
                    _selectionFilter.AllowsMarkers = false;
                }
            }
        }

        public ICommand ClearFilterCommand => new RelayCommand(() =>
        {
            // Clear the selection filter - this will allow all types of objects to be selected
            LandformSelectionAllowed = true;
            WaterSystemSelectionAllowed = true;
            LakeSelectionAllowed = true;
            RiverSelectionAllowed = true;
            PaintedWaterBodySelectionAllowed = true;
            PathSelectionAllowed = true;
            LabelSelectionAllowed = true;
            BoxSelectionAllowed = true;
            RegionSelectionAllowed = true;
            DrawnShapeSelectionAllowed = true;
            SymbolSelectionAllowed = true;
            VegetationSelectionAllowed = true;
            TerrainSelectionAllowed = true;
            StructureSelectionAllowed = true;
            MarkerSelectionAllowed = true;
        });

        public ICommand FilterAllCommand => new RelayCommand(() =>
        {
            // Set all selection filters - this will allow no types of objects to be selected
            // individual filters can then be cleared
            LandformSelectionAllowed = false;
            WaterSystemSelectionAllowed = false;
            LakeSelectionAllowed = false;
            RiverSelectionAllowed = false;
            PaintedWaterBodySelectionAllowed = false;
            PathSelectionAllowed = false;
            LabelSelectionAllowed = false;
            BoxSelectionAllowed = false;
            RegionSelectionAllowed = false;
            DrawnShapeSelectionAllowed = false;
            SymbolSelectionAllowed = false;
            VegetationSelectionAllowed = false;
            TerrainSelectionAllowed = false;
            StructureSelectionAllowed = false;
            MarkerSelectionAllowed = false;
        });

        public ICommand SetClearLandformFilterCommand => new RelayCommand(() =>
        {
            LandformSelectionAllowed = !LandformSelectionAllowed;
        });

        public ICommand SetClearWaterSystemFilterCommand => new RelayCommand(() =>
        {
            WaterSystemSelectionAllowed = !WaterSystemSelectionAllowed;
        });

        public ICommand SetClearLakeFilterCommand => new RelayCommand(() =>
        {
            LakeSelectionAllowed = !LakeSelectionAllowed;
        });

        public ICommand SetClearRiverFilterCommand => new RelayCommand(() =>
        {
            RiverSelectionAllowed = !RiverSelectionAllowed;
        });

        public ICommand SetClearPaintedWaterBodyFilterCommand => new RelayCommand(() =>
        {
            PaintedWaterBodySelectionAllowed = !PaintedWaterBodySelectionAllowed;
        });

        public ICommand SetClearPathFilterCommand => new RelayCommand(() =>
        {
            PathSelectionAllowed = !PathSelectionAllowed;
        });

        public ICommand SetClearLabelFilterCommand => new RelayCommand(() =>
        {
            LabelSelectionAllowed = !LabelSelectionAllowed;
        });

        public ICommand SetClearBoxFilterCommand => new RelayCommand(() =>
        {
            BoxSelectionAllowed = !BoxSelectionAllowed;
        });

        public ICommand SetClearRegionFilterCommand => new RelayCommand(() =>
        {
            RegionSelectionAllowed = !RegionSelectionAllowed;
        });

        public ICommand SetClearDrawnShapeFilterCommand => new RelayCommand(() =>
        {
            DrawnShapeSelectionAllowed = !DrawnShapeSelectionAllowed;
        });

        public ICommand SetClearAllSymbolsFilterCommand => new RelayCommand(() =>
        {
            SymbolSelectionAllowed = !SymbolSelectionAllowed;
        });

        public ICommand SetClearStructuresFilterCommand => new RelayCommand(() =>
        {
            StructureSelectionAllowed = !StructureSelectionAllowed;
        });

        public ICommand SetClearVegetationFilterCommand => new RelayCommand(() =>
        {
            VegetationSelectionAllowed = !VegetationSelectionAllowed;
        });

        public ICommand SetClearTerrainFilterCommand => new RelayCommand(() =>
        {
            TerrainSelectionAllowed = !TerrainSelectionAllowed;
        });

        public ICommand SetClearMarkerFilterCommand => new RelayCommand(() =>
        {
            MarkerSelectionAllowed = !MarkerSelectionAllowed;
        });


        public ISelectable? SelectAt(RealmStudioMap map, SKPoint worldPos, float tolerance, bool addToSelection = false)
        {
            List<ISelectable> candidates = [.. GetSelectionCandidatesAtPoint(map, worldPos, tolerance)];

            if (candidates.Count == 0)
            {
                if (!addToSelection)
                {
                    ClearSelection();
                }

                return null;
            }

            //
            // Single candidate:
            // Toggle selection like Visio.
            //
            if (candidates.Count == 1)
            {
                ISelectable selected = candidates[0];

                ResetSelectionCycle();

                if (SelectedObjects.Contains(selected))
                {
                    _selectedObjects.Remove(selected);
                    selected.IsSelected = false;

                    SelectionChanged?.Invoke(this, EventArgs.Empty);

                    return null;
                }

                if (addToSelection)
                {
                    AddToSelection(selected);
                }
                else
                {
                    SelectSingle(selected);
                }

                if (PrimarySelection != null)
                {
                    PropertiesPopupLeft = PrimarySelection.Bounds.Left;
                    PropertiesPopupTop = PrimarySelection.Bounds.Top;
                }

                OnPropertyChanged(nameof(SelectedObjects));
                OnPropertyChanged(nameof(PropertiesPopupVisible));
                OnPropertyChanged(nameof(PropertiesPopupLeft));
                OnPropertyChanged(nameof(PropertiesPopupTop));

                return selected;
            }

            //
            // Multiple candidates:
            // Cycle through them.
            //
            bool sameClick = SKPoint.Distance(worldPos, _lastClickPoint)  <= SelectionCycleDistance;

            bool sameCandidates = CandidateListsMatch(candidates, _lastCandidates);

            if (sameClick && sameCandidates)
            {
                _candidateIndex++;

                if (_candidateIndex >= candidates.Count)
                {
                    _candidateIndex = 0;
                }
            }
            else
            {
                _candidateIndex = 0;
                _lastCandidates = candidates;
                _lastClickPoint = worldPos;
            }

            ISelectable selectedCandidate = candidates[_candidateIndex];

            if (addToSelection)
            {
                AddToSelection(selectedCandidate);
            }
            else
            {
                SelectSingle(selectedCandidate);
            }

            if (PrimarySelection != null)
            {
                PropertiesPopupLeft = PrimarySelection.Bounds.Left;
                PropertiesPopupTop = PrimarySelection.Bounds.Top;
            }

            OnPropertyChanged(nameof(SelectedObjects));
            OnPropertyChanged(nameof(PropertiesPopupVisible));
            OnPropertyChanged(nameof(PropertiesPopupLeft));
            OnPropertyChanged(nameof(PropertiesPopupTop));

            return selectedCandidate;
        }

        private void AddToSelection(ISelectable selected)
        {
            SelectedObjects.Add(selected);
            selected.IsSelected = true;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SelectSingle(ISelectable selected)
        {
            ClearSelectionState();

            _selectedObjects.Clear();

            _selectedObjects.Add(selected);

            selected.IsSelected = true;

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ClearSelectionState(MapScene? scene = null)
        {
            for (int i = 0; i < SelectedObjects.Count; i++)
            {
                if (SelectedObjects[i] is MapComponent2D selected)
                {
                    selected.IsSelected = false;
                }
                else if (SelectedObjects[i] is WaterSystem ws)
                {
                    ws.IsSelected = false;
                }                
            }

            SelectedObjects.Clear();

            OnPropertyChanged(nameof(SelectedObjects));
            OnPropertyChanged(nameof(PropertiesPopupVisible));
            OnPropertyChanged(nameof(PropertiesPopupLeft));
            OnPropertyChanged(nameof(PropertiesPopupTop));

            if (scene != null)
            {
                scene.TransformWidget.Target = null;
            }
        }

        public void ClearSelection(MapScene? scene = null)
        {
            ClearSelectionState(scene);
            ResetSelectionCycle();
        }

        private void ResetSelectionCycle()
        {
            _candidateIndex = 0;
        }

        private static bool CandidateListsMatch(List<ISelectable> candidates, List<ISelectable> lastCandidates)
        {
            if (candidates.Count != lastCandidates.Count)
            {
                return false;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                if (((ISelectable)candidates[i]).Id != ((ISelectable)lastCandidates[i]).Id)
                {
                    return false;
                }
            }

            return true;
        }

        private List<ISelectable> GetSelectionCandidatesAtPoint(RealmStudioMap map, SKPoint worldPos, float tolerance)
        {
            var hits = new List<ISelectable>();

            for (int layerIndex = map.MapLayers.Count - 1; layerIndex >= 0; layerIndex--)
            {
                MapLayer layer = map.MapLayers[layerIndex];

                if (!layer.ShowLayer)
                {
                    continue;
                }

                if (layer.MapLayerOrder == MapBuilder.WATERLAYER)
                {
                    // water layer requires special handling because
                    // lakes, rivers, and painted water bodies are
                    // aggregated into water systems
                    foreach (var waterSystem in map.WaterSystems)
                    {
                        if (SelectionFilter.Allows(waterSystem) && waterSystem.HitTest(worldPos))
                        {
                            hits.Add(waterSystem);
                        }

                        foreach (var waterBody in waterSystem.WaterBodies)
                        {
                            if (SelectionFilter.Allows(waterBody) && waterBody.HitTest(worldPos))
                            {
                                hits.Add(waterBody);
                            }
                        }
                    }
                }
                else
                {
                    for (int i = layer.Shapes.Count - 1; i >= 0; i--)
                    {
                        var shape = layer.Shapes[i];

                        if (shape is MapSymbol ms && SelectionFilter.Allows(ms))
                        {
                            if (ms.HitTest(worldPos))
                            {
                                hits.Add(ms);
                            }
                        }
                        else if (shape is MapLabel ml && SelectionFilter.Allows(ml))
                        {
                            if (ml.HitTest(worldPos))
                            {
                                hits.Add(ml);
                            }
                        }
                        else if (shape is PlacedMapBox pmb && SelectionFilter.Allows(pmb))
                        {
                            if (pmb.HitTest(worldPos))
                            {
                                hits.Add(pmb);
                            }
                        }
                        else if (shape is MapScale scale && SelectionFilter.Allows(scale))
                        {
                            if (scale.HitTest(worldPos))
                            {
                                hits.Add(scale);
                            }
                        }
                        else if (shape is MapPath path && SelectionFilter.Allows(path))
                        {
                            if (path.HitTest(worldPos))
                            {
                                hits.Add(path);
                            }
                        }
                        else if (shape.HitTest(worldPos) && SelectionFilter.Allows(shape))
                        {
                            hits.Add(shape);
                        }
                    }
                }

            }

            return hits;
        }

        internal void SelectForLayout(EditorController editor, SKPoint worldPoint, int tolerance)
        {
            if (editor == null || editor.LayoutTool == null)
            {
                return;
            }

            List<ISelectable> hits = GetSelectionCandidatesAtPoint(editor.Scene!.Map, worldPoint, tolerance);

            for (int i = 0; i < hits.Count; i++)
            {
                ISelectable hit = hits[i];

                if (hit != null)
                {
                    if (hit is MapPath mp)
                    {
                        SelectSingle(mp);
                        editor.LayoutTool.LayoutPath = mp.HitPath;
                        break;
                    }

                    if (hit is River river)
                    {
                        SelectSingle(river);
                        editor.LayoutTool.LayoutPath = Utilities.BuildPath(river.ControlPoints);
                        break;
                    }

                }
            }
        }

        internal void SelectObjectsInArea(RealmStudioMap map, SKRect selectedRealmArea)
        {
            List<ISelectable> candidates = [.. GetSelectionCandidatesInArea(map, selectedRealmArea)];

            ClearSelection();

            foreach (var selected in candidates)
            {
                SelectedObjects.Add(selected);
                selected.IsSelected = true;
            }

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public IEnumerable<ISelectable> GetSelectionCandidatesInArea(RealmStudioMap map, SKRect selectionRect)
        {
            var hits = new List<ISelectable>();

            for (int layerIndex = map.MapLayers.Count - 1; layerIndex >= 0; layerIndex--)
            {
                MapLayer layer = map.MapLayers[layerIndex];

                if (!layer.ShowLayer)
                {
                    continue;
                }

                if (layer.MapLayerOrder == MapBuilder.WATERLAYER)
                {
                    // water layer requires special handling because
                    // lakes, rivers, and painted water bodies are
                    // aggregated into water systems
                    foreach (var waterSystem in map.WaterSystems)
                    {
                        if (SelectionFilter.Allows(waterSystem) && waterSystem.Bounds.IntersectsWith(selectionRect))
                        {
                            hits.Add(waterSystem);
                        }

                        foreach (var waterBody in waterSystem.WaterBodies)
                        {
                            if (SelectionFilter.Allows(waterBody) && waterBody.Bounds.IntersectsWith(selectionRect))
                            {
                                hits.Add(waterBody);
                            }
                        }
                    }
                }
                else
                {
                    for (int i = layer.Shapes.Count - 1; i >= 0; i--)
                    {
                        var shape = layer.Shapes[i];

                        if (shape is MapSymbol ms && SelectionFilter.Allows(ms))
                        {
                            if (ms.Bounds.IntersectsWith(selectionRect))
                            {
                                hits.Add(ms);
                            }
                        }
                        else if (shape is MapLabel ml && SelectionFilter.Allows(ml))
                        {
                            if (ml.Bounds.IntersectsWith(selectionRect))
                            {
                                hits.Add(ml);
                            }
                        }
                        else if (shape is MapScale scale && SelectionFilter.Allows(scale))
                        {
                            if (scale.Bounds.IntersectsWith(selectionRect))
                            {
                                hits.Add(scale);
                            }
                        }
                        else if (shape.Bounds.IntersectsWith(selectionRect) && SelectionFilter.Allows(shape))
                        {
                            hits.Add(shape);
                        }
                    }
                }

            }

            return hits;
        }

        internal void SelectObjectsInPath(RealmStudioMap map, SKPath lassoPath)
        {
            List<ISelectable> candidates = [.. GetSelectionCandidatesInPath(map, lassoPath)];

            ClearSelection();

            foreach (var selected in candidates)
            {
                SelectedObjects.Add(selected);
                selected.IsSelected = true;
            }

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public IEnumerable<ISelectable> GetSelectionCandidatesInPath(RealmStudioMap map, SKPath lassoPath)
        {
            var hits = new List<ISelectable>();

            for (int layerIndex = map.MapLayers.Count - 1; layerIndex >= 0; layerIndex--)
            {
                MapLayer layer = map.MapLayers[layerIndex];

                if (!layer.ShowLayer)
                {
                    continue;
                }

                if (layer.MapLayerOrder == MapBuilder.WATERLAYER)
                {
                    // water layer requires special handling because
                    // lakes, rivers, and painted water bodies are
                    // aggregated into water systems
                    foreach (var waterSystem in map.WaterSystems)
                    {
                        using var inter1 = waterSystem.MergedGeometry.Op(lassoPath, SKPathOp.Intersect);

                        if (inter1 != null && inter1.IsEmpty && SelectionFilter.Allows(waterSystem))
                        {
                            hits.Add(waterSystem);
                        }

                        foreach (var waterBody in waterSystem.WaterBodies)
                        {
                            using var inter2 = waterBody.HitPath.Op(lassoPath, SKPathOp.Intersect);

                            if (inter2 != null && inter2.IsEmpty && SelectionFilter.Allows(waterBody))
                            {
                                hits.Add(waterBody);
                            }
                        }
                    }
                }
                else
                {
                    for (int i = layer.Shapes.Count - 1; i >= 0; i--)
                    {
                        var shape = layer.Shapes[i];

                        if (shape is MapSymbol ms && SelectionFilter.Allows(ms))
                        {
                            if (lassoPath.Contains(ms.Bounds.MidX, ms.Bounds.MidY))
                            {
                                hits.Add(ms);
                            }
                        }
                        else if (shape is MapLabel ml && SelectionFilter.Allows(ml))
                        {
                            if (lassoPath.Contains(ml.Bounds.MidX, ml.Bounds.MidY))
                            {
                                hits.Add(ml);
                            }
                        }
                        else if (shape is MapScale scale && SelectionFilter.Allows(scale))
                        {
                            if (lassoPath.Contains(scale.Bounds.MidX, scale.Bounds.MidY))
                            {
                                hits.Add(scale);
                            }
                        }
                        else
                        {
                            if (lassoPath.Contains(shape.Bounds.MidX, shape.Bounds.MidY) && SelectionFilter.Allows(shape))
                            {
                                hits.Add(shape);
                            }
                        }
                    }
                }

            }

            return hits;
        }

    }

    // -------------------------------------------------
    // Selection Filter Class
    // -------------------------------------------------

    public sealed class SelectionFilter()
    {
        public HashSet<Type> SelectableTypes { get; } = [];

        public bool AllowsStructures { get; set; } = true;
        public bool AllowsVegetation { get; set; } = true;
        public bool AllowsTerrain { get; set; } = true;
        public bool AllowsMarkers { get; set; } = true;

        public void AddTypeToFilter(Type type)
        {
            SelectableTypes.Add(type);
        }

        public void RemoveTypeFromFilter(Type type)
        {
            SelectableTypes.Remove(type);
        }

        public void ClearFilter()
        {
            SelectableTypes.Clear();
        }

        public bool Allows(ISelectable shape)
        {
            bool allowed = SelectableTypes.Any(type =>
                type.IsAssignableFrom(shape.GetType()));

            if (!allowed)
            {
                return false;
            }

            if (shape is not MapSymbol symbol)
            {
                return true;
            }

            return symbol.SymbolDefinition.SymbolType switch
            {
                MapSymbolType.Structure => AllowsStructures,
                MapSymbolType.Vegetation => AllowsVegetation,
                MapSymbolType.Terrain => AllowsTerrain,
                MapSymbolType.Marker => AllowsMarkers,
                _ => true
            };
        }
    }
}
