using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using SkiaSharp;
using System.Windows.Input;
using Clipboard = RealmStudioX.Core.Clipboard;

namespace RealmStudioX.WPF.Editor.Services
{
    public class SelectionService : ViewModelBase
    {
        private const float SelectionCycleDistance = 8f;

        public event EventHandler? SelectionChanged;

        private readonly SelectionFilter _selectionFilter = new();

        public SelectionFilter SelectionFilter => _selectionFilter;

        private readonly List<ShapeReference> _selectedObjects = [];

        private Clipboard? _clipboard;

        public Clipboard? Clipboard => _clipboard;

        public bool CanPaste => _clipboard != null && _clipboard.Items.Count > 0;

        private SKPoint _lastClickPoint;

        private List<ShapeReference> _lastCandidates = [];

        private int _candidateIndex;

        public List<ShapeReference> SelectedObjects
        {
            get { return _selectedObjects; }
        }

        public ShapeReference? PrimarySelection
        {
            get { return _selectedObjects.Count > 0 ? _selectedObjects[0] : null; }
        }

        public string PrimarySelectionTypeName
        {
            get
            {
                return PrimarySelection != null && PrimarySelection.ReferencedShape != null ? PrimarySelection.ReferencedShape.GetType().Name : string.Empty;
            }
        }
        private SKRect _selectedArea = SKRect.Empty;

        public SKRect SelectedArea
        {
            get { return _selectedArea; }
            set { _selectedArea = value; }
        }

        public bool HasSelection => _selectedObjects.Count > 0;

        public bool HasSingleSelection => _selectedObjects.Count == 1;

        public bool HasMultipleSelection => _selectedObjects.Count > 1;

        public int SelectionCount => _selectedObjects.Count;

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

        public bool PropertiesPopupEnabled =>
            PrimarySelection != null &&
            PrimarySelection.ReferencedShape != null &&
            (PrimarySelection.ReferencedShape is Landform
             || PrimarySelection.ReferencedShape is WaterSystem
             || PrimarySelection.ReferencedShape is Lake
             || PrimarySelection.ReferencedShape is River
             || PrimarySelection.ReferencedShape is PaintedWaterBody
             || PrimarySelection.ReferencedShape is MapPath
             || PrimarySelection.ReferencedShape is MapRegion
             || PrimarySelection.ReferencedShape is MapSymbol);


        private bool _landformSelectionAllowed = true;

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


        public ShapeReference? SelectAt(RealmStudioMap map, SKPoint worldPos, float tolerance, bool addToSelection = false)
        {
            List<ShapeReference> candidates = GetSelectionCandidatesAtPoint(map, worldPos, tolerance).ToList();

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
                ShapeReference selected = candidates[0];

                if (selected.ReferencedShape == null)
                {
                    return null;
                }

                ResetSelectionCycle();

                if (SelectedObjects.Contains(selected))
                {
                    _selectedObjects.Remove(selected);
                    selected.ReferencedShape.IsSelected = false;

                    SelectionChanged?.Invoke(this, EventArgs.Empty);

                    OnPropertyChanged(nameof(SelectedObjects));
                    OnPropertyChanged(nameof(PropertiesPopupEnabled));

                    OnPropertyChanged(nameof(SelectionCount));
                    OnPropertyChanged(nameof(PrimarySelectionTypeName));

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

                OnPropertyChanged(nameof(SelectedObjects));
                OnPropertyChanged(nameof(PropertiesPopupEnabled));

                OnPropertyChanged(nameof(SelectionCount));
                OnPropertyChanged(nameof(PrimarySelectionTypeName));

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

            ShapeReference selectedCandidate = candidates[_candidateIndex];

            if (addToSelection)
            {
                AddToSelection(selectedCandidate);
            }
            else
            {
                SelectSingle(selectedCandidate);
            }

            OnPropertyChanged(nameof(SelectedObjects));
            OnPropertyChanged(nameof(PropertiesPopupEnabled));

            OnPropertyChanged(nameof(SelectionCount));
            OnPropertyChanged(nameof(PrimarySelectionTypeName));

            return selectedCandidate;
        }

        private void AddToSelection(ShapeReference selected)
        {
            if (selected.ReferencedShape == null)
            {
                return;
            }

            SelectedObjects.Add(selected);
            selected.ReferencedShape.IsSelected = true;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SelectSingle(ShapeReference selected)
        {
            if (selected.ReferencedShape == null)
            {
                return;
            }

            SelectedObjects.Add(selected);
            
            ClearSelectionState();

            _selectedObjects.Clear();

            _selectedObjects.Add(selected);

            selected.ReferencedShape.IsSelected = true;

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ClearSelectionState(MapScene? scene = null)
        {
            for (int i = 0; i < SelectedObjects.Count; i++)
            {
                if (SelectedObjects[i].ReferencedShape is MapComponent2D selected)
                {
                    selected.IsSelected = false;
                }
                else if (SelectedObjects[i].ReferencedShape is WaterSystem ws)
                {
                    ws.IsSelected = false;
                }                
            }

            SelectedObjects.Clear();

            OnPropertyChanged(nameof(SelectedObjects));
            OnPropertyChanged(nameof(PropertiesPopupEnabled));

            OnPropertyChanged(nameof(SelectionCount));
            OnPropertyChanged(nameof(PrimarySelectionTypeName));

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

        private static bool CandidateListsMatch(List<ShapeReference> candidates, List<ShapeReference> lastCandidates)
        {
            if (candidates.Count != lastCandidates.Count)
            {
                return false;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].ReferencedShape == null || lastCandidates[i].ReferencedShape == null)
                {
                    return false;
                }

                if (candidates[i].ReferencedShape!.Id != lastCandidates[i].ReferencedShape!.Id)
                {
                    return false;
                }
            }

            return true;
        }

        private List<ShapeReference> GetSelectionCandidatesAtPoint(RealmStudioMap map, SKPoint worldPos, float tolerance)
        {
            var hits = new List<ShapeReference>();

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
                            ShapeReference wsRef = new ShapeReference()
                            {
                                ReferencedShape = waterSystem,
                                ShapeLayer = layer
                            };

                            hits.Add(wsRef);
                        }

                        foreach (var waterBody in waterSystem.WaterBodies)
                        {
                            if (SelectionFilter.Allows(waterBody) && waterBody.HitTest(worldPos))
                            {
                                ShapeReference wbRef = new ShapeReference()
                                {
                                    ReferencedShape = waterBody,
                                    ShapeLayer = layer
                                };

                                hits.Add(wbRef);
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
                                ShapeReference shapeRef = new ShapeReference()
                                {
                                    ReferencedShape = ms,
                                    ShapeLayer = layer
                                };

                                hits.Add(shapeRef);
                            }
                        }
                        else if (shape is MapLabel ml && SelectionFilter.Allows(ml))
                        {
                            if (ml.HitTest(worldPos))
                            {
                                ShapeReference shapeRef = new ShapeReference()
                                {
                                    ReferencedShape = ml,
                                    ShapeLayer = layer
                                };

                                hits.Add(shapeRef);
                            }
                        }
                        else if (shape is PlacedMapBox pmb && SelectionFilter.Allows(pmb))
                        {
                            if (pmb.HitTest(worldPos))
                            {
                                ShapeReference shapeRef = new ShapeReference()
                                {
                                    ReferencedShape = pmb,
                                    ShapeLayer = layer
                                };

                                hits.Add(shapeRef);
                            }
                        }
                        else if (shape is MapScale scale && SelectionFilter.Allows(scale))
                        {
                            if (scale.HitTest(worldPos))
                            {
                                ShapeReference shapeRef = new ShapeReference()
                                {
                                    ReferencedShape = scale,
                                    ShapeLayer = layer
                                };

                                hits.Add(shapeRef);
                            }
                        }
                        else if (shape is MapPath path && SelectionFilter.Allows(path))
                        {
                            if (path.HitTest(worldPos))
                            {
                                ShapeReference shapeRef = new ShapeReference()
                                {
                                    ReferencedShape = path,
                                    ShapeLayer = layer
                                };

                                hits.Add(shapeRef);
                            }
                        }
                        else if (shape.HitTest(worldPos) && SelectionFilter.Allows(shape))
                        {
                            ShapeReference shapeRef = new ShapeReference()
                            {
                                ReferencedShape = shape,
                                ShapeLayer = layer
                            };

                            hits.Add(shapeRef);
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

            List<ShapeReference> hits = GetSelectionCandidatesAtPoint(editor.Scene!.Map, worldPoint, tolerance);

            for (int i = 0; i < hits.Count; i++)
            {
                ShapeReference hit = hits[i];

                if (hit != null)
                {
                    if (hit.ReferencedShape is MapPath mp)
                    {
                        SelectSingle(hit);
                        editor.LayoutTool.LayoutPath = mp.HitPath;
                        break;
                    }

                    if (hit.ReferencedShape is River river)
                    {
                        SelectSingle(hit);
                        editor.LayoutTool.LayoutPath = Utilities.BuildPath(river.ControlPoints);
                        break;
                    }

                }
            }
        }

        internal void SelectObjectsInArea(RealmStudioMap map, SKRect selectedRealmArea)
        {
            List<ShapeReference> candidates = [.. GetSelectionCandidatesInArea(map, selectedRealmArea)];

            ClearSelection();

            foreach (var selected in candidates)
            {
                SelectedObjects.Add(selected);

                if (selected.ReferencedShape != null)
                {
                    selected.ReferencedShape.IsSelected = true;
                }
            }

            OnPropertyChanged(nameof(SelectedObjects));
            OnPropertyChanged(nameof(PropertiesPopupEnabled));

            OnPropertyChanged(nameof(SelectionCount));
            OnPropertyChanged(nameof(PrimarySelectionTypeName));

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public IEnumerable<ShapeReference> GetSelectionCandidatesInArea(RealmStudioMap map, SKRect selectionRect)
        {
            var hits = new List<ShapeReference>();

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
                            ShapeReference shapeRef = new ShapeReference()
                            {
                                ReferencedShape = waterSystem,
                                ShapeLayer = layer
                            };

                            hits.Add(shapeRef);
                        }

                        foreach (var waterBody in waterSystem.WaterBodies)
                        {
                            if (SelectionFilter.Allows(waterBody) && waterBody.Bounds.IntersectsWith(selectionRect))
                            {
                                ShapeReference shapeRef = new ShapeReference()
                                {
                                    ReferencedShape = waterBody,
                                    ShapeLayer = layer
                                };

                                hits.Add(shapeRef);
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
                                ShapeReference shapeRef = new ShapeReference()
                                {
                                    ReferencedShape = ms,
                                    ShapeLayer = layer
                                };

                                hits.Add(shapeRef);
                            }
                        }
                        else if (shape is MapLabel ml && SelectionFilter.Allows(ml))
                        {
                            if (ml.Bounds.IntersectsWith(selectionRect))
                            {
                                ShapeReference shapeRef = new ShapeReference()
                                {
                                    ReferencedShape = ml,
                                    ShapeLayer = layer
                                };

                                hits.Add(shapeRef);
                            }
                        }
                        else if (shape is MapScale scale && SelectionFilter.Allows(scale))
                        {
                            if (scale.Bounds.IntersectsWith(selectionRect))
                            {
                                ShapeReference shapeRef = new ShapeReference()
                                {
                                    ReferencedShape = scale,
                                    ShapeLayer = layer
                                };

                                hits.Add(shapeRef);
                            }
                        }
                        else if (shape.Bounds.IntersectsWith(selectionRect) && SelectionFilter.Allows(shape))
                        {
                            ShapeReference shapeRef = new ShapeReference()
                            {
                                ReferencedShape = shape,
                                ShapeLayer = layer
                            };

                            hits.Add(shapeRef);
                        }
                    }
                }

            }

            return hits;
        }

        internal void SelectObjectsInPath(RealmStudioMap map, SKPath lassoPath)
        {
            List<ShapeReference> candidates = [.. GetSelectionCandidatesInPath(map, lassoPath)];

            ClearSelection();

            foreach (var selected in candidates)
            {
                SelectedObjects.Add(selected);

                if (selected.ReferencedShape != null)
                {
                    selected.ReferencedShape.IsSelected = true;
                }
            }

            OnPropertyChanged(nameof(SelectedObjects));
            OnPropertyChanged(nameof(PropertiesPopupEnabled));

            OnPropertyChanged(nameof(SelectionCount));
            OnPropertyChanged(nameof(PrimarySelectionTypeName));

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public IEnumerable<ShapeReference> GetSelectionCandidatesInPath(RealmStudioMap map, SKPath lassoPath)
        {
            var hits = new List<ShapeReference>();

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
                            ShapeReference shapeRef = new ShapeReference()
                            {
                                ReferencedShape = waterSystem,
                                ShapeLayer = layer
                            };
                            hits.Add(shapeRef);
                        }

                        foreach (var waterBody in waterSystem.WaterBodies)
                        {
                            using var inter2 = waterBody.HitPath.Op(lassoPath, SKPathOp.Intersect);

                            if (inter2 != null && inter2.IsEmpty && SelectionFilter.Allows(waterBody))
                            {
                                ShapeReference shapeRef = new ShapeReference()
                                {
                                    ReferencedShape = waterBody,
                                    ShapeLayer = layer
                                };
                                hits.Add(shapeRef);
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
                                ShapeReference shapeRef = new ShapeReference()
                                {
                                    ReferencedShape = ms,
                                    ShapeLayer = layer
                                };
                                hits.Add(shapeRef);
                            }
                        }
                        else if (shape is MapLabel ml && SelectionFilter.Allows(ml))
                        {
                            if (lassoPath.Contains(ml.Bounds.MidX, ml.Bounds.MidY))
                            {
                                ShapeReference shapeRef = new ShapeReference()
                                {
                                    ReferencedShape = ml,
                                    ShapeLayer = layer
                                };
                                hits.Add(shapeRef);
                            }
                        }
                        else if (shape is MapScale scale && SelectionFilter.Allows(scale))
                        {
                            if (lassoPath.Contains(scale.Bounds.MidX, scale.Bounds.MidY))
                            {
                                ShapeReference shapeRef = new ShapeReference()
                                {
                                    ReferencedShape = scale,
                                    ShapeLayer = layer
                                };
                                hits.Add(shapeRef);
                            }
                        }
                        else
                        {
                            if (lassoPath.Contains(shape.Bounds.MidX, shape.Bounds.MidY) && SelectionFilter.Allows(shape))
                            {
                                ShapeReference shapeRef = new ShapeReference()
                                {
                                    ReferencedShape = shape,
                                    ShapeLayer = layer
                                };
                                hits.Add(shapeRef);
                            }
                        }
                    }
                }

            }

            return hits;
        }

        private void DeleteSelectedObjects()
        {
            List<MapLayer> layersToUpdate = [];

            foreach (ShapeReference sr in _selectedObjects)
            {
                if (sr.ReferencedShape != null && sr.ShapeLayer != null)
                {
                    ((MapComponent2D)sr.ReferencedShape).IsSelected = false;

                    sr.ShapeLayer.Remove((MapComponent2D)sr.ReferencedShape);
                    
                    if (!layersToUpdate.Contains(sr.ShapeLayer))
                    {
                        layersToUpdate.Add(sr.ShapeLayer);
                    }
                }
            }

            foreach (MapLayer layer in layersToUpdate)
            {
                layer.RebuildIndexes();
            }
        }

        public void CopySelectedObjects()
        {
            if (_selectedObjects.Count == 0)
            {
                _clipboard = null;
                return;
            }

            SKRect bounds = GetSelectionBounds();

            _clipboard = new Clipboard
            {
                Anchor = new SKPoint(bounds.MidX, bounds.MidY)
            };

            foreach (ShapeReference sr in _selectedObjects)
            {
                if (sr.ReferencedShape is MapSymbol || sr.ReferencedShape is MapPath || sr.ReferencedShape is IDrawnMapComponent)
                {
                    SKPoint location = SKPoint.Empty;
                    
                    if (sr.ReferencedShape is MapPath mp)
                    {
                        location = new SKPoint(mp.Bounds.MidX, mp.Bounds.MidY);
                    }
                    else if (sr.ReferencedShape is MapSymbol ms)
                    {
                        location = ms.Location;  // map symbol location is the center of the symbol bounds
                    }
                    else if (sr.ReferencedShape is IDrawnMapComponent && sr.ReferencedShape is IRectangularShape irs)
                    {
                        SKRect b = new(irs.TopLeft.X, irs.TopLeft.Y, irs.BottomRight.X, irs.BottomRight.Y);
                        location = new SKPoint(b.MidX, b.MidY);
                    }
                    else if (sr.ReferencedShape is IDrawnMapComponent && sr.ReferencedShape is ICenterRadiusShape icrs)
                    {
                        location = icrs.Center;
                    }
                    else if (sr.ReferencedShape is IDrawnMapComponent && sr.ReferencedShape is IPointListShape ipls)
                    {
                        location = new SKPoint(ipls.Bounds.MidX, ipls.Bounds.MidY);
                    }

                    _clipboard.Items.Add(new ClipboardItem
                    {
                        MapLayerId = sr.ShapeLayer!.MapLayerId,
                        Offset = new SKPoint(_clipboard.Anchor.X - location.X, _clipboard.Anchor.Y - location.Y),
                        ObjectType = sr.ReferencedShape!.GetType(),
                        SerializedObject =
                            MapFileMethods.SerializeObject((dynamic)sr.ReferencedShape)
                    });
                }
            }
        }

        public void SelectObjects(List<ShapeReference> newSelection)
        {
            ClearSelection();

            foreach (ShapeReference sr in newSelection)
            {
                AddToSelection(sr);
            }
        }

        public SKRect GetSelectionBounds()
        {
            if (_selectedObjects.Count == 0)
                return SKRect.Empty;

            bool first = true;
            SKRect bounds = SKRect.Empty;

            foreach (ShapeReference sr in _selectedObjects)
            {
                if (sr.ReferencedShape is not IShape2D shape)
                    continue;

                if (first)
                {
                    bounds = ((MapComponent2D)shape).Bounds;
                    first = false;
                }
                else
                {
                    bounds = SKRect.Union(bounds, ((MapComponent2D)shape).Bounds);
                }
            }

            return bounds;
        }
    }

    // -------------------------------------------------
    // Selection Filter Class
    // -------------------------------------------------

    public sealed class SelectionFilter
    {
        public HashSet<Type> SelectableTypes { get; } = new HashSet<Type>();

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
