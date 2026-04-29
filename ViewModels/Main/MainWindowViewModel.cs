using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Panels;
using SkiaSharp;
using System.Windows.Input;

namespace RealmStudioX.WPF.ViewModels.Main
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly EditorController _editor;

        public EditorController Editor
        {
            get { return _editor; }
        }

        private SKRect _viewPortSize = SKRect.Empty;

        public double ViewportPixelWidth => _viewPortSize.Width;
        public double ViewportPixelHeight => _viewPortSize.Height;

        public BackgroundPanelViewModel BackgroundPanel { get; }

        public LandformPanelViewModel LandformPanel { get; }

        public WaterPanelViewModel WaterPanel { get; }

        public MapPathViewModel PathPanel { get; }

        public MainWindowViewModel(EditorController editor, AssetManager assetManager)
        {
            _editor = editor;

            // instantiate ViewModels for the panels; when adding a view model
            // remember to add a reference to it on the TabItem <panel:...> in MainTabs.xaml 

            // Background Panel
            BackgroundPanel = new BackgroundPanelViewModel(_editor, assetManager);

            // Ocean Panel

            // Landform Panel
            LandformPanel = new LandformPanelViewModel(_editor, assetManager);

            // Water Body Panel
            WaterPanel = new WaterPanelViewModel(_editor, assetManager);

            // Path Panel
            PathPanel = new MapPathViewModel(_editor, assetManager);

            MapName = "Default";
        }

        // -------------------------
        // UI State
        // -------------------------

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
        // Commands (buttons)
        // -------------------------
        public ICommand ResetZoomCommand => new RelayCommand(() =>
        {
            _editor.Scene?.Camera?.Reset(_editor.Scene.Map.MapWidth, _editor.Scene.Map.MapHeight);
        });


        // -------------------------
        // Other Methods
        // -------------------------

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

            switch (_editor.SelectedColorPaintBrush)
            {
                case ColorPaintBrush.SoftBrush:
                    modeText += "Soft Brush";
                    break;
                case ColorPaintBrush.HardBrush:
                    modeText += "Hard Brush";
                    break;
                case ColorPaintBrush.PatternBrush1:
                    modeText += "Pattern 1";
                    break;
                case ColorPaintBrush.PatternBrush2:
                    modeText += "Pattern 2";
                    break;
                case ColorPaintBrush.PatternBrush3:
                    modeText += "Pattern 3";
                    break;
                case ColorPaintBrush.PatternBrush4:
                    modeText += "Pattern 4";
                    break;
                case ColorPaintBrush.None:
                    modeText += "None";
                    break;
                default:
                    modeText += "None";
                    break;
            }

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
    }
}
