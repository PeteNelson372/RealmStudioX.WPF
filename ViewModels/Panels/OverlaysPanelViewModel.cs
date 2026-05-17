using MaterialDesignThemes.Wpf;
using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.Utilities;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class OverlaysPanelViewModel : ViewModelBase, IMeasureSettings, IGridSettings
    {
        private EditorController _editor;
        private AssetManager _assetManager;
        public ObservableCollection<FrameGridItem> FrameGridItems { get; } = [];

        public OverlaysPanelViewModel(EditorController editor, AssetManager assetManager)
        {
            _editor = editor;
            _assetManager = assetManager;

            AddFrameItems();
        }

        // scale
        public ICommand OpenScaleCommand => new RelayCommand(() =>
        {

        });

        // frame

        private bool _frameEnabled = false;
        public bool FrameEnabled
        {
            get => _frameEnabled;
            set
            {
                if (SetProperty(ref _frameEnabled, value))
                {
                    FrameChanged();
                }
            }
        }

        private Color _frameColor = Colors.White;
        public Color FrameColor
        {
            get => _frameColor;
            set
            {
                if (SetProperty(ref _frameColor, value))
                {
                    _frameBrush.Color = value;
                    FrameChanged();
                }
            }
        }

        private SolidColorBrush _frameBrush = new(Colors.White);
        public Brush FrameBrush => _frameBrush;


        private float _frameScale = 1.0f;
        public float FrameScale
        {
            get => _frameScale;
            set
            {
                if (SetProperty(ref _frameScale, value))
                {
                    FrameChanged();
                }
            }
        }

        private FrameGridItem? _selectedFrameGridItem;

        public FrameGridItem? SelectedFrameGridItem
        {
            get => _selectedFrameGridItem;
            set
            {
                if (SetProperty(ref _selectedFrameGridItem, value))
                {
                    FrameChanged();
                }
            }
        }

        private void FrameChanged()
        {
            if (SelectedFrameGridItem == null || SelectedFrameGridItem.Frame == null)
            {
                return;
            }

            _editor.SetFrame(
                SelectedFrameGridItem.Frame,
                FrameColor.ToSKColor(),
                FrameScale);
        }

        internal void AddFrameItems()
        {
            IReadOnlyList<MapFrame> frames = _assetManager.MapFrames;

            foreach (MapFrame frame in frames)
            {
                if (string.IsNullOrEmpty(frame.FrameBitmapPath))
                {
                    continue;
                }

                frame.FrameBitmap = SKBitmap.Decode(frame.FrameBitmapPath);
                FrameGridItem item = new(frame, UserInterfaceUtilities.CreateThumbnail(frame.FrameBitmapPath, 100, 60));
                FrameGridItems.Add(item);
            }
        }



        public ICommand SetScaleCommand => new RelayCommand(() =>
        {

        });

        // grid

        public ICommand SetGridCommand => new RelayCommand(() =>
        {
            GridEnabled = !GridEnabled;
            _editor.SetGrid((IGridSettings)this);
        });

        private bool _gridEnabled = false;
        public bool GridEnabled
        {
            get => _gridEnabled;
            set
            {
                if (SetProperty(ref _gridEnabled, value))
                {
                    GridChanged();
                    OnPropertyChanged(nameof(GridIcon));
                }
            }
        }

        public PackIconKind GridIcon => GridEnabled ? PackIconKind.GridOn : PackIconKind.GridOff;

        private MapGridType _gridType = MapGridType.Square;
        public MapGridType GridType
        {
            get => _gridType;
            set
            {
                if (SetProperty(ref _gridType, value))
                {
                    GridChanged();
                }
            }
        }

        private int _gridLayer = MapBuilder.DEFAULTGRIDLAYER;
        public int GridLayer
        {
            get => _gridLayer;
            set
            {
                if (SetProperty(ref _gridLayer, value))
                {
                    GridChanged();
                }
            }
        }

        private Color _gridColor = Color.FromArgb(126, 0, 0, 0);
        public Color GridColor
        {
            get => _gridColor;
            set
            {
                if (SetProperty(ref _gridColor, value))
                {
                    _gridBrush.Color = value;
                    GridChanged();
                }
            }
        }

        private SolidColorBrush _gridBrush = new(Color.FromArgb(126, 0, 0, 0));
        public Brush GridBrush => _gridBrush;


        public int MinGridSize { get; } = 8;
        public int MaxGridSize { get; } = 256;

        private int _gridSize = 64;
        public int GridSize
        {
            get => _gridSize;
            set
            {
                var clamped = Math.Clamp(value, MinGridSize, MaxGridSize);

                if (SetProperty(ref _gridSize, clamped))
                {
                    GridChanged();
                }
            }
        }

        public int MinGridLineWidth { get; } = 1;
        public int MaxGridLineWidth { get; } = 10;

        private int _gridLineWidth = 2;
        public int GridLineWidth
        {
            get => _gridLineWidth;
            set
            {
                var clamped = Math.Clamp(value, MinGridLineWidth, MaxGridLineWidth);

                if (SetProperty(ref _gridLineWidth, clamped))
                {
                    GridChanged();
                }
            }
        }

        private bool _showGridSize = true;
        public bool ShowGridSize
        {
            get => _showGridSize;
            set
            {
                if (SetProperty(ref _showGridSize, value))
                {
                    GridChanged();
                }
            }
        }

        private void GridChanged()
        {
            _editor.UpdateGrid((IGridSettings)this);
        }

        // measure

        public ICommand CreateMeasureCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawMapMeasure);
            _editor.ActivateTool(EditorToolType.MeasureTool, (IMeasureSettings)this);
        });

        public ICommand ClearMeasureCommand => new RelayCommand(() =>
        {
            _editor.ClearMapMeasures();
        });



        private Color _measureColor = Color.FromArgb(191, 138, 26, 0);
        public Color MeasureColor
        {
            get => _measureColor;
            set
            {
                if (SetProperty(ref _measureColor, value))
                {
                    _measureBrush.Color = value;

                }
            }
        }

        private SolidColorBrush _measureBrush = new(Color.FromArgb(191, 138, 26, 0));

        public Brush MeasureBrush => _measureBrush;


        private bool _useScaleUnits = true;
        public bool UseScaleUnits
        {
            get => _useScaleUnits;
            set
            {
                SetProperty(ref _useScaleUnits, value);
            }
        }

        private bool _measureArea = false;
        public bool MeasureArea
        {
            get => _measureArea;
            set
            {
                SetProperty(ref _measureArea, value);
            }
        }

    }

    public class FrameGridItem
    {
        public ImageSource? FrameImage { get; }

        public ImageSource Thumbnail { get; }

        public MapFrame Frame { get; }

        public FrameGridItem(MapFrame frame,
                  ImageSource thumbnail)
        {
            Frame = frame;
            Thumbnail = thumbnail;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public interface IGridSettings
    {
        bool GridEnabled { get; }
        MapGridType GridType { get; }
        int GridLayer { get; }
        int GridSize { get; }
        int GridLineWidth { get; }
        Color GridColor { get; }
        bool ShowGridSize { get; }
    }

    public interface IMeasureSettings
    {
        Color MeasureColor { get; }
        bool UseScaleUnits { get; }
        bool MeasureArea { get; }
    }
}

