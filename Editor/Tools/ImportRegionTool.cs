using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.ViewModels.Panels;
using SkiaSharp;

namespace RealmStudioX.WPF.Editor.Tools
{
    public sealed class ImportRegionTool(EditorController editor, MainWindowViewModel mainViewModel, ImportPanelViewModel importViewModel) : IToolEditor, IDisposable
    {
        private readonly EditorController _editor = editor;
        private readonly MainWindowViewModel _mainViewModel = mainViewModel;
        private readonly ImportPanelViewModel _importViewModel = importViewModel;

        private SKPoint _lastMouseWorld;

        private List<SKPoint> _importRegionPoints = [];

        private ImportRegion? _activeImportRegion;


        private bool disposedValue;

        public void Activate()
        {
        }

        public void Cancel()
        {

        }

        public void Deactivate()
        {

        }

        public void OnMouseDown(PointerState state)
        {
            _lastMouseWorld = state.WorldPoint;

            bool ctrl = (state.Modifiers & InputModifiers.Control) == InputModifiers.Control;
            bool shift = (state.Modifiers & InputModifiers.Shift) == InputModifiers.Shift;

            if (state.Button == EditorMouseButton.Left)
            {
                _importRegionPoints.Add(state.WorldPoint);
            }

            if (state.Button == EditorMouseButton.Right)
            {
                // commit the import region
                SKPath regionPath = Utilities.BuildClosedPath(_importRegionPoints);
                float area = Utilities.CalculatePolygonArea(_importRegionPoints);
                MapLayer workLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.WORKLAYER);

                _activeImportRegion = new(regionPath)
                {
                    Area = area,
                    Bounds = regionPath.Bounds,
                    Confidence = 0.0,
                    Index = workLayer.Shapes.Count + 1,
                    IsSelected = false,
                    Source = ImportRegionSource.User,
                    State = ImportRegionState.Preview,
                };

                ImportRegion clone = ImportRegion.Clone(_activeImportRegion);

                workLayer.Add(clone);
                _importViewModel.ImportRegions.Add(clone);

                _activeImportRegion = null;
                _importRegionPoints.Clear();

            }
        }

        public void OnMouseMove(PointerState state)
        {
            bool ctrl = (state.Modifiers & InputModifiers.Control) == InputModifiers.Control;
            bool shift = (state.Modifiers & InputModifiers.Shift) == InputModifiers.Shift;

            if (state.Button == EditorMouseButton.Left)
            {
                _importRegionPoints.Add(state.WorldPoint);                
            }

            _lastMouseWorld = state.WorldPoint;
        }

        public void OnMouseUp(PointerState state)
        {
            _lastMouseWorld = state.WorldPoint;
        }

        public void OnMouseDoubleClick(PointerState state)
        {
            // no action
        }

        public void OnMouseWheel(PointerState state)
        {
            // no action
        }


        public void RenderOverlay(SKCanvas canvas, SKPoint world)
        {
            if (_importRegionPoints.Count > 2)
            {
                SKPath regionPath = Utilities.BuildClosedPath(_importRegionPoints);
                canvas.DrawPath(regionPath, PaintObjects.DebugPaint);
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
        // ~MapPathTool()
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
