using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using SkiaSharp;

namespace RealmStudioX.WPF.Editor.Services
{
    public class SelectionService
    {
        private const float SelectionCycleDistance = 8f;

        public event EventHandler? SelectionChanged;

        private SelectionFilterState _selectionFilter = new([]);

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

        public ISelectable? SelectAt(RealmStudioMap map, SKPoint worldPos, float tolerance, bool addToSelection = false)
        {
            List<ISelectable> candidates = [.. GetSelectionCandidates(map, worldPos, tolerance)];

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

        private void ClearSelectionState()
        {
            foreach (ISelectable selected in SelectedObjects)
            {
                selected.IsSelected = false;
            }
        }

        public void ClearSelection()
        {
            ClearSelectionState();
            SelectedObjects.Clear();
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

        public static IEnumerable<ISelectable> GetSelectionCandidates(RealmStudioMap map, SKPoint worldPos, float tolerance)
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
                        if (waterSystem.HitTest(worldPos))
                        {
                            hits.Add(waterSystem);
                        }

                        foreach (var waterBody in waterSystem.WaterBodies)
                        {
                            if (waterBody.HitTest(worldPos))
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

                        if (shape is MapSymbol ms)
                        {
                            if (ms.HitTest(worldPos))
                            {
                                hits.Add(ms);
                            }
                        }
                        else if (shape is MapLabel ml)
                        {
                            if (ml.HitTest(worldPos))
                            {
                                hits.Add(ml);
                            }
                        }
                        else if (shape is MapScale scale)
                        {
                            if (scale.HitTest(worldPos))
                            {
                                hits.Add(scale);
                            }
                        }
                        else if (shape.HitTest(worldPos))
                        {
                            hits.Add(shape);
                        }
                    }
                }

            }

            return hits;
        }

        public static IEnumerable<ISelectable> GetSelectionCandidates(RealmStudioMap map, SKRect selectionRect)
        {
            // TODO
            return [];
        }

        public static IEnumerable<ISelectable> GetSelectionCandidates(RealmStudioMap map, SKPath lassoPath)
        {
            // TODO
            return [];
        }
    }
}
