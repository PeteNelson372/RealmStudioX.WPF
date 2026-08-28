using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.Editor.Services;
using RealmStudioX.WPF.ViewModels.Main;
using SkiaSharp;

namespace RealmStudioX.WPF.Editor.Tools
{
    internal class HeightMapTool(
            EditorController editor,
            HeightMapManager heightMapManager,
            MainWindowViewModel mainViewModel) : IToolEditor, IDisposable
    {
        private bool disposedValue;
        // -------------------------------------------------
        // Dependencies
        // -------------------------------------------------

        private readonly EditorController _editor = editor;
        private readonly HeightMapManager _heightMapManager = heightMapManager;
        private readonly MainWindowViewModel _mainViewModel = mainViewModel;

        private MapHeightMap? activeHeightMap;

        public void Activate()
        {
            MapLayer heightMapLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.HEIGHTMAPLAYER);

            foreach (MapComponent2D mc2d in heightMapLayer.Shapes)
            {
                if (mc2d is MapHeightMap mhm)
                {
                    activeHeightMap = mhm;

                    activeHeightMap.MinimumHeight = _mainViewModel.HeightMapViewModel.MinimumHeight;
                    activeHeightMap.MaximumHeight = _mainViewModel.HeightMapViewModel.MaximumHeight;
                    activeHeightMap.HeightMapPalette = _mainViewModel.HeightMapViewModel.SelectedPalette;

                    break;
                }
            }
        }

        public void Cancel()
        {

        }

        public void Deactivate()
        {

        }

        public void OnMouseDoubleClick(PointerState state)
        {
            // no action
        }

        public void OnMouseDown(PointerState state)
        {
            if (state.Button == EditorMouseButton.Left && _editor.Scene != null && activeHeightMap != null)
            {
                float heightChange = _mainViewModel.HeightMapViewModel.HeightChange;

                if (_editor.CurrentDrawingMode == MapDrawingMode.MapHeightDecrease)
                {
                    heightChange = -heightChange;
                }

                float brushRadius = _mainViewModel.LandformViewModel.LandformBrushSize / 2.0f;

                ApplyHeightMapBrush(state, activeHeightMap, heightChange, brushRadius);
            }
        }

        public void OnMouseMove(PointerState state)
        {
            if (state.Button == EditorMouseButton.Left && _editor.Scene != null && activeHeightMap != null)
            {
                float heightChange = _mainViewModel.HeightMapViewModel.HeightChange;

                if (_editor.CurrentDrawingMode == MapDrawingMode.MapHeightDecrease)
                {
                    heightChange = -heightChange;
                }

                float brushRadius = _mainViewModel.LandformViewModel.LandformBrushSize / 2.0f;

                ApplyHeightMapBrush(state, activeHeightMap, heightChange, brushRadius);
            }
        }

        public void OnMouseUp(PointerState state)
        {

        }

        public void OnMouseWheel(PointerState state)
        {
            // no action
        }

        public void RenderOverlay(SKCanvas canvas, SKPoint world)
        {
            canvas.DrawCircle(world, _mainViewModel.LandformViewModel.LandformBrushSize / 2.0f, PaintObjects.CursorCircleGreenPaint);
        }

        private void ApplyHeightMapBrush(PointerState state, MapHeightMap activeHeightMap, float heightChange, float brushRadius)
        {
            HeightMapManager.ChangeHeightMapAreaHeight(_editor.Scene!.Map, activeHeightMap, state.WorldPoint, brushRadius, heightChange);

            _mainViewModel.CommandService.MarkMapModified();
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
