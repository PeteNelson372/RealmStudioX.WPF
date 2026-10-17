#nullable enable

using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor.Services;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.ViewModels.Panels;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace RealmStudioX.WPF.Editor.Tools
{
    public sealed class LandformTool(
        CommandManager commands,
        MainWindowViewModel mainWindowViewModel,
        PaintService paintService,
        MapLayer targetLayer,
        MapScene scene,
        EditorController editor,
        ILandformSettings landformSettings) : IToolEditor, IDisposable
    {
        // -------------------------------------------------
        // Dependencies
        // -------------------------------------------------
        private readonly CommandManager _commands = commands;
        private readonly MapLayer _layer = targetLayer;
        private readonly MainWindowViewModel _mainViewModel = mainWindowViewModel;
        private readonly IAssetProvider _assets = mainWindowViewModel.AssetManager;
        private readonly PaintService _paintService = paintService;
        private readonly MapScene _scene = scene;
        private readonly EditorController _editor = editor;
        private readonly ILandformSettings _landformSettings = landformSettings;

        private readonly HashSet<Landform> _modifiedLandforms = [];

        private Cmd_ModifyLandforms? _activeModifyCommand;

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
                if (_editor.CurrentDrawingMode == MapDrawingMode.LandErase)
                {
                    _activeModifyCommand = new(_layer);
                    ApplyErase(state.WorldPoint);
                }
                else if (_editor.CurrentDrawingMode == MapDrawingMode.LandPaint)
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
                if (_editor.CurrentDrawingMode == MapDrawingMode.LandPaint && _activeLandform != null)
                {
                    ContinuePaint(state.WorldPoint);
                }
                else if (_editor.CurrentDrawingMode == MapDrawingMode.LandErase)
                {
                    ApplyErase(state.WorldPoint);
                }
            }
        }

        public void OnMouseUp(PointerState state)
        {
            if (state.Button == EditorMouseButton.Left)
            {
                MapLayer landLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.LANDFORMLAYER);
                MapLayer landDrawingLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.LANDDRAWINGLAYER);

                if (_editor.CurrentDrawingMode == MapDrawingMode.LandPaint && _activeModifyCommand != null && _activeLandform != null)
                {
                    _activeLandform.EndStroke();

                    LandformPanelViewModel.MergeOverlappingLandforms(_activeLandform, _activeModifyCommand, landLayer);

                    _activeLandform.EndInteractive();

                    _editor.Scene!.MarkLandClipPathModified();

                    landLayer.InvalidateTiles(_activeLandform.Bounds);
                    landDrawingLayer.InvalidateTiles(_activeLandform.Bounds);

                    foreach (var lf in _modifiedLandforms)
                    {
                        lf.EndInteractive();

                        //var simplified = RealmStudioShapeRenderingLib.Utilities.SimplifyPath(lf.HitPath);
                        //lf.ReplaceGeometry(simplified);

                        landLayer.InvalidateTiles(lf.Bounds);
                        landDrawingLayer.InvalidateTiles(lf.Bounds);
                    }

                    _commands.Execute(_activeModifyCommand);
                }

                if (_editor.CurrentDrawingMode == MapDrawingMode.LandErase && _activeModifyCommand != null)
                {
                    foreach (var lf in _modifiedLandforms.ToList())
                    {
                        lf.EndInteractive();

                        // Process splitting
                        _mainViewModel.LandformViewModel.ProcessPotentialSplit(lf, _activeModifyCommand);

                        landLayer.InvalidateTiles(lf.Bounds);
                        landDrawingLayer.InvalidateTiles(lf.Bounds);
                    }

                    _commands.Execute(_activeModifyCommand);
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

        // -------------------------------------------------
        // Erasing
        // -------------------------------------------------

        private void ApplyErase(SKPoint worldPos)
        {
            float r = _landformSettings.LandformEraserSize / 2;

            // Precompute eraser circle path once
            SKPathBuilder erasePathBuilder = new()
            {
                FillType = SKPathFillType.EvenOdd
            };

            erasePathBuilder.AddCircle(worldPos.X, worldPos.Y, r);

            var erasePath = erasePathBuilder.Snapshot();

            erasePathBuilder.Detach();
            erasePathBuilder.Dispose();

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

                // Precise intersection test -- turns out to be not necessary, as the EraseCircle method will handle it internally
                //using var intersection = lf.HitPath.Op(erasePath, SKPathOp.Intersect);

                //if (intersection == null || intersection.IsEmpty)
                //    continue;

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
        // Overlay (lightweight, O(1))
        // -------------------------------------------------

        public void RenderOverlay(SKCanvas canvas, SKPoint world)
        {
            _activeLandform?.Render(canvas, null);

            if (_editor.CurrentDrawingMode == MapDrawingMode.LandErase || _editor.CurrentDrawingMode == MapDrawingMode.LandPaint)
            {
                var brushRadius = _editor.CurrentDrawingMode
                    == MapDrawingMode.LandErase ? _landformSettings.LandformEraserSize / 2 : _landformSettings.LandformBrushSize / 2;

                canvas.DrawCircle(world, brushRadius, PaintObjects.CursorCirclePaint);
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
