#nullable enable

using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.ViewModels.Panels;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace RealmStudioX.WPF.Editor.Tools
{
    public sealed class LandformTool(
        CommandManager commands,
        IAssetProvider assets,
        MapLayer targetLayer,
        MapScene scene,
        EditorState editorState,
        ILandformSettings landformSettings) : IToolEditor, IDisposable
    {
        // -------------------------------------------------
        // Dependencies
        // -------------------------------------------------
        private readonly CommandManager _commands = commands;
        private readonly MapLayer _layer = targetLayer;
        private readonly IAssetProvider _assets = assets;
        private readonly MapScene _scene = scene;
        private readonly EditorState _editorState = editorState;
        private readonly ILandformSettings _landformSettings = landformSettings;

        private readonly HashSet<Landform> _modifiedLandforms = [];

        private Cmd_ModifyLandforms? _activeModifyCommand;

        // -------------------------------------------------
        // Configuration
        // -------------------------------------------------


        private const float MinimumIslandArea = 400f;   // tune this; this could be made a user-configurable setting in the future.
                                                        // This is used to prevent tiny "islands" from being created when splitting landforms.
                                                        // it might also need to be made relative to the map size
                                                        // (e.g. a percentage of the total map area) rather than an absolute value,
                                                        // to be more intuitive for users working with different map sizes.

        private const float MinimumIslandWidth = 10f;   // unused for now, but could be used in future to prevent long thin "snakes"
        private const float MinimumIslandHeight = 10f;  // unused for now, but could be used in future to prevent long thin "snakes"


        // -------------------------------------------------
        // State
        // -------------------------------------------------

        private Landform? _activeLandform;
        private SKPoint _lastMouseWorld;
        private bool _painting;
        private bool disposedValue;


        // -------------------------------------------------
        // Input handling
        // -------------------------------------------------

        public void OnMouseDown(PointerState state)
        {
            _lastMouseWorld = state.WorldPoint;

            if (state.Button == EditorMouseButton.Left)
            {
                if (_editorState.CurrentDrawingMode == MapDrawingMode.LandErase)
                {
                    _activeModifyCommand = new(_layer);
                    ApplyErase(state.WorldPoint);
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.LandPaint)
                {
                    _activeModifyCommand = new(_layer);
                    BeginPaint(state.WorldPoint);
                }
            }
        }

        public void OnMouseMove(PointerState state)
        {
            _lastMouseWorld = state.WorldPoint;

            if (state.Button == EditorMouseButton.Left)
            {
                if (_editorState.CurrentDrawingMode == MapDrawingMode.LandPaint && _activeLandform != null)
                {
                    ContinuePaint(state.WorldPoint);
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.LandErase)
                {
                    ApplyErase(state.WorldPoint);
                }
            }
        }

        public void OnMouseUp(PointerState state)
        {
            if (state.Button == EditorMouseButton.Left)
            {
                if (_editorState.CurrentDrawingMode == MapDrawingMode.LandPaint && _activeModifyCommand != null)
                {
                    EndPaint();

                    foreach (var lf in _modifiedLandforms)
                    {
                        lf.EndInteractive();

                        var simplified = Utilities.SimplifyPath(lf.HitPath);
                        lf.ReplaceGeometry(simplified);
                    }

                    _commands.Execute(_activeModifyCommand);

                    _scene.MarkLandClipPathModified();


                }

                if (_editorState.CurrentDrawingMode == MapDrawingMode.LandErase && _activeModifyCommand != null)
                {
                    foreach (var lf in _modifiedLandforms.ToList())
                    {
                        lf.EndInteractive();

                        // Process splitting
                        ProcessPotentialSplit(lf);
                    }

                    _commands.Execute(_activeModifyCommand);

                    _scene.MarkLandClipPathModified();
                }

                _painting = false;
                _activeLandform = null;
                _activeModifyCommand = null;
                _modifiedLandforms.Clear();
            }
        }

        public void OnMouseDoubleClick(PointerState state)
        {
            // landform editor does nothing on double-click
        }

        public void OnMouseWheel(PointerState state)
        {
            // landform editor does nothing on mouse wheel
        }


        public void Cancel()
        {
            _activeLandform = null;
        }

        // -------------------------------------------------
        // Painting
        // -------------------------------------------------

        public void BeginPaint(SKPoint worldPos)
        {

            string hatchTextureId = ((AssetManager)_assets).GetByName(AssetType.HatchTexture, "Random Hatch")[0].Id;
            string dashTextureId = ((AssetManager)_assets).GetByName(AssetType.HatchTexture, "Watercolor Dashes")[0].Id;

            LandformShadingSettings shading = new()
            {
                UseTextureBackground = _landformSettings.TextureFill,
                LandformBackgroundColor = _landformSettings.LandformBackgroundColor.ToSKColor(),
                LandformOutlineColor = _landformSettings.LandformOutlineColor.ToSKColor(),
                LandformTextureId = _landformSettings.LandformTextureId,
                LandformTextureScale = 1.0f,
                LandformTextureMirror = false,
                LandformOutlineWidth = _landformSettings.LandformOutlineWidth,
                LandShadingDepth = _landformSettings.LandformShadingDepth,
            };

            CoastlineSettings coastlineSettings = new()
            {
                CoastlineStyle = _landformSettings.SelectedCoastlineStyle,
                EffectDistance = _landformSettings.CoastlineEffectDistance,
                CoastlineColor = _landformSettings.CoastlineColor.ToSKColor(),
                HatchTextureId = hatchTextureId,
                DashTextureId = dashTextureId,
            };

            _activeLandform = new Landform
            {
                BrushRadius = _landformSettings.LandformBrushSize / 2,
                Shading = shading.Clone(),
                Coastline = coastlineSettings.Clone(),
                RenderMode = LandformRenderMode.Interactive
            };

            _activeLandform.ResolveAssets(_assets);

            _activeLandform.BeginStroke(worldPos);
            _activeLandform.BeginInteractive();

            _activeModifyCommand?.RegisterNewLandform(_activeLandform);

            _modifiedLandforms.Add(_activeLandform);

            _painting = true;
        }

        public void ContinuePaint(SKPoint worldPos)
        {
            if (_activeLandform == null)
            {
                return;
            }

            if (_modifiedLandforms.Add(_activeLandform))
            {
                _activeModifyCommand?.CaptureBefore(_activeLandform);
            }

            _activeLandform.AddStrokePoint(worldPos);
        }

        public void EndPaint()
        {
            if (!_painting || _activeLandform == null || _activeModifyCommand == null)
            {
                return;
            }

            _activeLandform.EndStroke();

            MergeOverlappingLandforms();

            //_activeLandform.EndInteractive();

            _activeLandform.InvalidateRenderCache();
        }

        // -------------------------------------------------
        // Erasing
        // -------------------------------------------------

        private void ApplyErase(SKPoint worldPos)
        {
            float r = _landformSettings.LandformEraserSize / 2;

            // Precompute eraser circle path once
            using var erasePath = new SKPath();
            erasePath.AddCircle(worldPos.X, worldPos.Y, r);

            foreach (var shape in _layer.Shapes)
            {
                if (shape is not Landform lf)
                    continue;

                if (!lf.HitPath.Bounds.IntersectsWith(erasePath.Bounds))
                    continue;

                // Fast bounds rejection (expand bounds by brush radius)
                var expandedBounds = lf.Bounds;
                expandedBounds.Inflate(r, r);

                if (!expandedBounds.Contains(worldPos))
                    continue;

                // Precise intersection test
                using var intersection = lf.HitPath.Op(erasePath, SKPathOp.Intersect);

                if (intersection == null || intersection.IsEmpty)
                    continue;

                // Ensure we only capture state once per modified shape
                if (!_modifiedLandforms.Contains(lf))
                {
                    _activeModifyCommand!.CaptureBefore(lf);

                    lf.BeginInteractive();
                    _modifiedLandforms.Add(lf);
                }
   
                lf.EraseCircle(worldPos, r);

            }
        }

        // -------------------------------------------------
        // Merging/splitting landforms
        // -------------------------------------------------

        private void MergeOverlappingLandforms()
        {
            if (_activeLandform == null
                || _activeLandform.HitPath == null
                || _activeLandform.HitPath.PointCount == 0
                || _activeModifyCommand == null)
            {
                return;
            }

            var active = _activeLandform;

            SKPath unionPath = new(active.HitPath);

            var toMerge = new List<Landform>();

            foreach (var shape in _layer.Shapes)
            {
                if (shape is not Landform existing)
                    continue;

                if (existing == active)
                    continue;

                if (!existing.Bounds.IntersectsWith(active.Bounds))
                    continue;

                using var intersection =
                    existing.HitPath.Op(active.HitPath, SKPathOp.Intersect);

                if (intersection == null || intersection.IsEmpty)
                    continue;

                toMerge.Add(existing);
            }

            foreach (var existing in toMerge)
            {
                _activeModifyCommand.CaptureBefore(existing);
                _activeModifyCommand.RegisterRemovedLandform(existing);

                var newUnion = unionPath.Op(existing.HitPath, SKPathOp.Union);

                unionPath.Dispose();
                unionPath = newUnion;

            }

            active.RestoreGeometry(unionPath);
            unionPath.Dispose();
        }

        private void ProcessPotentialSplit(Landform lf)
        {
            if (lf.HitPath.IsEmpty)
            {
                _activeModifyCommand!.RegisterRemovedLandform(lf);
                return;
            }

            var contours = Utilities.ExtractContours(lf.HitPath);

            if (contours.Count <= 1)
            {
                return;
            }

            const float minArea = MinimumIslandArea;
            var validContours = new List<SKPath>();

            foreach (var contour in contours)
            {
                float area = ComputeArea(contour);
                if (area >= minArea)
                {
                    validContours.Add(contour);
                }
            }

            if (validContours.Count == 0)
            {
                _activeModifyCommand!.RegisterRemovedLandform(lf);
                return;
            }

            if (validContours.Count == 1)
            {
                lf.ReplaceGeometry(validContours[0]);
                return;
            }

            // TRUE SPLIT

            lf.EndInteractive();
            _activeModifyCommand!.RegisterRemovedLandform(lf);

            foreach (var contour in validContours)
            {
                var newLand = new Landform();
                newLand.CloneSettingsFrom(lf);

                newLand.ResolveAssets(_assets);

                newLand.ReplaceGeometry(contour);
                newLand.EndInteractive();

                _activeModifyCommand!.RegisterNewLandform(newLand);

                newLand.InvalidateRenderCache();
            }
        }

        private static float ComputeArea(SKPath path)
        {
            if (path == null || path.IsEmpty)
                return 0f;

            float totalArea = 0f;

            using var measure = new SKPathMeasure(path, false);

            const int sampleCount = 64;
            var points = new SKPoint[sampleCount];

            do
            {
                float length = measure.Length;

                if (length <= 0f)
                    continue;

                for (int i = 0; i < sampleCount; i++)
                {
                    float distance = length * i / (sampleCount - 1);
                    measure.GetPosition(distance, out points[i]);
                }

                totalArea += Math.Abs(Shoelace(points));

            } while (measure.NextContour());

            return totalArea;
        }

        private static float Shoelace(ReadOnlySpan<SKPoint> pts)
        {
            if (pts.Length < 3)
                return 0f;

            float area = 0f;

            for (int i = 0; i < pts.Length; i++)
            {
                var p1 = pts[i];
                var p2 = pts[(i + 1) % pts.Length];

                area += (p1.X * p2.Y) - (p2.X * p1.Y);
            }

            return area * 0.5f;
        }

        // -------------------------------------------------
        // Overlay (lightweight, O(1))
        // -------------------------------------------------

        public void RenderOverlay(SKCanvas canvas, SKPoint world)
        {
            _activeLandform?.Render(canvas, null);

            if (_editorState.CurrentDrawingMode == MapDrawingMode.LandErase || _editorState.CurrentDrawingMode == MapDrawingMode.LandPaint)
            {
                var brushRadius = _editorState.CurrentDrawingMode
                    == MapDrawingMode.LandErase ? _landformSettings.LandformEraserSize / 2 : _landformSettings.LandformBrushSize / 2;

                canvas.DrawCircle(
                    world,
                    brushRadius,
                    PaintObjects.CursorCirclePaint);
            }
        }

        public void Activate()
        {
        }

        public void Deactivate()
        {

        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _activeModifyCommand?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~LandformTool()
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
