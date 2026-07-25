using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using SkiaSharp;
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

        // -------------------------------------------------
        // Commands
        // -------------------------------------------------

        public ICommand NextCommand { get; }

        public ICommand PreviousCommand { get; }

        // -------------------------------------------------
        // Bindable Properties
        // -------------------------------------------------

        private BitmapSource? _imageSource;
        public BitmapSource? ImageSource
        {
            get => _imageSource;
            private set => SetProperty(ref _imageSource, value);
        }

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

        public string? SelectedAssetId =>
            _browser.Current?.Id;

        // -------------------------------------------------
        // Private Helpers
        // -------------------------------------------------

        private void UpdateCurrent()
        {
            CurrentImage = _browser.CurrentImage;

            ImageSource = CurrentImage == null
                ? null
                : (BitmapSource?)SKBitmap
                    .FromImage(CurrentImage)
                    .ToImageSource();

            CurrentName = _browser.Current?.Name;

            TextureSelectionChanged?.Invoke();
        }

        // -------------------------------------------------
        // Actions
        // -------------------------------------------------

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

        public void SetById(string? assetId)
        {
            if (_browser.SelectById(assetId))
            {
                UpdateCurrent();
            }
        }

        public void SetByIndex(int index)
        {
            if (_browser.SelectByIndex(index))
            {
                UpdateCurrent();
            }
        }
    }
}