using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Controls;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.ViewModels.Painting;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class LandformPanelViewModel : ViewModelBase, IPaintToolViewModel, ILandformSettings
    {
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

        private readonly MainWindowViewModel _mainWindowViewModel;
        public MainWindowViewModel MainViewModel => _mainWindowViewModel;

        private readonly EditorController _editor;
        private readonly AssetManager _assetManager;

        public ColorPalette? PaintPalette { get; }

        public LandformPanelViewModel(MainWindowViewModel mainViewModel, EditorController editor, AssetManager assetManager)
        {
            _mainWindowViewModel = mainViewModel;
            _editor = editor;
            _assetManager = assetManager;
            var browser = new AssetBrowser(_assetManager, AssetType.LandTexture);
            TextureBrowser = new AssetBrowserViewModel(browser);

            TextureBrowser.TextureSelectionChanged += LandformValuesChanged;

            var paletteBrowser = new AssetBrowser(_assetManager, AssetType.ColorPalette);

            IReadOnlyList<AssetDescriptor> paletteDescriptors = paletteBrowser.Assets;

            for (int i = 0; i < paletteDescriptors.Count; i++)
            {
                AssetDescriptor descriptor = paletteDescriptors[i];
                if (descriptor.Type == AssetType.ColorPalette)
                {
                    string xml = File.ReadAllText(descriptor.FilePath);
                    ColorPalette palette = MapFileMethods.DeserializeObject<ColorPalette>(xml);
                    if (palette != null && palette.PaletteType == ColorPaletteType.LandformColors)
                    {
                        PaintPalette = palette;

                        foreach (var colorEntry in PaintPalette.ColorEntries)
                        {
                            colorEntry.DisplayName = ColorPalette.GetColorName(colorEntry.Color);
                        }

                        break;
                    }
                }
            }
        }

        public readonly record struct LandformBoundary(SKPath Perimeter, SKRect Bounds);

        private readonly List<LandformBoundary> _landformBoundaries = [];

        public IReadOnlyList<LandformBoundary> LandformBoundaries => _landformBoundaries;

        public void UpdateLandformBoundaries()
        {
            MapLayer landformLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.LANDFORMLAYER);

            _landformBoundaries.Clear();

            foreach (MapComponent2D shape in landformLayer.Shapes)
            {
                if (shape is Landform landform)
                {
                    _landformBoundaries.Add(new LandformBoundary(landform.PerimeterPath, landform.PerimeterPath.Bounds));
                }
            }
        }

        public static void ClearHeightsOutsideLandforms(float[,] heightMap, IReadOnlyList<LandformBoundary> boundaries)
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

        private GeneratedLandformType _selectedLandformType = GeneratedLandformType.NotSet;
        
        public GeneratedLandformType SelectedLandformType
        {
            get => _selectedLandformType;
            set => SetProperty(ref _selectedLandformType, value);
        }

        private LandformCoastlineStyle _selectedCoastlineStyle = LandformCoastlineStyle.HatchPattern;

        public LandformCoastlineStyle SelectedCoastlineStyle
        {
            get => _selectedCoastlineStyle;
            set
            {
                if (SetProperty(ref _selectedCoastlineStyle, value))
                {
                    LandformValuesChanged();
                }
            }
        }

        private int _coastlineEffectDistance = 120;
        public int CoastlineEffectDistance
        {
            get => _coastlineEffectDistance;
            set
            {
                if (SetProperty(ref _coastlineEffectDistance, value))
                {
                    LandformValuesChanged();
                }
            }
        }

        public int MinLandformBrushSize { get; } = 4;
        public int MaxLandformBrushSize { get; } = 512;

        private int _landformBrushSize = 64;
        public int LandformBrushSize
        {
            get => _landformBrushSize;
            set
            {
                var clamped = Math.Clamp(value, MinLandformBrushSize, MaxLandformBrushSize);

                _landformBrushSize = clamped;
                
                OnPropertyChanged();
            }
        }

        private int _landformShadingDepth = 16;
        public int LandformShadingDepth
        {
            get => _landformShadingDepth;
            set
            {
                if (SetProperty(ref _landformShadingDepth, value))
                {
                    LandformValuesChanged();
                }
            }
        }

        private Color _landformOutlineColor = Color.FromArgb(255, 65, 55, 40);
        public Color LandformOutlineColor
        {
            get => _landformOutlineColor;
            set
            {
                if (SetProperty(ref _landformOutlineColor, value))
                {
                    _landformOutlineBrush.Color = value;
                    LandformValuesChanged();
                }
            }
        }

        private SolidColorBrush _landformOutlineBrush = new(Color.FromArgb(255, 65, 55, 40));

        public Brush LandformOutlineBrush => _landformOutlineBrush;

        public int _landformOutlineWidth = 2;

        public int LandformOutlineWidth
        {
            get => _landformOutlineWidth;
            set
            {
                if (SetProperty(ref _landformOutlineWidth, value))
                {
                    LandformValuesChanged();
                }
            }
        }

        private Color _landformBackgroundColor = Colors.White;
        public Color LandformBackgroundColor
        {
            get => _landformBackgroundColor;
            set
            {
                if (SetProperty(ref _landformBackgroundColor, value))
                {
                    _landformBackgroundBrush.Color = value;
                    LandformValuesChanged();
                }
            }
        }

        private SolidColorBrush _landformBackgroundBrush = new(Colors.White);

        public Brush LandformBackgroundBrush => _landformBackgroundBrush;


        private Color _coastlineColor = Color.FromArgb(187, 156, 195, 183);
        public Color CoastlineColor
        {
            get => _coastlineColor;
            set
            {
                if (SetProperty(ref _coastlineColor, value))
                {
                    _coastlineColorBrush.Color = value;
                    LandformValuesChanged();
                }
            }
        }

        private SolidColorBrush _coastlineColorBrush = new(Color.FromArgb(187, 156, 195, 183));

        public Brush CoastlineColorBrush => _coastlineColorBrush;

        private bool _textureFill = true;
        public bool TextureFill
        {
            get => _textureFill;
            set
            {
                if (SetProperty(ref _textureFill, value))
                {
                    LandformValuesChanged();
                }
            }
        }

        public string? LandformTextureId => TextureBrowser.SelectedAssetId;


        public int MinLandformEraserSize { get; } = 4;
        public int MaxLandformEraserSize { get; } = 512;

        public int _landformEraserSize = 64;

        public int LandformEraserSize
        {
            get => _landformEraserSize;
            set
            {
                var clamped = Math.Clamp(value, MinLandformEraserSize, MaxLandformEraserSize);

                _landformEraserSize = clamped;
                OnPropertyChanged();
            }
        }

        public AssetBrowserViewModel TextureBrowser { get; }

        public ObservableCollection<BrushPatternItem> BrushPatterns => _mainWindowViewModel.PaintService.BrushPatterns;

        public BrushPatternItem? SelectedBrushPattern
        {
            get => _mainWindowViewModel.PaintService.Settings.SelectedBrushPattern;
            set
            {
                _mainWindowViewModel.PaintService.Settings.SelectedBrushPattern = value;

                OnPropertyChanged(nameof(SelectedBrushPattern));

                MainViewModel.OnDrawingModeChanged(_editor.CurrentDrawingMode);
            }
        }

        public ICommand SelectBrushPatternCommand =>
            new RelayCommand(
                parameter =>
                {
                    if (parameter is not BrushPatternItem pattern)
                    {
                        return;
                    }

                    MainViewModel.PaintService.Settings.SelectedBrushPattern = pattern;

                    OnPropertyChanged(nameof(SelectedBrushPattern));

                    IsBrushPopupOpen = false;
                }, usesParameter: true);

        private bool _isBrushPopupOpen;

        public bool IsBrushPopupOpen
        {
            get => _isBrushPopupOpen;

            set => SetProperty(ref _isBrushPopupOpen, value);
        }

        // painting color

        private SKColor _paintingColor = SKColors.Black;
        public SKColor PaintingColor
        {
            get => _paintingColor;
            set
            {
                if (SetProperty(ref _paintingColor, value))
                {
                    _paintingColorBrush.Color = value.ToColor();
                    _mainWindowViewModel.PaintService.Settings.SelectedColor = value;
                }
            }
        }

        private SolidColorBrush _paintingColorBrush = new(Colors.Black);
        public Brush PaintingColorBrush => _paintingColorBrush;

        // brush spacing
        public int BrushSpacing
        {
            get => _mainWindowViewModel.PaintService.Settings.BrushSpacing;
            set
            {
                _mainWindowViewModel.PaintService.Settings.BrushSpacing = value;
            }
        }

        public int MinBrushSize { get; } = 1;
        public int MaxBrushSize { get; } = 256;

        public int PaintBrushSize
        {
            get => _mainWindowViewModel.PaintService.Settings.BrushSize;
            set
            {
                var clamped = Math.Clamp(value, MinBrushSize, MaxBrushSize);
                _mainWindowViewModel.PaintService.Settings.BrushSize = clamped;

                OnPropertyChanged();
            }
        }

        public void MouseWheelBrushSizeChanged(int delta)
        {
            int newSize = PaintBrushSize + delta;
            PaintBrushSize = newSize;

            _mainWindowViewModel.PaintService.Settings.BrushSize = PaintBrushSize;
        }

        private void LandformValuesChanged()
        {
            if (_assetManager == null)
                return;

            string hatchTextureId = (_assetManager).GetByName(AssetType.HatchTexture, "Random Hatch")[0].Id;
            string dashTextureId = (_assetManager).GetByName(AssetType.HatchTexture, "Watercolor Dashes")[0].Id;

            LandformShadingSettings shading = new()
            {
                UseTextureBackground = TextureFill,
                LandformBackgroundColor = LandformBackgroundColor.ToSKColor(),
                LandformOutlineColor = LandformOutlineColor.ToSKColor(),
                LandformTextureId = TextureBrowser.SelectedAssetId,
                LandformTextureScale = 1.0f,
                LandformTextureMirror = false,
                LandformOutlineWidth = LandformOutlineWidth,
                LandShadingDepth = LandformShadingDepth,
            };

            CoastlineSettings coastlineSettings = new()
            {
                CoastlineStyle = SelectedCoastlineStyle,
                EffectDistance = CoastlineEffectDistance,
                CoastlineColor = CoastlineColor.ToSKColor(),
                HatchTextureId = hatchTextureId,
                DashTextureId = dashTextureId,
            };

            _editor.UpdateSelectedLandform(shading, coastlineSettings);
        }

        // commands
        public ICommand SelectCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.ShapeSelect);
            _editor.ActivateTool(EditorToolType.SelectionTool);
        });

        public ICommand PaintCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.LandPaint);
            _editor.ActivateTool(EditorToolType.LandformTool, (ILandformSettings)this);

        });

        public ICommand EraseCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.LandErase);
            _editor.ActivateTool(EditorToolType.LandformTool, (ILandformSettings)this);
        });

        public ICommand FillLandformCommand => new RelayCommand(() =>
        {

        });

        public ICommand ClearLandformCommand => new RelayCommand(() =>
        {

        });

        public ICommand GenerateLandformsCommand => new RelayCommand(() =>
        {

        });

        // paint
        public ICommand PaintBrushCommand => new RelayCommand(() =>
        {
            _mainWindowViewModel.PaintService.Settings.BrushSize = PaintBrushSize;
            _mainWindowViewModel.PaintService.Settings.SelectedColor = PaintingColor;

            _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.LANDDRAWINGLAYER));
            _editor.SetDrawingMode(MapDrawingMode.DrawingPaint);
            _editor.ActivateTool(EditorToolType.PaintTool);
        });

        public ICommand ErasePaintCommand => new RelayCommand(() =>
        {
            _mainWindowViewModel.PaintService.Settings.BrushSize = PaintBrushSize;

            _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.LANDDRAWINGLAYER));
            _editor.SetDrawingMode(MapDrawingMode.LandErase);
            _editor.ActivateTool(EditorToolType.PaintTool);
        });

        public ICommand SelectPaletteColorCommand => new RelayCommand(SelectPaletteColor =>
        {
            if (SelectPaletteColor is SKColor color)
            {
                PaintingColor = color;
            }
        });

        // -------------------------------------------------
        // Merging/splitting landforms
        // -------------------------------------------------

        public static void MergeOverlappingLandforms(Landform active, Cmd_ModifyLandforms modifyCommand, MapLayer layer)
        {
            if (active == null
                || active.HitPath == null
                || active.HitPath.PointCount == 0
                || modifyCommand == null)
            {
                return;
            }

            SKPath unionPath = new(active.HitPath)
            {
                FillType = SKPathFillType.EvenOdd
            };

            var toMerge = new List<Landform>();

            foreach (var shape in layer.Shapes)
            {
                if (shape is not Landform existing)
                    continue;

                if (existing == active)
                    continue;

                if (!existing.Bounds.IntersectsWith(active.Bounds))
                    continue;

                using SKPath existingInput = new(existing.HitPath)
                {
                    FillType = SKPathFillType.EvenOdd
                };

                using SKPath activeInput = new(active.HitPath)
                {
                    FillType = SKPathFillType.EvenOdd
                };

                using SKPath intersection = new();

                bool intersectionSucceeded =
                    existing.HitPath.Op(
                        active.HitPath,
                        SKPathOp.Intersect,
                        intersection);

                bool overlaps;

                if (intersectionSucceeded)
                {
                    // A successful, empty intersection means there is no
                    // filled-area overlap.
                    overlaps = !intersection.IsEmpty;
                }
                else
                {
                    // PathOps failed, so use a raster-based overlap test.
                    overlaps = RasterPathsOverlap(
                        existing.HitPath,
                        active.HitPath);

                    Debug.WriteLine(
                        $"MergeOverlappingLandforms: intersection failed; " +
                        $"raster fallback reports overlap={overlaps}. " +
                        $"Existing points={existing.HitPath.PointCount}, " +
                        $"active points={active.HitPath.PointCount}.");
                }

                if (!overlaps)
                    continue;

                toMerge.Add(existing);
            }

            foreach (var existing in toMerge)
            {
                modifyCommand.CaptureBefore(existing);
                modifyCommand.RegisterRemovedLandform(existing);

                var newUnion = unionPath.Op(existing.HitPath, SKPathOp.Union);

                unionPath.Dispose();
                unionPath = new(newUnion)
                {
                    FillType = SKPathFillType.EvenOdd
                };
            }

            active.RestoreGeometry(unionPath);

            unionPath.Dispose();

            active.InvalidateRenderCache();
        }

        private static bool RasterPathsOverlap(
    SKPath firstPath,
    SKPath secondPath)
        {
            if (firstPath.IsEmpty || secondPath.IsEmpty)
                return false;

            SKRect firstBounds = firstPath.Bounds;
            SKRect secondBounds = secondPath.Bounds;

            float overlapLeft =
                Math.Max(firstBounds.Left, secondBounds.Left);

            float overlapTop =
                Math.Max(firstBounds.Top, secondBounds.Top);

            float overlapRight =
                Math.Min(firstBounds.Right, secondBounds.Right);

            float overlapBottom =
                Math.Min(firstBounds.Bottom, secondBounds.Bottom);

            if (overlapRight <= overlapLeft ||
                overlapBottom <= overlapTop)
            {
                return false;
            }

            int left = (int)Math.Floor(overlapLeft);
            int top = (int)Math.Floor(overlapTop);
            int right = (int)Math.Ceiling(overlapRight);
            int bottom = (int)Math.Ceiling(overlapBottom);

            int width = right - left;
            int height = bottom - top;

            if (width <= 0 || height <= 0)
                return false;

            using SKBitmap firstMask =
                RasterizePath(firstPath, left, top, width, height);

            using SKBitmap secondMask =
                RasterizePath(secondPath, left, top, width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (firstMask.GetPixel(x, y).Red > 0 &&
                        secondMask.GetPixel(x, y).Red > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static SKBitmap RasterizePath(
            SKPath path,
            int left,
            int top,
            int width,
            int height)
        {
            SKBitmap bitmap = new(
                new SKImageInfo(
                    width,
                    height,
                    SKColorType.Gray8,
                    SKAlphaType.Opaque));

            using (SKCanvas canvas = new(bitmap))
            using (SKPaint paint = new())
            {
                canvas.Clear(SKColors.Black);

                // Convert world coordinates to local mask coordinates.
                canvas.Translate(-left, -top);

                paint.Color = SKColors.White;
                paint.Style = SKPaintStyle.Fill;
                paint.IsAntialias = false;

                canvas.DrawPath(path, paint);
                canvas.Flush();
            }

            return bitmap;
        }

        public void ProcessPotentialSplit(Landform lf, Cmd_ModifyLandforms modifyCommand)
        {
            if (lf.HitPath.IsEmpty)
            {
                modifyCommand.RegisterRemovedLandform(lf);
                return;
            }

            var contours = RealmStudioShapeRenderingLib.Utilities.ExtractContours(lf.HitPath);

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
                modifyCommand.RegisterRemovedLandform(lf);
                return;
            }

            if (validContours.Count == 1)
            {
                lf.ReplaceGeometry(validContours[0]);
                return;
            }

            // TRUE SPLIT

            lf.EndInteractive();
            modifyCommand.RegisterRemovedLandform(lf);

            foreach (var contour in validContours)
            {
                var newLand = new Landform();
                newLand.CloneSettingsFrom(lf);

                newLand.ResolveAssets(_mainWindowViewModel.AssetManager);

                newLand.ReplaceGeometry(contour);
                newLand.EndInteractive();

                modifyCommand.RegisterNewLandform(newLand);

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
    }

    public interface ILandformSettings
    {
        int LandformBrushSize { get; }
        GeneratedLandformType SelectedLandformType { get; }
        LandformCoastlineStyle SelectedCoastlineStyle { get; }
        int CoastlineEffectDistance { get; }
        int LandformShadingDepth { get; }
        Color LandformOutlineColor { get; }
        int LandformOutlineWidth { get; }
        Color LandformBackgroundColor { get; }
        Color CoastlineColor { get; }
        bool TextureFill { get; }
        string? LandformTextureId { get; }
        int LandformEraserSize { get; }
    }
}
