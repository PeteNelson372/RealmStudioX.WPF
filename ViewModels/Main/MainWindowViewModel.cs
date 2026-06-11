using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Controls;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace RealmStudioX.WPF.ViewModels.Main
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly EditorController _editor;

        public EditorController Editor
        {
            get { return _editor; }
        }

        private readonly AssetManager _assetManager;

        public AssetManager AssetManager => _assetManager;

        private readonly FontManager _fontManager;

        private SKRect _viewPortSize = SKRect.Empty;

        public double ViewportPixelWidth => _viewPortSize.Width;
        public double ViewportPixelHeight => _viewPortSize.Height;

        public BackgroundPanelViewModel BackgroundViewModel { get; }

        public OceanPanelViewModel OceanViewModel { get; }

        public LandformPanelViewModel LandformViewModel { get; }

        public WaterPanelViewModel WaterViewModel { get; }

        public PathPanelViewModel PathViewModel { get; }

        public SymbolsPanelViewModel SymbolsViewModel {  get; }

        public LabelsPanelViewModel LabelsViewModel { get; }

        public OverlaysPanelViewModel OverlaysViewModel { get; }

        public FontSelectionViewModel FontPanelViewModel { get; }

        public MapScaleViewModel ScaleViewModel { get; }

        public RegionPanelViewModel RegionViewModel { get; }

        public DrawingPanelViewModel DrawingViewModel { get; }

        public event Action? RequestOpenNameGeneratorConfig;


        public MainWindowViewModel(EditorController editor, AssetManager assetManager, FontManager fontManager)
        {
            _editor = editor;
            _assetManager = assetManager;
            _fontManager = fontManager;

            // instantiate ViewModels for the panels; when adding a view model
            // remember to add a reference to it on the TabItem <panel:...> in MainTabs.xaml
            // and in MainWindow.xaml.cs ShowToolPanel() method

            // Background Panel
            BackgroundViewModel = new BackgroundPanelViewModel(_editor, assetManager);

            // Ocean Panel
            OceanViewModel = new OceanPanelViewModel(_editor, assetManager);

            // Landform Panel
            LandformViewModel = new LandformPanelViewModel(_editor, assetManager);

            // Water Body Panel
            WaterViewModel = new WaterPanelViewModel(_editor, assetManager);

            // Path Panel
            PathViewModel = new PathPanelViewModel(_editor, assetManager);

            // Symbols Panel
            SymbolsViewModel = new SymbolsPanelViewModel(_editor, assetManager);

            // Labels Panel
            LabelsViewModel = new LabelsPanelViewModel(this, _editor, assetManager);

            // Overlays Panel
            OverlaysViewModel = new OverlaysPanelViewModel(_editor, assetManager);

            // Font Panel (Font selection control)
            FontPanelViewModel = new FontSelectionViewModel(this, _fontManager);

            // Map Scale Control
            ScaleViewModel = new MapScaleViewModel(_editor);

            // Regions Panel
            RegionViewModel = new RegionPanelViewModel(_editor, assetManager);

            // Drawing Panel
            DrawingViewModel = new DrawingPanelViewModel(this, _editor, assetManager);

            MapName = "Default";
        }

        // -------------------------
        // UI State
        // -------------------------

        private RenderContext _renderContext;
        public RenderContext RenderContext
        {
            get => _renderContext;
            set => _renderContext = value;
        }

        private string _mapName = string.Empty;
        public string MapName
        {
            get => _mapName;
            set => SetProperty(ref _mapName, value);
        }

        private string _mapSizeLabel = string.Empty;
        public string MapSizeLabel
        {
            get => _mapSizeLabel;
            set => SetProperty(ref _mapSizeLabel, value);
        }

        private string _zoomLevelLabel = string.Empty;
        public string ZoomLevelLabel
        {
            get => _zoomLevelLabel;
            set => SetProperty(ref _zoomLevelLabel, value);
        }

        private string _drawingModeLabel = string.Empty;
        public string DrawingModeLabel
        {
            get => _drawingModeLabel;
            set => SetProperty(ref _drawingModeLabel, value);
        }

        private string _drawingLayerLabel = string.Empty;
        public string DrawingLayerLabel
        {
            get => _drawingLayerLabel;
            set => SetProperty(ref _drawingLayerLabel, value);
        }

        private string _drawingPointLabel = string.Empty;
        public string DrawingPointLabel
        {
            get => _drawingPointLabel;
            set => SetProperty(ref _drawingPointLabel, value);
        }

        private string _cursorPointLabel = string.Empty;
        public string CursorPointLabel
        {
            get => _cursorPointLabel;
            set => SetProperty(ref _cursorPointLabel, value);
        }

        public double MaxScrollX =>
            _editor.Scene?.Map == null ? 0: _editor.Scene.Map.MapWidth * Zoom;

        public double MaxScrollY =>
            _editor.Scene?.Map == null ? 0: _editor.Scene.Map.MapHeight * Zoom;

        public double Zoom
        {
            get => _editor.Scene?.Camera.Zoom ?? 1.0;
            set
            {
                if (_editor.Scene == null || _editor.Scene.Camera == null)
                    return;

                var camera = _editor.Scene.Camera;

                // clamp to 10% to 800%
                value = Math.Clamp(value, 0.1, 8.0);

                if (Math.Abs(camera.Zoom - value) < 0.0001)
                    return;

                camera.SetZoom((float)value, _editor.Scene.Map.MapWidth, _editor.Scene.Map.MapHeight);

                // Notify UI that Zoom changed
                OnPropertyChanged(nameof(Zoom));
                OnPropertyChanged(nameof(MaxScrollX));
                OnPropertyChanged(nameof(MaxScrollY));

                UpdateZoomLabel(camera.Zoom);
            }
        }

        public void UpdateZoomLabel(double zoom)
        {
            ZoomLevelLabel = $"Zoom: {(int)(zoom * 100)}%";
        }

        // -------------------------
        // Commands (menu and buttons)
        // -------------------------

        public ICommand NewOpenCommand => new RelayCommand(() =>
        {
            var dialog = new CreateOpenMapDialog();
            var result = dialog.ShowDialog();

            if (result != true || dialog.ViewModel.Result == null)
            {
                return;
            }

            if ((bool)result)
            {
                CreateOpenMapResult dlgResult = dialog.ViewModel.Result;

                CreateOrOpenMap(dlgResult);
            }
        });

        public ICommand SaveCommand => new RelayCommand(() =>
        {
            string mapPath = Path.Join(AssetManager.RootRealmsDirectory, MapName + ".rsmx");

            MessageBox.Show(mapPath, "Save", MessageBoxButton.OK, MessageBoxImage.Information);

            _editor.Scene!.Map.MapPath = mapPath;

            MapFileMethods.SaveMap(_editor.Scene.Map);
        });

        public ICommand ExportCommand => new RelayCommand(() =>
        {
            MessageBox.Show("Export Map", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        });

        public ICommand PrintCommand => new RelayCommand(() =>
        {
            MessageBox.Show("Print Map", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
        });

        public ICommand ResetZoomCommand => new RelayCommand(() =>
        {
            _editor.Scene?.Camera?.Reset(_editor.Scene.Map.MapWidth, _editor.Scene.Map.MapHeight);
        });


        public ICommand OpenNameGeneratorConfigCommand => new RelayCommand(() =>
        {
            RequestOpenNameGeneratorConfig?.Invoke();
        });

        public ICommand ExitCommand => new RelayCommand(() =>
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to exit?", "Confirm Exit", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        });

        public ICommand UndoCommand => new RelayCommand(() =>
        {
            _editor.Commands.Undo();
        });

        public ICommand RedoCommand => new RelayCommand(() =>
        {
            _editor.Commands.Redo();
        });


        // -------------------------
        // Other Methods
        // -------------------------

        public void OnDrawingModeChanged(MapDrawingMode mode)
        {
            DrawingModeLabel = SetDrawingModeLabel();
        }

        public void CreateOrOpenMap(CreateOpenMapResult result)
        {
            RealmStudioMap? map = null;

            if (result.IsNew)
            {
                // Create new map
                if (string.IsNullOrEmpty(result.MapName))
                {
                    result.MapName = "Default";
                }

                if (string.IsNullOrEmpty(result.FilePath))
                {
                    result.FilePath = string.Empty;
                }

                map = MapBuilder.CreateMap(result.FilePath, result.MapName, result.Width, result.Height, result.MapAreaWidth, result.MapAreaHeight, result.MapAreaUnits);

                InitializeScene(map);
            }
            else
            {
                // Load existing map
                
                if (!string.IsNullOrEmpty(result.FilePath))
                {
                    try
                    {
                        map = MapFileMethods.OpenMap(result.FilePath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                        map = null;
                    }

                    if (map != null)
                    {

                    }
                    else
                    {
                        MessageBox.Show("Could not open selected map.", "Error Opening Map", MessageBoxButton.OK);
                    }
                }
            }

            if (map != null)
            {                
                InitializeScene(map);

                MapName = map.MapName;
                MapSizeLabel = $"Map Size: {map.MapWidth} x {map.MapHeight}, Map Area: {map.MapAreaWidth} x {map.MapAreaHeight} {map.MapAreaUnits}";

                ScaleViewModel.UnitLabel = map.MapAreaUnits;
                ScaleViewModel.FontStyle = new FontStyleModel
                {
                    Family = "Segoe UI",
                    Size = 14,
                };

                OnDrawingModeChanged(MapDrawingMode.None);

                _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.DRAWINGLAYER));

                FinalizeMapLoad(map);
            }
        }

        public void FinalizeMapLoad(RealmStudioMap map)
        {
            PlacedMapFrame? pmf = null;
            MapGrid? grid = null;

            // go through the map and load textures and bitmaps, etc.
            // load shape assets
            AssetInitializer.InitializeMapShapeAssets(map, _assetManager, _fontManager);

            // finalize the geometry of the shapes
            foreach (MapLayer layer in map.MapLayers)
            {
                foreach (MapComponent2D shape  in layer.Shapes)
                {
                    shape.FinalizeShapeGeometry(map);

                    // special handling to set frame
                    if (shape is PlacedMapFrame frame && frame.FrameDefinition != null)
                    {
                        pmf = frame;
                    }

                    // special handling to set grid
                    if (shape is MapGrid mg)
                    {
                        grid = mg;
                    }

                    layer.RebuildIndexes();
                }
            }

            foreach (WaterSystem ws in map.WaterSystems)
            {
                ws.FinalizeWaterSystem(map);
            }

            // background
            if (!string.IsNullOrEmpty(map.Background.TextureId))
            {
                TextureFillRequest fillRequest = new()
                {
                    TextureId = map.Background.TextureId,
                    Scale = (float)map.Background.Scale,
                    Mirror = map.Background.Mirror,
                };

                _editor.FillBackground(fillRequest);
            }

            // ocean texture
            if (!string.IsNullOrEmpty(map.Ocean.TextureId))
            {
                TextureFillRequest applyTextureRequest = new()
                {
                    TextureId = map.Ocean.TextureId,
                    Scale = (float)map.Ocean.Scale,
                    Opacity = map.Ocean.TextureOpacity,
                    Mirror = map.Ocean.Mirror,
                    Color = map.Ocean.OverlayColor,
                };

                _editor.ApplyOceanTexture(applyTextureRequest);
            }

            // set the frame
            if (pmf != null && pmf.FrameDefinition != null)
            {
                _editor.SetFrame(pmf.FrameDefinition, pmf.FrameTint, pmf.FrameScale);
            }

            // set the grid (the view model has to be set with the grid values)
            if (grid != null)
            {
                OverlaysViewModel.GridColor = grid.GridColor.ToColor();
                OverlaysViewModel.GridEnabled = true;
                OverlaysViewModel.GridLineWidth = grid.GridLineWidth;
                OverlaysViewModel.GridLayer = grid.GridLayerIndex;
                OverlaysViewModel.GridSize = grid.GridSize;
                OverlaysViewModel.GridType = grid.GridType;
                OverlaysViewModel.ShowGridSize = grid.ShowGridSize;
            }


            // ensure tiles and spatial grid are updated
            foreach (MapLayer layer in map.MapLayers)
            {

                layer.InvalidateAllTiles();
            }

        }

        public void InitializeScene(RealmStudioMap map)
        {
            MapScene newScene = new(map, _fontManager)
            {
                RenderContext = _renderContext
            };

            newScene.Camera.Viewport = new SKRect(0, 0, map.MapWidth, map.MapHeight);
            _editor.SetScene(newScene);

            AttachScene(newScene);
        }

        public void AttachScene(MapScene scene)
        {
            // Unhook old if needed (optional for now)

            var camera = scene.Camera;

            camera.ViewChanged += OnCameraChanged;

            // Sync immediately
            OnCameraChanged();
        }

        public void SetViewPortSize(SKRect rect)
        {
            _viewPortSize = rect;

            OnPropertyChanged(nameof(ViewportPixelWidth));
            OnPropertyChanged(nameof(ViewportPixelHeight));
            OnPropertyChanged(nameof(MaxScrollX));
            OnPropertyChanged(nameof(MaxScrollY));

        }

        private void OnCameraChanged()
        {
            OnPropertyChanged(nameof(ScrollX));
            OnPropertyChanged(nameof(ScrollY));
            OnPropertyChanged(nameof(MaxScrollX));
            OnPropertyChanged(nameof(MaxScrollY));
            OnPropertyChanged(nameof(ViewportPixelWidth));
            OnPropertyChanged(nameof(ViewportPixelHeight));
        }

        public string SetDrawingModeLabel()
        {
            string modeText = "Drawing Mode: ";

            modeText += _editor.CurrentDrawingMode switch
            {
                MapDrawingMode.None => "None",
                MapDrawingMode.LandPaint => "Paint Landform",
                MapDrawingMode.LandErase => "Erase Landform",
                MapDrawingMode.LandColorErase => "Erase Landform Color",
                MapDrawingMode.LandColor => "Color Landform",
                MapDrawingMode.OceanErase => "Erase Ocean",
                MapDrawingMode.OceanPaint => "Paint Ocean",
                MapDrawingMode.ColorSelect => "Select Color",
                MapDrawingMode.LandformSelect => "Select Landform",
                MapDrawingMode.LandformHeightMapSelect => "Select Landform",
                MapDrawingMode.WaterPaint => "Paint Water Feature",
                MapDrawingMode.WaterErase => "Erase Water Feature",
                MapDrawingMode.WaterColor => "Color Water Feature",
                MapDrawingMode.WaterColorErase => "Erase Water Feature Color",
                MapDrawingMode.LakePaint => "Paint Lake",
                MapDrawingMode.RiverPaint => "Paint River",
                MapDrawingMode.RiverEdit => "Edit River",
                MapDrawingMode.WaterFeatureSelect => "Select Water Feature",
                MapDrawingMode.PathPaint => "Draw Path",
                MapDrawingMode.PathSelect => "Select Path",
                MapDrawingMode.PathEdit => "Edit Path",
                MapDrawingMode.SymbolErase => "Erase Symbol",
                MapDrawingMode.SymbolPlace => "Place Symbol",
                MapDrawingMode.SymbolSelect => "Select Symbol",
                MapDrawingMode.SymbolColor => "Color Symbol",
                MapDrawingMode.DrawBezierLabelPath => "Draw Curve Label Path",
                MapDrawingMode.DrawArcLabelPath => "Draw Arc Label Path",
                MapDrawingMode.DrawLabel => "Place Label",
                MapDrawingMode.LabelSelect => "Select Label",
                MapDrawingMode.DrawBox => "Draw Box",
                MapDrawingMode.PlaceWindrose => "Place Windrose",
                MapDrawingMode.SelectMapScale => "Move Map Scale",
                MapDrawingMode.DrawMapMeasure => "Draw Map Measure",
                MapDrawingMode.RegionPaint => "Draw Region",
                MapDrawingMode.RegionSelect => "Select Region",
                MapDrawingMode.RealmAreaSelect => "Select Area",
                MapDrawingMode.HeightMapPaint => "Paint Height Map",
                MapDrawingMode.MapHeightIncrease => "Increase Map Height",
                MapDrawingMode.MapHeightDecrease => "Decrease Map Height",
                MapDrawingMode.DrawingLine => "Draw Line",
                MapDrawingMode.DrawingErase => "Erase",
                MapDrawingMode.DrawingPaint => "Paint",
                MapDrawingMode.DrawingRectangle => "Draw Rectangle",
                MapDrawingMode.DrawingEllipse => "Draw Ellipse",
                MapDrawingMode.DrawingPolygon => "Draw Polygon",
                MapDrawingMode.DrawingStamp => "Stamp",
                MapDrawingMode.DrawingDiamond => "Draw Diamond",
                MapDrawingMode.DrawingRoundedRectangle => "Draw Rounded Rectangle",
                MapDrawingMode.DrawingTriangle => "Draw Triangle",
                MapDrawingMode.DrawingRightTriangle => "Draw Right Triangle",
                MapDrawingMode.DrawingHexagon => "Draw Hexagon",
                MapDrawingMode.DrawingPentagon => "Draw Pentagon",
                MapDrawingMode.DrawingArrow => "Draw Arrow",
                MapDrawingMode.DrawingFivePointStar => "Draw 5-Point Star",
                MapDrawingMode.DrawingSixPointStar => "Draw 6-Point Star",
                MapDrawingMode.DrawingSelect => "Select Drawn Object",
                MapDrawingMode.InteriorFloorPaint => "Paint Interior Floor",
                MapDrawingMode.ShapeSelect => "Select Any Shape",
                _ => "Undefined",
            };

            modeText += ". Selected Brush: ";



            return modeText;
        }

        internal void SetDrawingLayerLabel()
        {
            if (_editor.ActiveDrawingLayer != null)
            {
                DrawingLayerLabel = _editor.ActiveDrawingLayer.MapLayerName.ToString().ToUpperInvariant();
            }
            else
            {
                DrawingLayerLabel = "NONE";
            }
        }

        public double ScrollX
        {
            get => -_editor.Scene?.Camera.Pan.X ?? 0;
            set
            {
                var cam = _editor.Scene?.Camera;

                if (cam == null)
                    return;

                var clamped = Math.Clamp(value, 0, MaxScrollX);

                cam.SetPan(new SKPoint(-(float)clamped, cam.Pan.Y),
                           _viewPortSize.Width, _viewPortSize.Height);

                OnPropertyChanged();
            }
        }

        public double ScrollY
        {
            get => -_editor.Scene?.Camera.Pan.Y ?? 0;

            set
            {
                var cam = _editor.Scene?.Camera;

                if (cam == null)
                    return;

                var clamped = Math.Clamp(value, 0, MaxScrollY);

                cam.SetPan(
                    new SKPoint(cam.Pan.X, -(float)clamped),
                    _viewPortSize.Width, _viewPortSize.Height);

                OnPropertyChanged();
            }
        }

        public static ImageSource ToImageSource(SKBitmap bitmap)
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(data.ToArray());

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = stream;
            bmp.EndInit();
            bmp.Freeze(); // IMPORTANT for performance

            return bmp;
        }
    }
}
