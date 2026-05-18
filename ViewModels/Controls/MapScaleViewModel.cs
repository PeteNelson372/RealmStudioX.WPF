using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Controls
{
    public class MapScaleViewModel : ViewModelBase, IMapScaleSettings
    {
        private EditorController _editor;

        public MapScaleViewModel(EditorController editor)
        {
            _editor = editor;
        }

        // font style model
        private FontStyleModel _fontStyle = new();
        public FontStyleModel FontStyle
        {
            get => _fontStyle;
            set
            {
                if (SetProperty(ref _fontStyle, value))
                {
                    _fontStyle = value;
                    OnPropertyChanged(nameof(SelectedFontFamily));
                }
            }
        }

        public string SelectedFontFamily
        {
            get => FontStyle?.Family ?? "Segoe UI";
        }


        public int MinScaleWidth { get; } = 64;
        public int MaxScaleWidth { get; } = 2048;

        private int _scaleWidth = 256;
        public int ScaleWidth
        {
            get => _scaleWidth;
            set
            {
                var clamped = Math.Clamp(value, MinScaleWidth, MaxScaleWidth);
                SetProperty(ref _scaleWidth, clamped);
            }
        }

        public int MinScaleHeight { get; } = 4;
        public int MaxScaleHeight { get; } = 64;

        private int _scaleHeight = 16;
        public int ScaleHeight
        {
            get => _scaleHeight;
            set
            {
                var clamped = Math.Clamp(value, MinScaleHeight, MaxScaleHeight);
                SetProperty(ref _scaleHeight, clamped);
            }
        }

        public int MinScaleSegments { get; } = 1;
        public int MaxScaleSegments { get; } = 32;

        private int _scaleSegments = 5;
        public int ScaleSegments
        {
            get => _scaleSegments;
            set
            {
                var clamped = Math.Clamp(value, MinScaleSegments, MaxScaleSegments);
                SetProperty(ref _scaleSegments, clamped);
            }
        }

        public int MinScaleLineWidth { get; } = 2;
        public int MaxScaleLineWidth { get; } = 8;

        private int _scaleLineWidth = 3;
        public int ScaleLineWidth
        {
            get => _scaleLineWidth;
            set
            {
                var clamped = Math.Clamp(value, MinScaleLineWidth, MaxScaleLineWidth);
                SetProperty(ref _scaleLineWidth, clamped);
            }
        }

        // color 1
        private Color _segmentColor1 = Colors.Black;
        public Color SegmentColor1
        {
            get => _segmentColor1;
            set
            {
                if (SetProperty(ref _segmentColor1, value))
                {
                    _segmentColor1Brush.Color = value;
                }
            }
        }

        private SolidColorBrush _segmentColor1Brush = new(Colors.Black);
        public Brush SegmentColor1Brush => _segmentColor1Brush;

        // color 2
        private Color _segmentColor2 = Colors.White;
        public Color SegmentColor2
        {
            get => _segmentColor2;
            set
            {
                if (SetProperty(ref _segmentColor2, value))
                {
                    _segmentColor2Brush.Color = value;
                }
            }
        }

        private SolidColorBrush _segmentColor2Brush = new(Colors.White);
        public Brush SegmentColor2Brush => _segmentColor2Brush;

        // color 3
        private Color _segmentColor3 = Colors.Black;
        public Color SegmentColor3
        {
            get => _segmentColor3;
            set
            {
                if (SetProperty(ref _segmentColor3, value))
                {
                    _segmentColor3Brush.Color = value;
                }
            }
        }

        private SolidColorBrush _segmentColor3Brush = new(Colors.Black);
        public Brush SegmentColor3Brush => _segmentColor3Brush;


        public float MinSegmentDistance{ get; } = 0.1f;
        public float MaxSegmentDistance { get; } = 10000.0f;

        private float _segmentDistance = 100.0f;
        public float SegmentDistance
        {
            get => _segmentDistance;
            set
            {
                var clamped = Math.Clamp(value, MinSegmentDistance, MaxSegmentDistance);
                SetProperty(ref _segmentDistance, clamped);
            }
        }

        private string _unitLabel = string.Empty;
        public string UnitLabel
        {
            get => _unitLabel;
            set => SetProperty(ref _unitLabel, value);
        }

        private ScaleNumbersDisplayLocation _scaleNumbersDisplayLocation = ScaleNumbersDisplayLocation.EveryOther;
        public ScaleNumbersDisplayLocation ScaleNumbersDisplayLocation
        {
            get => _scaleNumbersDisplayLocation;
            set => SetProperty(ref _scaleNumbersDisplayLocation, value);
        }

        // font color
        private Color _fontColor = Colors.White;
        public Color FontColor
        {
            get => _fontColor;
            set
            {
                if (SetProperty(ref _fontColor, value))
                {
                    _fontColorBrush.Color = value;
                }
            }
        }

        private SolidColorBrush _fontColorBrush = new(Colors.Black);
        public Brush FontColorBrush => _fontColorBrush;

        // numbers outline color
        private Color _numbersOutlineColor = Colors.Black;
        public Color NumbersOutlineColor
        {
            get => _numbersOutlineColor;
            set
            {
                if (SetProperty(ref _numbersOutlineColor, value))
                {
                    _numbersOutlineColorBrush.Color = value;
                }
            }
        }

        private SolidColorBrush _numbersOutlineColorBrush = new(Colors.Black);
        public Brush NumbersOutlineColorBrush => _numbersOutlineColorBrush;


        public int MinNumbersOutlineWidth { get; } = 0;
        public int MaxNumbersOutlineWidth { get; } = 32;

        private int _numbersOutlineWidth = 1;
        public int NumbersOutlineWidth
        {
            get => _numbersOutlineWidth;
            set
            {
                var clamped = Math.Clamp(value, MinNumbersOutlineWidth, MaxNumbersOutlineWidth);
                SetProperty(ref _numbersOutlineWidth, clamped);
            }
        }


        public ICommand CreateScaleCommand => new RelayCommand(() =>
        {
            _editor.CreateMapScale((IMapScaleSettings)this);
        });

        public ICommand RemoveScaleCommand => new RelayCommand(() =>
        {
            _editor.RemoveMapScale();
        });
    }

    public interface IMapScaleSettings
    {
        int ScaleWidth { get; }
        int ScaleHeight { get; }
        int ScaleSegments { get; }
        int ScaleLineWidth { get; }
        Color SegmentColor1 { get; }
        Color SegmentColor2 { get; }
        Color SegmentColor3 { get; }
        float SegmentDistance { get; }
        string UnitLabel { get; }
        ScaleNumbersDisplayLocation ScaleNumbersDisplayLocation { get; }
        FontStyleModel FontStyle { get; }
        Color FontColor { get; }
        Color NumbersOutlineColor { get; }
        int NumbersOutlineWidth { get; }
    }
}
