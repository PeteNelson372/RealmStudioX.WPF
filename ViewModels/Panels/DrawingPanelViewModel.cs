using RealmStudioShapeRenderingLib;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.Editor.Tools;
using RealmStudioX.WPF.Utilities;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class DrawingPanelViewModel : ViewModelBase, IDrawingSettings
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        public MainWindowViewModel MainViewModel => _mainWindowViewModel;

        private readonly EditorController _editor;
        public EditorController Editor => _editor;

        private readonly AssetManager _assetManager;

        public ObservableCollection<BoxGridItem> BoxItems { get; } = [];

        public DrawingPanelViewModel(MainWindowViewModel mainViewModel, EditorController editor, AssetManager assetManager)
        {
            _mainWindowViewModel = mainViewModel;
            _editor = editor;
            _assetManager = assetManager;
        }


        public ObservableCollection<BrushPatternItem>BrushPatterns
        { get; } = [];

        private BrushPatternItem? _selectedBrushPattern;

        public BrushPatternItem? SelectedBrushPattern
        {
            get => _selectedBrushPattern;
            set => SetProperty(ref _selectedBrushPattern, value);
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

        // label color

        private Color _labelColor = Color.FromRgb(61,53,30);
        public Color LabelColor
        {
            get => _labelColor;
            set
            {
                if (SetProperty(ref _labelColor, value))
                {
                    _labelColorBrush.Color = value;

                }
            }
        }

        private SolidColorBrush _labelColorBrush = new(Color.FromRgb(61, 53, 30));

        public Brush LabelColorBrush => _labelColorBrush;

        // outline color

        private Color _outlineColor = Color.FromArgb(161, 214, 202, 171);
        public Color OutlineColor
        {
            get => _outlineColor;
            set
            {
                if (SetProperty(ref _outlineColor, value))
                {
                    _outlineColorBrush.Color = value;

                }
            }
        }

        private SolidColorBrush _outlineColorBrush = new(Color.FromArgb(161, 214, 202, 171));

        public Brush OutlineColorBrush => _outlineColorBrush;

        // outline width

        public float MinOutlineWidth { get; } = 0.0f;
        public float MaxOutlineWidth { get; } = 32.0f;

        private float _outlineWidth = 0.0f;
        public float OutlineWidth
        {
            get => _outlineWidth;
            set
            {
                var clamped = Math.Clamp(value, MinOutlineWidth, MaxOutlineWidth);

                if (_outlineWidth != clamped)
                {
                    _outlineWidth = clamped;
                    OnPropertyChanged();

                }
            }
        }

        // glow color

        private Color _glowColor = Color.FromRgb(61, 53, 30);
        public Color GlowColor
        {
            get => _glowColor;
            set
            {
                if (SetProperty(ref _glowColor, value))
                {
                    _glowColorBrush.Color = value;

                }
            }
        }

        private SolidColorBrush _glowColorBrush = new(Colors.White);

        public Brush GlowColorBrush => _glowColorBrush;


        // rotation

        public int MinRotation { get; } = 0;
        public int MaxRotation { get; } = 359;

        private int _rotation = 0;
        public int Rotation
        {
            get => _rotation;
            set
            {
                var clamped = Math.Clamp(value, MinRotation, MaxRotation);

                if (_rotation != clamped)
                {
                    _rotation = clamped;
                    OnPropertyChanged();

                }
            }
        }





        public ICommand SelectCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.ShapeSelect);
        });

        public ICommand PlaceLabelCommand => new RelayCommand(() =>
        {

        });

        public ICommand DrawLabelCurveCommand => new RelayCommand(() =>
        {

        });

        public ICommand DrawLabelArcCommand => new RelayCommand(() =>
        {

        });





        public ICommand CreateBoxCommand => new RelayCommand(() =>
        {

        });


    }

    public interface IDrawingSettings
    {

    }

    public class BrushPatternItem
    {
        public string Name { get; set; } = "";

        public ImageSource? PreviewImage { get; set; }

        public MapBrush? BrushDefinition { get; set; }
    }
}

