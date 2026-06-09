using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using SkiaSharp;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Controls
{
    public class PixelEditorViewModel : ViewModelBase, IPixelEditSettings
    {
        private EditorController _editor;

        public PixelEditorViewModel(EditorController editor)
        {
            _editor = editor;
        }

        public EditorController Editor
        {
            get => _editor;
        }

        private SKPoint _editLocation = SKPoint.Empty;
        public SKPoint EditLocation
        {
            get => _editLocation;
            set => _editLocation = value;
        }

        private List<PixelEdit> _pixelEdits = [];
        public List<PixelEdit> PixelEdits
        {
            get => _pixelEdits;
            set => _pixelEdits = value;
        }

        // pixel color
        private Color _pixelColor = Colors.White;
        public Color PixelColor
        {
            get => _pixelColor;
            set
            {
                if (SetProperty(ref _pixelColor, value))
                {
                    _pixelColorBrush.Color = value;
                }
            }
        }

        private SolidColorBrush _pixelColorBrush = new(Colors.Black);
        public Brush PixelColorBrush => _pixelColorBrush;

        private SKBitmap _workingBitmap = new();
        public SKBitmap WorkingBitmap
        {
            get => _workingBitmap;
            set
            {
                _workingBitmap = value;
                PixelEditorImage = (System.Windows.Media.Imaging.BitmapSource?)_workingBitmap.ToImageSource();
                OnPropertyChanged(nameof(PixelEditorImage));
            }
        }

        public BitmapSource? PixelEditorImage
        {
            get;
            private set;
        }

        private string _hoverPixelText = string.Empty;
        public string HoverPixelText
        {
            get => _hoverPixelText;
            set
            {
                SetProperty(ref _hoverPixelText, value);
            }
        }

        private string _currentColorText = string.Empty;
        public string CurrentColorText
        {
            get => _currentColorText;
            set
            {
                SetProperty(ref _currentColorText, value);
            }
        }

        private string _originalColorText = string.Empty;
        public string OriginalColorText
        {
            get => _originalColorText;
            set
            {
                SetProperty(ref _originalColorText, value);
            }
        }    

    }

    public interface IPixelEditSettings
    {
        Color PixelColor { get; }
        SKPoint EditLocation { get; }
        List<PixelEdit> PixelEdits { get; }
    }
}
