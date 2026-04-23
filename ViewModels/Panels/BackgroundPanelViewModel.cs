using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Controls;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using System.Windows.Input;

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
    }
}
