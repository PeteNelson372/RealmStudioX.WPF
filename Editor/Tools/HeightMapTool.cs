using RealmStudioShapeRenderingLib;
using RealmStudioX._3D.Models;
using RealmStudioX.Core;
using RealmStudioX.WPF.Editor.Services;
using RealmStudioX.WPF.ViewModels.Main;
using SharpDX.Mathematics.Interop;
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

        public MapHeightMap? ActiveHeightMap => activeHeightMap;

        private HeightMapTerrain3D? _activeTerrain = null;
        public HeightMapTerrain3D? ActiveTerrain
        {
            get { return _activeTerrain; }
            set { _activeTerrain = value; }
        }

        float _heightChange = 0;
        float _brushRadius = 0;
        float _smoothingStrength = 0;

        private int _heightMapModLeft;
        private int _heightMapModTop;
        private int _heightMapModRight;
        private int _heightMapModBottom;
        private bool _heightMapHasModRegion;

        private readonly record struct LandformBoundary(SKPath Perimeter, SKRect Bounds);

        private List<LandformBoundary> _landformBoundaries = [];

        public void Activate()
        {
            MapLayer heightMapLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.HEIGHTMAPLAYER);

            foreach (MapComponent2D mc2d in heightMapLayer.Shapes)
            {
                if (mc2d is MapHeightMap mhm)
                {
                    activeHeightMap = mhm;

                    activeHeightMap.MinimumElevation = _mainViewModel.HeightMapViewModel.MinimumElevation;
                    activeHeightMap.MaximumElevation = _mainViewModel.HeightMapViewModel.MaximumElevation;
                    activeHeightMap.HeightMapPalette = _mainViewModel.HeightMapViewModel.SelectedPalette;

                    activeHeightMap.RebuildHypsometricColorLookup();

                    break;
                }
            }

            // get landform perimeters and bounds
            MapLayer landformLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.LANDFORMLAYER);

            _landformBoundaries.Clear();

            foreach (Shape2D shape in landformLayer.Shapes)
            {
                if (shape is Landform landform)
                {
                    SKPath perimeter = landform.PerimeterPath;

                    _landformBoundaries.Add(
                        new LandformBoundary(
                            perimeter,
                            perimeter.Bounds));
                }
            }

            // TODO: this call is here temporarily to clean up heightmaps used for testing
            // it can be removed once the code is ready for production
            if (activeHeightMap != null && activeHeightMap.HeightMap != null)
            {
                ClearHeightsOutsideLandforms(activeHeightMap.HeightMap, _landformBoundaries);
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
            if (_editor.CurrentDrawingMode == MapDrawingMode.MapHeightIncrease || _editor.CurrentDrawingMode == MapDrawingMode.MapHeightDecrease)
            {
                _heightChange = _mainViewModel.HeightMapViewModel.ElevationChange;

                if (_editor.CurrentDrawingMode == MapDrawingMode.MapHeightDecrease)
                {
                    _heightChange = -_heightChange;
                }

                _brushRadius = _mainViewModel.HeightMapViewModel.HeightMapBrushSize / 2.0f;

                ResetHeightMapModifiedRegion();

                ChangeHeightMapElevationAtPointer(state);
            }
            else if (_editor.CurrentDrawingMode == MapDrawingMode.MapHeightSmooth)
            {
                _brushRadius = _mainViewModel.HeightMapViewModel.HeightMapBrushSize / 2.0f;
                _smoothingStrength = _mainViewModel.HeightMapViewModel.SmoothingStrength / 100.0f;

                ApplySmoothingBrushAtPointer(state);
            }
        }

        public void OnMouseMove(PointerState state)
        {
            if (_editor.CurrentDrawingMode == MapDrawingMode.MapHeightIncrease || _editor.CurrentDrawingMode == MapDrawingMode.MapHeightDecrease)
            {
                ChangeHeightMapElevationAtPointer(state);
            }
            else if (_editor.CurrentDrawingMode == MapDrawingMode.MapHeightSmooth)
            {
                ApplySmoothingBrushAtPointer(state);
            }
        }

        private static void ClearHeightsOutsideLandforms(float[,] heightMap, IReadOnlyList<LandformBoundary> boundaries)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool insideLandform = false;

                    foreach (LandformBoundary boundary in boundaries)
                    {
                        if (!boundary.Bounds.Contains(x, y))
                            continue;

                        if (boundary.Perimeter.Contains(x, y))
                        {
                            insideLandform = true;
                            break;
                        }
                    }

                    if (!insideLandform)
                        heightMap[x, y] = 0.0f;
                }
            }
        }

        public void ApplySmoothingBrushAtPointer(PointerState state)
        {
            if (state.Button == EditorMouseButton.Left
                && _editor.Scene != null
                && activeHeightMap != null
                && activeHeightMap.HeightMap != null)
            {
                float[,]? heightMap = activeHeightMap.HeightMap;
                int width = heightMap.GetLength(0);
                int height = heightMap.GetLength(1);

                SKBitmap? heightMapBitmap = activeHeightMap.HeightMapBitmap;

                if (heightMapBitmap != null && heightMap != null)
                {
                    ApplySmoothingBrush(state.WorldPoint.X, state.WorldPoint.Y, _brushRadius, activeHeightMap.HeightMap, _smoothingStrength);

                    int left = (int)Math.Max(1, state.WorldPoint.X - _brushRadius);
                    int right = (int)Math.Min(width - 2, state.WorldPoint.X + _brushRadius);
                    int top = (int)Math.Max(1, state.WorldPoint.Y - _brushRadius);
                    int bottom = (int)Math.Min(height - 2, state.WorldPoint.Y + _brushRadius);

                    activeHeightMap.UpdateHeightMapBitmap(heightMapBitmap, heightMap, left, top, right, bottom);

                    activeHeightMap.InvalidateContours();

                    AccumulateHeightMapModifiedRegion(left, top, right, bottom);
                }
            }
        }

        public void ChangeHeightMapElevationAtPointer(PointerState state)
        {
            if (state.Button == EditorMouseButton.Left && _editor.Scene != null && activeHeightMap != null)
            {
                ApplyHeightMapBrush(state, activeHeightMap, _heightChange, _brushRadius);
            }
        }



        public void OnMouseUp(PointerState state)
        {
            _activeTerrain?.UpdateRegion(_heightMapModLeft, _heightMapModTop, _heightMapModRight, _heightMapModBottom);
            ResetHeightMapModifiedRegion();
        }

        public void OnMouseWheel(PointerState state)
        {
            // no action
        }

        public void RenderOverlay(SKCanvas canvas, SKPoint world)
        {
            if (_mainViewModel.RenderHeightMap)
            {
                canvas.DrawCircle(world, _mainViewModel.HeightMapViewModel.HeightMapBrushSize / 2.0f, PaintObjects.CursorCircleGreenPaint);
            }
        }

        private bool IsInsideLandform(float x, float y)
        {
            foreach (LandformBoundary boundary in _landformBoundaries)
            {
                if (!boundary.Bounds.Contains(x, y))
                    continue;

                if (boundary.Perimeter.Contains(x, y))
                    return true;
            }

            return false;
        }

        private void ApplyHeightMapBrush(PointerState state, MapHeightMap activeHeightMap, float heightChange, float brushRadius)
        {
            ChangeHeightMapAreaHeight(_editor.Scene!.Map, activeHeightMap, state.WorldPoint, brushRadius, heightChange);

            _mainViewModel.CommandService.MarkMapModified();
        }

        private void ResetHeightMapModifiedRegion()
        {
            _heightMapHasModRegion = false;
        }

        internal void ChangeHeightMapAreaHeight(RealmStudioMap? map, MapHeightMap activeHeightMap, SKPoint mapPoint, float brushRadius, float changeAmount)
        {
            ArgumentNullException.ThrowIfNull(map);

            float[,]? heightMap = activeHeightMap.HeightMap;

            SKBitmap? heightMapBitmap = activeHeightMap.HeightMapBitmap;

            if (heightMapBitmap != null && heightMap != null)
            {
                ApplyHeightBrush(mapPoint.X, mapPoint.Y, brushRadius, heightMap, changeAmount);

                int left = (int)Math.Max(1, mapPoint.X - brushRadius);
                int right = (int)Math.Min(map.MapWidth - 2, mapPoint.X + brushRadius);
                int top = (int)Math.Max(1, mapPoint.Y - brushRadius);
                int bottom = (int)Math.Min(map.MapHeight - 2, mapPoint.Y + brushRadius);

                activeHeightMap.UpdateHeightMapBitmap(heightMapBitmap, heightMap, left, top, right, bottom);

                activeHeightMap.InvalidateContours();

                AccumulateHeightMapModifiedRegion(left, top, right, bottom);
            }
        }

        private void ApplyHeightBrush(
            float centerX,
            float centerY,
            float radius,
            float[,] heightMap,
            float changeAmount)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            float radiusSquared = radius * radius;

            int left = (int)Math.Max(1, centerX - radius);
            int right = (int)Math.Min(width - 2, centerX + radius);
            int top = (int)Math.Max(1, centerY - radius);
            int bottom = (int)Math.Min(height - 2, centerY + radius);

            for (int y = top; y <= bottom; y++)
            {
                float dy = y - centerY;
                float dySquared = dy * dy;

                for (int x = left; x <= right; x++)
                {
                    if (!IsInsideLandform(x, y))
                        continue;

                    float dx = x - centerX;

                    float distanceSquared =
                        dx * dx + dySquared;

                    if (distanceSquared > radiusSquared)
                        continue;

                    // Full strength at the center, falling smoothly
                    // to zero at the edge of the brush.
                    float falloff =
                        1.0f - distanceSquared / radiusSquared;

                    heightMap[x, y] +=
                        changeAmount * falloff;
                }
            }
        }

        private void AccumulateHeightMapModifiedRegion(
            int left,
            int top,
            int right,
            int bottom)
        {
            if (!_heightMapHasModRegion)
            {
                _heightMapModLeft = left;
                _heightMapModTop = top;
                _heightMapModRight = right;
                _heightMapModBottom = bottom;
                _heightMapHasModRegion = true;
                return;
            }

            _heightMapModLeft = Math.Min(_heightMapModLeft, left);

            _heightMapModTop = Math.Min(_heightMapModTop, top);

            _heightMapModRight = Math.Max(_heightMapModRight, right);

            _heightMapModBottom = Math.Max(_heightMapModBottom, bottom);
        }

        private void ApplySmoothingBrush(
            float centerX,
            float centerY,
            float radius,
            float[,] heightMap,
            float smoothingStrength)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            if (radius <= 0.0f || smoothingStrength <= 0.0f)
                return;

            smoothingStrength = Math.Clamp(
                smoothingStrength,
                0.0f,
                1.0f);

            float radiusSquared = radius * radius;

            int left = Math.Max(
                1,
                (int)MathF.Floor(centerX - radius));

            int right = Math.Min(
                width - 2,
                (int)MathF.Ceiling(centerX + radius));

            int top = Math.Max(
                1,
                (int)MathF.Floor(centerY - radius));

            int bottom = Math.Min(
                height - 2,
                (int)MathF.Ceiling(centerY + radius));

            if (left > right || top > bottom)
                return;

            // At the minimum brush size (radius = 2),
            // this gives the original 3x3 smoothing behavior.
            //
            // Larger brushes progressively smooth larger-scale
            // features.
            int smoothingRadius = Math.Max(
                1,
                (int)MathF.Round(radius * 0.5f));

            // The blur needs samples beyond the brush footprint.
            int sourceLeft = Math.Max(
                0,
                left - smoothingRadius);

            int sourceRight = Math.Min(
                width - 1,
                right + smoothingRadius);

            int sourceTop = Math.Max(
                0,
                top - smoothingRadius);

            int sourceBottom = Math.Min(
                height - 1,
                bottom + smoothingRadius);

            float[,] blurred = CreateBoxBlur(
                heightMap,
                sourceLeft,
                sourceTop,
                sourceRight,
                sourceBottom,
                smoothingRadius);

            for (int y = top; y <= bottom; y++)
            {
                float dy = y - centerY;
                float dySquared = dy * dy;

                for (int x = left; x <= right; x++)
                {
                    if (!IsInsideLandform(x, y))
                        continue;

                    float dx = x - centerX;

                    float distanceSquared =
                        dx * dx + dySquared;

                    if (distanceSquared > radiusSquared)
                        continue;

                    // Full strength at the center and zero
                    // at the edge of the circular brush.
                    float falloff =
                        1.0f -
                        distanceSquared / radiusSquared;

                    float amount =
                        smoothingStrength * falloff;

                    if (amount <= 0.0f)
                        continue;

                    float currentValue =
                        heightMap[x, y];

                    float smoothedValue =
                        blurred[
                            x - sourceLeft,
                            y - sourceTop];

                    heightMap[x, y] =
                        currentValue +
                        (smoothedValue - currentValue) * amount;
                }
            }
        }

        private static float[,] CreateBoxBlur(
            float[,] source,
            int left,
            int top,
            int right,
            int bottom,
            int radius)
        {
            int width = right - left + 1;
            int height = bottom - top + 1;

            float[,] horizontal =
                new float[width, height];

            float[,] result =
                new float[width, height];

            int kernelSize =
                radius * 2 + 1;

            float inverseKernelSize =
                1.0f / kernelSize;

            int sourceWidth =
                source.GetLength(0);

            int sourceHeight =
                source.GetLength(1);

            // ---------------------------------------------------------
            // Horizontal pass
            // ---------------------------------------------------------

            for (int y = top; y <= bottom; y++)
            {
                float sum = 0.0f;

                // Initial window.
                for (int offset = -radius;
                     offset <= radius;
                     offset++)
                {
                    int sampleX =
                        Math.Clamp(
                            left + offset,
                            0,
                            sourceWidth - 1);

                    sum += source[sampleX, y];
                }

                horizontal[0, y - top] =
                    sum * inverseKernelSize;

                // Slide the window across the row.
                for (int x = left + 1;
                     x <= right;
                     x++)
                {
                    int outgoingX =
                        Math.Clamp(
                            x - radius - 1,
                            0,
                            sourceWidth - 1);

                    int incomingX =
                        Math.Clamp(
                            x + radius,
                            0,
                            sourceWidth - 1);

                    sum -= source[outgoingX, y];
                    sum += source[incomingX, y];

                    horizontal[x - left, y - top] =
                        sum * inverseKernelSize;
                }
            }

            // ---------------------------------------------------------
            // Vertical pass
            // ---------------------------------------------------------

            for (int x = 0; x < width; x++)
            {
                float sum = 0.0f;

                // Initial window.
                for (int offset = -radius;
                     offset <= radius;
                     offset++)
                {
                    int sampleY =
                        Math.Clamp(
                            top + offset,
                            top,
                            bottom);

                    sum += horizontal[
                        x,
                        sampleY - top];
                }

                result[x, 0] =
                    sum * inverseKernelSize;

                // Slide the window down the column.
                for (int y = 1; y < height; y++)
                {
                    int absoluteY =
                        top + y;

                    int outgoingY =
                        Math.Clamp(
                            absoluteY - radius - 1,
                            top,
                            bottom);

                    int incomingY =
                        Math.Clamp(
                            absoluteY + radius,
                            top,
                            bottom);

                    sum -= horizontal[
                        x,
                        outgoingY - top];

                    sum += horizontal[
                        x,
                        incomingY - top];

                    result[x, y] =
                        sum * inverseKernelSize;
                }
            }

            return result;
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
