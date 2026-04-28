using RealmStudioShapeRenderingLib;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Controls;
using RealmStudioX.WPF.ViewModels.Infrastructure;
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
            set => SetProperty(ref _selectedCoastlineStyle, value);
        }

        private int _coastlineEffectDistance = 120;
        public int CoastlineEffectDistance
        {
            get => _coastlineEffectDistance;
            set => SetProperty(ref _coastlineEffectDistance, value);
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
            set => SetProperty(ref _landformShadingDepth, value);
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
                }
            }
        }

        private SolidColorBrush _landformOutlineBrush = new(Color.FromArgb(255, 65, 55, 40));

        public Brush LandformOutlineBrush => _landformOutlineBrush;

        public int _landformOutlineWidth = 1;

        public int LandformOutlineWidth
        {
            get => _landformOutlineWidth;
            set => SetProperty(ref _landformOutlineWidth, value);
        }

        private Color _landformBackgroundColor = Color.FromArgb(255, 65, 55, 40);
        public Color LandformBackgroundColor
        {
            get => _landformBackgroundColor;
            set
            {
                if (SetProperty(ref _landformBackgroundColor, value))
                {
                    _landformBackgroundBrush.Color = value;
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
                }
            }
        }

        private SolidColorBrush _coastlineColorBrush = new(Color.FromArgb(187, 156, 195, 183));

        public Brush CoastlineColorBrush => _coastlineColorBrush;

        private bool _textureFill = true;
        public bool TextureFill
        {
            get => _textureFill;
            set => SetProperty(ref _textureFill, value);
        }

        public string? LandformTextureId => TextureBrowser.SelectedAssetId;


        public int _landformEraserSize = 64;

        public int LandformEraserSize
        {
            get => _landformEraserSize;
            set => SetProperty(ref _landformEraserSize, value);
        }

        public AssetBrowserViewModel TextureBrowser { get; }

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
