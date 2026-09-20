using RealmStudioShapeRenderingLib;
using RealmStudioX._3D.Models;
using RealmStudioX.Core;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.ViewModels.Panels;
using SkiaSharp;
using System.Collections;
using System.Windows.Input;
using Application = System.Windows.Application;
using Cursors = System.Windows.Input.Cursors;

namespace RealmStudioX.WPF.Editor.Tools
{
    internal class HeightMapTool(EditorController editor, MainWindowViewModel mainViewModel) : IToolEditor, IDisposable
    {
        private bool disposedValue;
        // -------------------------------------------------
        // Dependencies
        // -------------------------------------------------

        private readonly EditorController _editor = editor;
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

        // Separate region for the current mouse stroke. The render
        // region may be consumed by the background worker before
        // MouseUp, but the 3D terrain still needs the complete stroke.
        private int _strokeModLeft;
        private int _strokeModTop;
        private int _strokeModRight;
        private int _strokeModBottom;
        private bool _strokeHasModRegion;

        private readonly object _heightMapRenderLock = new();

        private Task? _heightMapRenderTask;

        private bool _heightMapRenderRequested;

        private int _heightMapRenderGeneration;

        private readonly List<Landform> _heightMapLandforms = [];

        private readonly object _heightMapModifiedRegionLock = new();

        public void Activate()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

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

                _mainViewModel.LandformViewModel.UpdateLandformBoundaries();


                // TODO: this call is here temporarily to clean up heightmaps used for testing
                // it can be removed once the code is ready for production
                if (activeHeightMap != null && activeHeightMap.HeightMap != null)
                {
                    LandformPanelViewModel.ClearHeightsOutsideLandforms(activeHeightMap.HeightMap, _mainViewModel.LandformViewModel.LandformBoundaries);
                }

                _heightMapLandforms.Clear();

                MapLayer landformLayer =
                    MapBuilder.GetMapLayerByIndex(
                        _editor.Scene!.Map,
                        MapBuilder.LANDFORMLAYER);

                foreach (MapComponent2D shape in landformLayer.Shapes)
                {
                    if (shape is Landform landform &&
                        !landform.HitPath.IsEmpty)
                    {
                        _heightMapLandforms.Add(landform);
                    }
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
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
            _editor.CommandService!.MarkMapModified();

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
                ResetHeightMapModifiedRegion();

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


        public bool IsInsideLandform(int x, int y)
        {
            BitArray? mask = _mainViewModel.HeightMapManager.LandformMask;


            if (mask == null)
                return false;

            if ((uint)x >= (uint)_mainViewModel.HeightMapManager.LandformMaskWidth ||
                (uint)y >= (uint)_mainViewModel.HeightMapManager.LandformMaskHeight)
            {
                return false;
            }

            return mask[
                y * _mainViewModel.HeightMapManager.LandformMaskWidth + x];
        }

        public void ApplySmoothingBrushAtPointer(PointerState state)
        {
            if (state.Button != EditorMouseButton.Left ||
                _editor.Scene == null ||
                activeHeightMap == null ||
                activeHeightMap.HeightMap == null)
            {
                return;
            }

            float[,] heightMap =
                activeHeightMap.HeightMap;

            int width =
                heightMap.GetLength(0);

            int height =
                heightMap.GetLength(1);

            ApplySmoothingBrush(
                state.WorldPoint.X,
                state.WorldPoint.Y,
                _brushRadius,
                heightMap,
                _smoothingStrength);

            int left =
                (int)Math.Max(
                    1,
                    state.WorldPoint.X - _brushRadius);

            int right =
                (int)Math.Min(
                    width - 2,
                    state.WorldPoint.X + _brushRadius);

            int top =
                (int)Math.Max(
                    1,
                    state.WorldPoint.Y - _brushRadius);

            int bottom =
                (int)Math.Min(
                    height - 2,
                    state.WorldPoint.Y + _brushRadius);

            activeHeightMap.InvalidateContours();

            AccumulateHeightMapModifiedRegion(
                left,
                top,
                right,
                bottom);

            RequestHeightMapRender();
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
            int left;
            int top;
            int right;
            int bottom;
            bool hasRegion;

            lock (_heightMapModifiedRegionLock)
            {
                left = _strokeModLeft;
                top = _strokeModTop;
                right = _strokeModRight;
                bottom = _strokeModBottom;
                hasRegion = _strokeHasModRegion;

                _strokeHasModRegion = false;
            }

            if (hasRegion)
            {
                _activeTerrain?.UpdateRegion(
                    left,
                    top,
                    right,
                    bottom);
            }
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

        private void ApplyHeightMapBrush(PointerState state, MapHeightMap activeHeightMap, float heightChange, float brushRadius)
        {
            ChangeHeightMapAreaHeight(_editor.Scene!.Map, activeHeightMap, state.WorldPoint, brushRadius, heightChange);
        }

        private void ResetHeightMapModifiedRegion()
        {
            lock (_heightMapModifiedRegionLock)
            {
                _heightMapModLeft = 1;
                _heightMapModTop = 1;
                _heightMapModRight = 0;
                _heightMapModBottom = 0;
                _heightMapHasModRegion = false;

                _strokeModLeft = 1;
                _strokeModTop = 1;
                _strokeModRight = 0;
                _strokeModBottom = 0;
                _strokeHasModRegion = false;
            }
        }

        internal void ChangeHeightMapAreaHeight(
            RealmStudioMap? map,
            MapHeightMap activeHeightMap,
            SKPoint mapPoint,
            float brushRadius,
            float changeAmount)
        {
            ArgumentNullException.ThrowIfNull(map);

            float[,]? heightMap =
                activeHeightMap.HeightMap;

            if (heightMap == null)
                return;

            ApplyHeightBrush(
                mapPoint.X,
                mapPoint.Y,
                brushRadius,
                heightMap,
                changeAmount);

            int left =
                (int)Math.Max(
                    1,
                    mapPoint.X - brushRadius);

            int right =
                (int)Math.Min(
                    map.MapWidth - 2,
                    mapPoint.X + brushRadius);

            int top =
                (int)Math.Max(
                    1,
                    mapPoint.Y - brushRadius);

            int bottom =
                (int)Math.Min(
                    map.MapHeight - 2,
                    mapPoint.Y + brushRadius);

            if (left > right || top > bottom)
                return;

            activeHeightMap.InvalidateContours();

            /*
             * Accumulate the changed region and increment the render
             * generation under the same lock. This keeps the region
             * and generation as one consistent render request.
             */
            AccumulateHeightMapModifiedRegion(
                left,
                top,
                right,
                bottom);

            RequestHeightMapRender();
        }

        private void RequestHeightMapRender()
        {
            lock (_heightMapRenderLock)
            {
                /*
                 * Coalesce mouse-move requests into one worker. The worker
                 * always renders the newest accumulated modified region.
                 */
                _heightMapRenderRequested = true;

                if (_heightMapRenderTask != null &&
                    !_heightMapRenderTask.IsCompleted)
                {
                    return;
                }

                _heightMapRenderTask =
                    Task.Run(ProcessHeightMapRenderQueue);
            }
        }

        private void ProcessHeightMapRenderQueue()
        {
            while (true)
            {
                lock (_heightMapRenderLock)
                {
                    if (!_heightMapRenderRequested)
                    {
                        _heightMapRenderTask = null;
                        return;
                    }

                    _heightMapRenderRequested = false;
                }

                if (!TryGetHeightMapRenderRequest(
                        out SKRect modifiedRect,
                        out int generation))
                {
                    continue;
                }

                RenderHeightMapPatches(
                    generation,
                    modifiedRect);
            }
        }

        private void RenderHeightMapPatches(
            int generation,
            SKRect modifiedRect)
        {
            MapHeightMap? heightMap =
                activeHeightMap;

            if (heightMap == null ||
                heightMap.HeightMap == null ||
                modifiedRect.IsEmpty)
            {
                return;
            }

            List<HeightMapPatchResult> results = [];

            try
            {
                foreach (Landform landform in _heightMapLandforms)
                {
                    /*
                     * A patch is normally tiny. If the mouse has moved
                     * while we are rendering, abandon this batch instead
                     * of producing stale pixels.
                     */
                    if (generation !=
                        Volatile.Read(
                            ref _heightMapRenderGeneration))
                    {
                        DisposePatchResults(results);
                        return;
                    }

                    landform.PerimeterPath.GetBounds(
                        out SKRect landformBounds);

                    if (!landformBounds.IntersectsWith(
                            modifiedRect))
                    {
                        continue;
                    }

                    SKBitmap? patch =
                        landform.CreateHeightMapPatch(
                            heightMap,
                            modifiedRect,
                            out SKRect patchBounds);

                    if (patch == null)
                        continue;

                    /*
                     * The worker never modifies the bitmap currently being
                     * displayed. It applies the patch to the Landform's
                     * back buffer instead.
                     */
                    landform.ApplyHeightMapPatchToBackBuffer(
                        patch,
                        patchBounds);

                    results.Add(
                        new HeightMapPatchResult(
                            landform,
                            patch,
                            patchBounds));
                }

                if (generation !=
                    Volatile.Read(
                        ref _heightMapRenderGeneration))
                {
                    DisposePatchResults(results);
                    return;
                }

                InstallHeightMapPatchResults(
                    results,
                    generation);
            }
            catch
            {
                DisposePatchResults(results);
                throw;
            }
        }

        private bool TryGetHeightMapRenderRequest(
            out SKRect modifiedRect,
            out int generation)
        {
            lock (_heightMapModifiedRegionLock)
            {
                if (!_heightMapHasModRegion)
                {
                    modifiedRect = SKRect.Empty;

                    generation =
                        Volatile.Read(
                            ref _heightMapRenderGeneration);

                    return false;
                }

                modifiedRect = new SKRect(
                    _heightMapModLeft,
                    _heightMapModTop,
                    _heightMapModRight + 1,
                    _heightMapModBottom + 1);

                generation =
                    Volatile.Read(
                        ref _heightMapRenderGeneration);

                return true;
            }
        }

        private void AccumulateHeightMapModifiedRegion(
            int left,
            int top,
            int right,
            int bottom)
        {
            lock (_heightMapModifiedRegionLock)
            {
                if (!_heightMapHasModRegion)
                {
                    _heightMapModLeft = left;
                    _heightMapModTop = top;
                    _heightMapModRight = right;
                    _heightMapModBottom = bottom;
                    _heightMapHasModRegion = true;
                }
                else
                {
                    _heightMapModLeft =
                        Math.Min(
                            _heightMapModLeft,
                            left);

                    _heightMapModTop =
                        Math.Min(
                            _heightMapModTop,
                            top);

                    _heightMapModRight =
                        Math.Max(
                            _heightMapModRight,
                            right);

                    _heightMapModBottom =
                        Math.Max(
                            _heightMapModBottom,
                            bottom);
                }

                /*
                 * Keep the generation synchronized with the modified
                 * region. The worker captures both under this same lock.
                 */
                Interlocked.Increment(
                    ref _heightMapRenderGeneration);

                /*
                 * The 3D terrain uses a separate stroke region. It is
                 * intentionally not cleared when a 2D patch is installed.
                 */
                if (!_strokeHasModRegion)
                {
                    _strokeModLeft = left;
                    _strokeModTop = top;
                    _strokeModRight = right;
                    _strokeModBottom = bottom;
                    _strokeHasModRegion = true;
                }
                else
                {
                    _strokeModLeft =
                        Math.Min(
                            _strokeModLeft,
                            left);

                    _strokeModTop =
                        Math.Min(
                            _strokeModTop,
                            top);

                    _strokeModRight =
                        Math.Max(
                            _strokeModRight,
                            right);

                    _strokeModBottom =
                        Math.Max(
                            _strokeModBottom,
                            bottom);
                }
            }
        }

        private static void DisposePatchResults(
            List<HeightMapPatchResult> results)
        {
            foreach (HeightMapPatchResult result in results)
            {
                result.Patch.Dispose();
            }
        }

        private void InstallHeightMapPatchResults(
            List<HeightMapPatchResult> results,
            int generation)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                /*
                 * The UI thread gets the final say. If the user moved the
                 * mouse since this batch was rendered, none of these
                 * patches are allowed to reach the displayed bitmaps.
                 */
                if (generation !=
                    Volatile.Read(
                        ref _heightMapRenderGeneration))
                {
                    DisposePatchResults(results);
                    RequestHeightMapRender();
                    return;
                }

                foreach (HeightMapPatchResult result in results)
                {
                    result.Landform.CommitHeightMapPatch(
                        result.Patch,
                        result.PatchBounds);

                    result.Patch.Dispose();
                }

                results.Clear();

                /*
                 * Consume the rendered region only if no newer height
                 * modification has occurred. The stroke region remains
                 * untouched for the 3D terrain update on MouseUp.
                 */
                lock (_heightMapModifiedRegionLock)
                {
                    if (generation ==
                        Volatile.Read(
                            ref _heightMapRenderGeneration))
                    {
                        _heightMapHasModRegion = false;

                        _heightMapModLeft = 1;
                        _heightMapModTop = 1;
                        _heightMapModRight = 0;
                        _heightMapModBottom = 0;
                    }
                }

                _editor.RequestRedraw();
            });
        }

        private sealed class HeightMapPatchResult
        {
            public Landform Landform { get; }

            public SKBitmap Patch { get; }

            public SKRect PatchBounds { get; }

            public HeightMapPatchResult(
                Landform landform,
                SKBitmap patch,
                SKRect patchBounds)
            {
                Landform = landform;
                Patch = patch;
                PatchBounds = patchBounds;
            }
        }

        private void ApplyHeightBrush(
            float centerX,
            float centerY,
            float radius,
            float[,] heightMap,
            float changeAmount)
        {
            BitArray? landformMask = _mainViewModel.HeightMapManager.LandformMask;

            if (landformMask == null)
                return;

            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            float radiusSquared =
                radius * radius;

            int left =
                (int)Math.Max(
                    1,
                    centerX - radius);

            int right =
                (int)Math.Min(
                    width - 2,
                    centerX + radius);

            int top =
                (int)Math.Max(
                    1,
                    centerY - radius);

            int bottom =
                (int)Math.Min(
                    height - 2,
                    centerY + radius);

            for (int y = top; y <= bottom; y++)
            {
                float dy =
                    y - centerY;

                float dySquared =
                    dy * dy;

                int rowIndex =
                    y * width;

                for (int x = left; x <= right; x++)
                {
                    float dx =
                        x - centerX;

                    float distanceSquared =
                        dx * dx + dySquared;

                    if (distanceSquared >
                        radiusSquared)
                    {
                        continue;
                    }

                    if (!landformMask[rowIndex + x])
                        continue;

                    float falloff =
                        1.0f -
                        distanceSquared /
                        radiusSquared;

                    heightMap[x, y] +=
                        changeAmount * falloff;
                }
            }
        }

        private void ApplySmoothingBrush(
            float centerX,
            float centerY,
            float radius,
            float[,] heightMap,
            float smoothingStrength)
        {
            BitArray? landformMask = _mainViewModel.HeightMapManager.LandformMask;

            if (landformMask == null)
                return;

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
                    if (!landformMask[y * width + x])
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
