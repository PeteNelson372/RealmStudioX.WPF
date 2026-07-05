using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Controls;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.ViewModels.Painting;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection.Metadata;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class OceanPanelViewModel : ViewModelBase, IWindroseSettings
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        public MainWindowViewModel MainViewModel => _mainWindowViewModel;

        private EditorController _editor;
        private AssetManager _assetManager;

        public Action<TextureFillRequest>? FillRequested;
        public Action? ClearRequested;

        public AssetBrowserViewModel TextureBrowser { get; }

        public ColorPalette? OceanPalette { get; }

        public OceanPanelViewModel(MainWindowViewModel mainViewModel, EditorController editor, AssetManager assetManager)
        {
            _mainWindowViewModel = mainViewModel;
            _editor = editor;
            _assetManager = assetManager;
            var browser = new AssetBrowser(_assetManager, AssetType.WaterTexture);
            TextureBrowser = new AssetBrowserViewModel(browser);

            var paletteBrowser = new AssetBrowser(_assetManager, AssetType.ColorPalette);

            IReadOnlyList<AssetDescriptor> paletteDescriptors = paletteBrowser.GetAssets();

            for (int i = 0; i < paletteDescriptors.Count; i++)
            {
                AssetDescriptor descriptor = paletteDescriptors[i];
                if (descriptor.Type == AssetType.ColorPalette)
                {
                    string xml = File.ReadAllText(descriptor.FilePath);
                    ColorPalette palette = MapFileMethods.DeserializeObject<ColorPalette>(xml);
                    if (palette != null && palette.PaletteType == ColorPaletteType.OceanColors)
                    {
                        OceanPalette = palette;
                        break;
                    }
                }
            }
        }

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

        private void SelectPaletteColor(SKColor color)
        {
            PaintingColor = color;
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

        // texture

        public ICommand ApplyTextureCommand => new RelayCommand(() =>
        {
            TextureFillRequest applyTextureRequest = new()
            {
                TextureId = TextureBrowser.SelectedAssetId,
                Scale = (float)TextureScale,
                Opacity = TextureOpacity,
                Mirror = MirrorTexture,
            };

            _editor.ApplyOceanTexture(applyTextureRequest);
        });

        public ICommand ClearTextureCommand => new RelayCommand(() =>
        {
            _editor.ClearOceanTexture();
        });


        // paint
        public ICommand PaintCommand => new RelayCommand(() =>
        {
            _mainWindowViewModel.PaintService.Settings.BrushSize = BrushSize;
            _mainWindowViewModel.PaintService.Settings.SelectedColor = PaintingColor;

            _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.OCEANDRAWINGLAYER));
            _editor.SetDrawingMode(MapDrawingMode.DrawingPaint);
            _editor.ActivateTool(EditorToolType.PaintTool);
        });

        public ICommand ErasePaintCommand => new RelayCommand(() =>
        {
            _mainWindowViewModel.PaintService.Settings.BrushSize = BrushSize;

            _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.OCEANDRAWINGLAYER));
            _editor.SetDrawingMode(MapDrawingMode.OceanErase);
            _editor.ActivateTool(EditorToolType.PaintTool);
        });

        // color

        public ICommand FillColorCommand => new RelayCommand(() =>
        {
            TextureFillRequest fillColorRequest = new()
            {
                Color = OceanColor.ToSKColor(),
            };

            _editor.FillOceanColor(fillColorRequest);
        });

        public ICommand ClearOceanColorCommand => new RelayCommand(() =>
        {
            _editor.ClearOceanColor();
        });

        public ICommand SelectPaletteColorCommand => new RelayCommand(SelectPaletteColor =>
        {
            if (SelectPaletteColor is SKColor color)
            {
                PaintingColor = color;
            }
        });

        private void PreviewChanged()
        {
            if (_assetManager == null)
                return;

            TextureFillRequest updateRequest = new()
            {
                TextureId = TextureBrowser.SelectedAssetId,
                Scale = (float)TextureScale,
                Opacity = TextureOpacity,
                Mirror = MirrorTexture,
                Color = OceanColor.ToSKColor(),
            };

            _editor.UpdateOceanPreview(updateRequest);
        }

        public float MinOpacity { get; } = 0f;
        public float MaxOpacity { get; } = 1f;

        private float _textureOpacity = 1.0f;
        public float TextureOpacity
        {
            get => _textureOpacity;
            set
            {
                var clamped = Math.Clamp(value, MinOpacity, MaxOpacity);

                if (SetProperty(ref _textureOpacity, clamped))
                {
                    PreviewChanged();
                }
            }
        }

        public float MinScale { get; } = 0f;
        public float MaxScale { get; } = 2f;

        private float _textureScale = 1.0f;
        public float TextureScale
        {
            get => _textureScale;
            set
            {
                var clamped = Math.Clamp(value, MinScale, MaxScale);

                if (SetProperty(ref _textureScale, clamped))
                {
                    PreviewChanged();
                }
            }
        }

        private bool _mirrorTexture = false;
        public bool MirrorTexture
        {
            get => _mirrorTexture;
            set
            {
                if (SetProperty(ref _mirrorTexture, value))
                {
                    PreviewChanged();
                }
            }
        }

        // ocean color

        private Color _oceanColor = Colors.White;
        public Color OceanColor
        {
            get => _oceanColor;
            set
            {
                if (SetProperty(ref _oceanColor, value))
                {
                    _oceanColorBrush.Color = value;
                }
            }
        }

        private SolidColorBrush _oceanColorBrush = new(Colors.White);

        public Brush OceanColorBrush => _oceanColorBrush;


        // WINDROSE

        // windrose color

        private Color _windroseColor = Color.FromArgb(127, 61, 55, 40);
        public Color WindroseColor
        {
            get => _windroseColor;
            set
            {
                if (SetProperty(ref _windroseColor, value))
                {
                    _windroseColorBrush.Color = value;
                }
            }
        }

        private SolidColorBrush _windroseColorBrush = new(Color.FromArgb(127, 61, 55, 40));

        public Brush WindroseColorBrush => _windroseColorBrush;

        // windrose directions

        public int MinDirections { get; } = 4;
        public int MaxDirections { get; } = 32;

        private int _windroseDirections = 16;
        public int WindroseDirections
        {
            get => _windroseDirections;
            set
            {
                var clamped = Math.Clamp(value, MinDirections, MaxDirections);

                SetProperty(ref _windroseDirections, clamped);
            }
        }

        // windrose line width

        public int MinLineWidth { get; } = 1;
        public int MaxLineWidth { get; } = 16;

        private int _windroseLineWidth = 2;
        public int WindroseLineWidth
        {
            get => _windroseLineWidth;
            set
            {
                var clamped = Math.Clamp(value, MinLineWidth, MaxLineWidth);

                SetProperty(ref _windroseLineWidth, clamped);
            }
        }

        // windrose inner radius

        public int MinInnerRadius { get; } = 0;
        public int MaxInnerRadius { get; } = 1024;

        private int _windroseInnerRadius = 0;
        public int WindroseInnerRadius
        {
            get => _windroseInnerRadius;
            set
            {
                var clamped = Math.Clamp(value, MinInnerRadius, MaxInnerRadius);
                SetProperty(ref _windroseInnerRadius, clamped);
            }
        }

        // windrose outer radius

        public int MinOuterRadius { get; } = 100;
        public int MaxOuterRadius { get; } = 20000;

        private int _windroseOuterRadius = 1000;
        public int WindroseOuterRadius
        {
            get => _windroseOuterRadius;
            set
            {
                var clamped = Math.Clamp(value, MinOuterRadius, MaxOuterRadius);
                SetProperty(ref _windroseOuterRadius, clamped);
            }
        }

        // windrose circles

        public int MinCircles { get; } = 0;
        public int MaxCircles { get; } = 2;

        private int _windroseCircles = 0;
        public int WindroseCircles
        {
            get => _windroseCircles;
            set
            {
                var clamped = Math.Clamp(value, MinCircles, MaxCircles);
                SetProperty(ref _windroseCircles, clamped);
            }
        }

        // windrose direction lines fade

        private bool _windroseFade= false;
        public bool WindroseFade
        {
            get => _windroseFade;
            set
            {
                SetProperty(ref _windroseFade, value);
            }
        }

        public ICommand PlaceWindroseCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.PlaceWindrose);
            _editor.ActivateTool(EditorToolType.WindroseTool, (IWindroseSettings)this);
        });

        public ICommand ClearWindroseCommand => new RelayCommand(() =>
        {
            _editor.ClearWindroses();
        });
    }

    public interface IWindroseSettings
    {
        Color WindroseColor { get; }
        int WindroseDirections { get; }
        int WindroseLineWidth {  get; }
        int WindroseInnerRadius { get; }
        int WindroseOuterRadius { get; }
        int WindroseCircles { get; }
        bool WindroseFade { get; }

    }
}
