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
        private EditorController _editor;
        private AssetManager _assetManager;

        public Action<TextureFillRequest>? FillRequested;
        public Action? ClearRequested;

        public AssetBrowserViewModel TextureBrowser { get; }

        public BackgroundPanelViewModel(EditorController editor, AssetManager assetManager)
        {
            _editor = editor;
            _assetManager = assetManager;
            var browser = new AssetBrowser(assetManager, AssetType.BackgroundTexture);
            TextureBrowser = new AssetBrowserViewModel(browser);
        }

        private ICommand? _fillCommand;
        public ICommand FillCommand =>
            _fillCommand ??= new RelayCommand(() =>
            {
                TextureFillRequest fillRequest = new()
                {
                    TextureId = TextureBrowser.SelectedAssetId,
                    Scale = (float)TextureScale,
                    Mirror = MirrorTexture
                };

                _editor.FillBackground(fillRequest);
            });


        private ICommand? _clearCommand;
        public ICommand ClearCommand =>
            _clearCommand ??= new RelayCommand(() => _editor.ClearBackground());

        private void PreviewChanged()
        {
            if (_assetManager == null)
                return;

            TextureFillRequest updateRequest = new()
            {
                TextureId = TextureBrowser.SelectedAssetId,
                Scale = (float)TextureScale,
                Mirror = MirrorTexture
            };

            _editor.UpdateBackgroundPreview(updateRequest);
        }

        private float _textureScale = 1.0f;
        public float TextureScale
        {
            get => _textureScale;
            set
            {
                if (SetProperty(ref _textureScale, value))
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
