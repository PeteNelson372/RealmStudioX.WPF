using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
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
    public class BackgroundPanelViewModel : ViewModelBase
    {
        private AssetManager _assetManager;

        public Action<TextureFillRequest>? FillRequested;
        public Action? ClearRequested;

        public Action<TextureFillRequest>? PreviewChanged;

        public AssetBrowserViewModel TextureBrowser { get; }

        public BackgroundPanelViewModel(EditorController editor, AssetManager assetManager)
        {
            _assetManager = assetManager;
            var browser = new AssetBrowser(assetManager, AssetType.BackgroundTexture);
            TextureBrowser = new AssetBrowserViewModel(browser);
        }

        public ICommand FillCommand => new RelayCommand(() =>
        {
            FillRequested?.Invoke(new TextureFillRequest
            {
                TextureId = TextureBrowser.SelectedAssetId,
                Scale = (float)TextureScale,
                Mirror = MirrorTexture
            });
        });

        public ICommand ClearCommand => new RelayCommand(() =>
        {
            ClearRequested?.Invoke();
        });

        private void RaisePreviewChanged()
        {
            if (_assetManager == null)
                return;

            PreviewChanged?.Invoke(new TextureFillRequest
            {
                TextureId = TextureBrowser.SelectedAssetId,
                Scale = (float)TextureScale,
                Mirror = MirrorTexture
            });
        }

        private float _textureScale = 1.0f;
        public float TextureScale
        {
            get => _textureScale;
            set
            {
                if (SetProperty(ref _textureScale, value))
                    RaisePreviewChanged();
            }
        }

        private bool _mirrorTexture = false;
        public bool MirrorTexture
        {
            get => _mirrorTexture;
            set
            {
                if (SetProperty(ref _mirrorTexture, value))
                    RaisePreviewChanged();
            }
        }

        private VignetteShapeType _vignetteType = VignetteShapeType.Oval;
        public VignetteShapeType VignetteType
        {
            get => _vignetteType;
            set => SetProperty(ref _vignetteType, value);
        }

        private int _vignetteStrength = 148;
        public int VignetteStrength
        {
            get => _vignetteStrength;
            set => SetProperty(ref _vignetteStrength, value);
        }

        private Color _vignetteColor = Color.FromArgb(255, 201, 151, 123);
        public Color VignetteColor
        {
            get => _vignetteColor;
            set
            {
                if (SetProperty(ref _vignetteColor, value))
                {
                    _vignetteBrush.Color = value;
                }
            }
        }

        private SolidColorBrush _vignetteBrush = new(Color.FromArgb(255, 201, 151, 123));

        public Brush VignetteBrush => _vignetteBrush;
    }
}
