using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Models.Map;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using System.Windows.Input;

namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    public class ResizeMapViewModel : ViewModelBase
    {
        private RealmStudioMap _mapToResize;

        public RealmStudioMap MapToResize
        {
            get => _mapToResize;
            set => SetProperty(ref _mapToResize, value);
        }

        private ResizeMapAnchorPoint _resizeAnchorPoint = ResizeMapAnchorPoint.CenterZoomed;

        public ResizeMapAnchorPoint ResizeAnchorPoint
        {
            get => _resizeAnchorPoint;
            set => SetProperty(ref _resizeAnchorPoint, value);
        }

        private bool _scaleMapEnabled = true;

        public bool ScaleMapEnabled
        {
            get => _scaleMapEnabled;
            set => SetProperty(ref _scaleMapEnabled, value);
        }

        private string _mapName = string.Empty;
        public string MapName
        {
            get => _mapName;
            set => SetProperty(ref _mapName, value);
        }

        private string? _selectedTheme;
        public string? SelectedTheme
        {
            get => _selectedTheme;
            set => SetProperty(ref _selectedTheme, value);
        }

        private int _width = 1920;
        public int Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        private int _height = 1080;
        public int Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        private float _aspectRatio = (float)1920 / 1080;
        public float AspectRatio
        {
            get => _aspectRatio;
            set => SetProperty(ref _aspectRatio, value);
        }

        private float _areaWidth = 100;
        public float AreaWidth
        {
            get => _areaWidth;
            set => SetProperty(ref _areaWidth, value);
        }

        private float _areaHeight = 75;
        public float AreaHeight
        {
            get => _areaHeight;
            set => SetProperty(ref _areaHeight, value);
        }

        private string _mapAreaUnits = "Miles";
        public string MapAreaUnits
        {
            get => _mapAreaUnits;
            set => SetProperty(ref _mapAreaUnits, value);
        }

        public ResizeMapResult? Result { get; private set; }

        public ICommand ResizeMapCommand { get; }
        public ICommand CancelResizeCommand { get; }

        public event Action<bool?>? RequestClose;

        public ResizeMapViewModel(RealmStudioMap map)
        {
            _mapToResize = map ?? throw new ArgumentNullException(nameof(map));
            ResizeMapCommand = new RelayCommand(Resize);
            CancelResizeCommand = new RelayCommand(Cancel);
        }

        private void Resize()
        {
            Result = new ResizeMapResult()
            {
                Map = MapToResize,
                AnchorPoint = ResizeAnchorPoint,
                Width = Width,
                Height = Height,
            };

            RequestClose?.Invoke(true);
        }

        // -------------------------
        // CANCEL
        // -------------------------
        private void Cancel()
        {
            Result = null;
            RequestClose?.Invoke(false);
        }
    }
}
