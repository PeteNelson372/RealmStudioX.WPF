using RealmStudioShapeRenderingLib;
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
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class LandformPanelViewModel : ViewModelBase, IPaintToolViewModel, ILandformSettings
    {
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
            _mainWindowViewModel.PaintService.Settings.BrushSize = BrushSize;
            _mainWindowViewModel.PaintService.Settings.SelectedColor = PaintingColor;

            _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.LANDDRAWINGLAYER));
            _editor.SetDrawingMode(MapDrawingMode.DrawingPaint);
            _editor.ActivateTool(EditorToolType.PaintTool);
        });

        public ICommand ErasePaintCommand => new RelayCommand(() =>
        {
            _mainWindowViewModel.PaintService.Settings.BrushSize = BrushSize;

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
