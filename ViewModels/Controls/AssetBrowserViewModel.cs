using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using SkiaSharp;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace RealmStudioX.WPF.ViewModels.Controls
{
    public class AssetBrowserViewModel : ViewModelBase
    {
        private readonly AssetBrowser _browser;
        public event Action? TextureSelectionChanged;

        public AssetBrowserViewModel(AssetBrowser browser)
        {
            _browser = browser;

            NextCommand = new RelayCommand(Next);
            PreviousCommand = new RelayCommand(Previous);

            UpdateCurrent();
        }

        private BitmapSource? _imageSource;
        public BitmapSource? ImageSource
        {
            get => _imageSource;
            private set => SetProperty(ref _imageSource, value);
        }

        public string? SelectedAssetId => _browser.GetCurrentAsset()?.Id;

        private void UpdateCurrent()
        {
            var img = _browser.GetCurrentImage();

            ImageSource = (BitmapSource?)SKBitmap.FromImage(img).ToImageSource();
            CurrentName = _browser.GetCurrentAsset()?.Name;
            TextureSelectionChanged?.Invoke();
        }

        // -------------------------
        // Commands
        // -------------------------

        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }

        // -------------------------
        // Bindable properties
        // -------------------------

        private SKImage? _currentImage;
        public SKImage? CurrentImage
        {
            get => _currentImage;
            private set => SetProperty(ref _currentImage, value);
        }

        private string? _currentName;
        public string? CurrentName
        {
            get => _currentName;
            private set => SetProperty(ref _currentName, value);
        }

        // -------------------------
        // Actions
        // -------------------------

        private void Next()
        {
            _browser.Next();
            UpdateCurrent();
        }

        private void Previous()
        {
            _browser.Previous();
            UpdateCurrent();
        }
    }
}
