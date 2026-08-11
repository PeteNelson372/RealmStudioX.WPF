using RealmStudioShapeRenderingLib;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.ViewModels.Painting;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class WaterPanelViewModel : ViewModelBase, IPaintToolViewModel, IWaterBodySettings
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        public MainWindowViewModel MainViewModel => _mainWindowViewModel;

        private readonly EditorController _editor;
        private readonly AssetManager _assetManager;

        public ColorPalette? PaintPalette { get; }

        public WaterPanelViewModel(MainWindowViewModel mainViewModel, EditorController editor, AssetManager assetManager)
        {
            _mainWindowViewModel = mainViewModel;
            _editor = editor;
            _assetManager = assetManager;

            var paletteBrowser = new AssetBrowser(_assetManager, AssetType.ColorPalette);

            IReadOnlyList<AssetDescriptor> paletteDescriptors = paletteBrowser.Assets;

            for (int i = 0; i < paletteDescriptors.Count; i++)
            {
                AssetDescriptor descriptor = paletteDescriptors[i];
                if (descriptor.Type == AssetType.ColorPalette)
                {
                    string xml = File.ReadAllText(descriptor.FilePath);
                    ColorPalette palette = MapFileMethods.DeserializeObject<ColorPalette>(xml);
                    if (palette != null && palette.PaletteType == ColorPaletteType.WaterFeatureColors)
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

        public int MinWaterBrushSize { get; } = 4;
        public int MaxWaterBrushSize { get; } = 256;

        private int _waterBrushSize = 20;
        public int WaterBrushSize
        {
            get => _waterBrushSize;
            set
            {
                var clamped = Math.Clamp(value, MinWaterBrushSize, MaxWaterBrushSize);

                _waterBrushSize = clamped;
                OnPropertyChanged();
            }
        }

        public int MinWaterEraserSize { get; } = 4;
        public int MaxWaterEraserSize { get; } = 256;

        private int _waterEraserSize = 20;
        public int WaterEraserSize
        {
            get => _waterEraserSize;
            set
            {
                var clamped = Math.Clamp(value, MinWaterEraserSize, MaxWaterEraserSize);

                _waterEraserSize = clamped;
                OnPropertyChanged();
            }
        }

        // shallow water 

        private Color _shallowWaterColor = Color.FromArgb(168, 140, 191, 197);
        public Color ShallowWaterColor
        {
            get => _shallowWaterColor;
            set
            {
                if (SetProperty(ref _shallowWaterColor, value))
                {
                    _shallowWaterColorBrush.Color = value;
                }
            }
        }

        private SolidColorBrush _shallowWaterColorBrush = new(Color.FromArgb(168, 140, 191, 197));

        public Brush ShallowWaterColorBrush => _shallowWaterColorBrush;

        // deep water

        private Color _deepWaterColor = Color.FromArgb(168, 140, 191, 197);
        public Color DeepWaterColor
        {
            get => _deepWaterColor;
            set
            {
                if (SetProperty(ref _deepWaterColor, value))
                {
                    _deepWaterColorBrush.Color = value;
                }
            }
        }

        private SolidColorBrush _deepWaterColorBrush = new(Color.FromArgb(168, 140, 191, 197));

        public Brush DeepWaterColorBrush => _deepWaterColorBrush;

        // shoreline color

        private Color _shorelineColor = Colors.Tan;
        public Color ShorelineColor
        {
            get => _shorelineColor;
            set
            {
                if (SetProperty(ref _shorelineColor, value))
                {
                    _shorelineColorBrush.Color = value;
                }
            }
        }

        private SolidColorBrush _shorelineColorBrush = new(Colors.Tan);

        public Brush ShorelineColorBrush => _shorelineColorBrush;


        // river width

        private int _riverWidth = 16;
        public int RiverWidth
        {
            get => _riverWidth;
            set => SetProperty(ref _riverWidth, value);
        }

        // meander strength

        private float _meanderStrength = 1.6f;
        public float MeanderStrength
        {
            get => _meanderStrength;
            set => SetProperty(ref _meanderStrength, value);
        }

        // source fade-in

        private bool _sourceFadeIn = true;
        public bool SourceFadeIn
        {
            get => _sourceFadeIn;
            set => SetProperty(ref _sourceFadeIn, value);
        }

        // edit river points
        private bool _editRiverPoints = false;
        public bool EditRiverPoints
        {
            get => _editRiverPoints;
            set => SetProperty(ref _editRiverPoints, value);
        }

        // commands
        public ICommand SelectCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.ShapeSelect);
            _editor.ActivateTool(EditorToolType.SelectionTool);
        });

        public ICommand WaterPaintCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.WaterPaint);
            _editor.ActivateTool(EditorToolType.WaterBodyTool, (IWaterBodySettings)this);
        });

        public ICommand CreateLakeCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.LakePaint);
            _editor.ActivateTool(EditorToolType.WaterBodyTool, (IWaterBodySettings)this);
        });

        public ICommand DrawRiverCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.RiverPaint);
            _editor.ActivateTool(EditorToolType.WaterBodyTool, (IWaterBodySettings)this);
        });

        public ICommand EraseCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.WaterErase);
            _editor.ActivateTool(EditorToolType.WaterBodyTool, (IWaterBodySettings)this);
        });

        public ICommand EditRiverCommand => new RelayCommand(() =>
        {
            if (_editor != null && _editor.SelectionService!.PrimarySelection != null && _editor.SelectionService!.PrimarySelection.ReferencedShape is River r)
            {
                r.Editor.IsEditing = EditRiverPoints;

                if (r.Editor.IsEditing)
                {
                    r.WaterSystem?.BeginInteractive();
                    r.BeginInteractive();
                }
                else
                {
                    r.WaterSystem?.EndInteractive();
                    r.EndInteractive();

                    r.Editor.OnChanged!();
                }
            }
        });

        // paint

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
                },
        usesParameter: true);

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

        public int BrushSize
        {
            get => _mainWindowViewModel.PaintService.Settings.BrushSize;
            set
            {
                var clamped = Math.Clamp(value, MinBrushSize, MaxBrushSize);
                OnPropertyChanged();

                _mainWindowViewModel.PaintService.Settings.BrushSize = clamped;
            }
        }

        public void MouseWheelBrushSizeChanged(int delta)
        {
            int newSize = BrushSize + delta;
            BrushSize = newSize;

            _mainWindowViewModel.PaintService.Settings.BrushSize = BrushSize;
        }


        public ICommand PaintBrushCommand => new RelayCommand(() =>
        {
            _mainWindowViewModel.PaintService.Settings.BrushSize = BrushSize;
            _mainWindowViewModel.PaintService.Settings.SelectedColor = PaintingColor;

            _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.WATERDRAWINGLAYER));
            _editor.SetDrawingMode(MapDrawingMode.DrawingPaint);
            _editor.ActivateTool(EditorToolType.PaintTool);
        });

        public ICommand ErasePaintCommand => new RelayCommand(() =>
        {
            _mainWindowViewModel.PaintService.Settings.BrushSize = BrushSize;

            _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.WATERDRAWINGLAYER));
            _editor.SetDrawingMode(MapDrawingMode.WaterColorErase);
            _editor.ActivateTool(EditorToolType.PaintTool);
        });

        public ICommand SelectPaletteColorCommand => new RelayCommand(SelectPaletteColor =>
        {
            if (SelectPaletteColor is SKColor color)
            {
                PaintingColor = color;
            }
        });
    }

    public interface IWaterBodySettings
    {
        int WaterBrushSize { get; }
        int WaterEraserSize { get; }
        Color ShallowWaterColor { get; }
        Color DeepWaterColor { get; }
        Color ShorelineColor { get; }
        int RiverWidth { get; }
        float MeanderStrength { get; }
        bool SourceFadeIn { get; }
        bool EditRiverPoints { get; }

    }
}
