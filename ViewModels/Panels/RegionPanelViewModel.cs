using RealmStudioShapeRenderingLib;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class RegionPanelViewModel : ViewModelBase, IRegionSettings
    {
        private readonly EditorController _editor;
        private readonly AssetManager _assetManager;

        public RegionPanelViewModel(EditorController editor, AssetManager assetManager)
        {
            _editor = editor;
            _assetManager = assetManager;
        }

        // region style
        private PathType _regionStyle = PathType.SolidLinePath;
        public PathType RegionStyle
        {
            get => _regionStyle;
            set
            {
                if (SetProperty(ref _regionStyle, value))
                {
                    RegionValuesChanged();
                }
            }
        }

        // region color

        private Color _regionColor = Color.FromRgb(0, 86, 179);
        public Color RegionColor
        {
            get => _regionColor;
            set
            {
                if (SetProperty(ref _regionColor, value))
                {
                    _regionColorBrush.Color = value;
                    RegionValuesChanged();
                }
            }
        }

        private SolidColorBrush _regionColorBrush = new(Color.FromRgb(0, 86, 179));

        public Brush RegionColorBrush => _regionColorBrush;

        // region border width

        public int MinRegionBorderWidth { get; } = 2;
        public int MaxRegionBorderWidth { get; } = 20;

        private int _regionBorderWidth = 8;
        public int RegionBorderWidth
        {
            get => _regionBorderWidth;
            set
            {
                var clamped = Math.Clamp(value, MinRegionBorderWidth, MaxRegionBorderWidth);

                if (SetProperty(ref _regionBorderWidth, clamped))
                {
                    RegionValuesChanged();
                }
            }
        }

        // region smoothing

        public int MinSmoothing { get; } = 1;
        public int MaxSmoothing { get; } = 100;

        private int _smoothing = 20;
        public int Smoothing
        {
            get => _smoothing;
            set
            {
                var clamped = Math.Clamp(value, MinSmoothing, MaxSmoothing);

                if (SetProperty(ref _smoothing, clamped))
                {
                    RegionValuesChanged();
                }
            }
        }

        // region inner opacity

        public int MinInnerOpacity { get; } = 0;
        public int MaxInnerOpacity { get; } = 255;

        private int _innerOpacity = 64;
        public int InnerOpacity
        {
            get => _innerOpacity;
            set
            {
                var clamped = Math.Clamp(value, MinInnerOpacity, MaxInnerOpacity);

                if (SetProperty(ref _innerOpacity, clamped))
                {
                    RegionValuesChanged();
                }
            }
        }

        private void RegionValuesChanged()
        {
            _editor.UpdateSelectedRegion((IRegionSettings)this);
        }

        public ICommand SelectCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.ShapeSelect);
            _editor.ActivateTool(EditorToolType.SelectionTool);
        });

        public ICommand CreateRegionCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.RegionPaint);
            _editor.ActivateTool(EditorToolType.RegionTool, (IRegionSettings)this);
        });
    }

    public interface IRegionSettings
    {
        PathType RegionStyle { get; }
        Color RegionColor { get; }
        int RegionBorderWidth { get; }
        int Smoothing { get; }
        int InnerOpacity { get; }
    }
}
