using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Controls;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using SkiaSharp.Views.WPF;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class LandformPanelViewModel : ViewModelBase, ILandformSettings
    {
        private readonly EditorController _editor;
        private readonly AssetManager _assetManager;

        public LandformPanelViewModel(EditorController editor, AssetManager assetManager)
        {
            _editor = editor;
            _assetManager = assetManager;
            var browser = new AssetBrowser(_assetManager, AssetType.LandTexture);
            TextureBrowser = new AssetBrowserViewModel(browser);

            TextureBrowser.TextureSelectionChanged += LandformValuesChanged;
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

        private int _landformBrushSize = 64;
        public int LandformBrushSize
        {
            get => _landformBrushSize;
            set => SetProperty(ref _landformBrushSize, value);
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


        public int _landformEraserSize = 64;

        public int LandformEraserSize
        {
            get => _landformEraserSize;
            set => SetProperty(ref _landformEraserSize, value);
        }

        public AssetBrowserViewModel TextureBrowser { get; }

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
