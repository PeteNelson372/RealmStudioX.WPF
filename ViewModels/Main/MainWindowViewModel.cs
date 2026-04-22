using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using System.Windows.Input;

namespace RealmStudioX.WPF.ViewModels.Main
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly EditorController _editor;

        public MainWindowViewModel(EditorController editor)
        {
            _editor = editor;

            SelectLandformToolCommand = new RelayCommand(SelectLandformTool);
            SelectBackgroundToolCommand = new RelayCommand(SelectBackgroundTool);

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

        private string _applicationStatusMessage = string.Empty;
        public string ApplicationStatusMessage
        {
            get => _applicationStatusMessage;
            set => SetProperty(ref _applicationStatusMessage, value);
        }

        // -------------------------
        // Commands (buttons)
        // -------------------------

        public ICommand SelectLandformToolCommand { get; }
        public ICommand SelectBackgroundToolCommand { get; }

        private void SelectLandformTool()
        {
            //_editor.SetTool(new PaintLandformTool());
        }

        private void SelectBackgroundTool()
        {
            //_editor.SetTool(new EraseTool());
        }

        // -------------------------
        // Other Methods
        // -------------------------
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
    }
}
