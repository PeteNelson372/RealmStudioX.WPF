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
    public class WaterPanelViewModel : ViewModelBase, IWaterBodySettings
    {
        private readonly EditorController _editor;
        private readonly AssetManager _assetManager;

        public WaterPanelViewModel(EditorController editor, AssetManager assetManager)
        {
            _editor = editor;
            _assetManager = assetManager;
        }

        private int _waterBrushSize = 20;
        public int WaterBrushSize
        {
            get => _waterBrushSize;
            set => SetProperty(ref _waterBrushSize, value);
        }

        private int _waterEraserSize = 20;
        public int WaterEraserSize
        {
            get => _waterEraserSize;
            set => SetProperty(ref _waterEraserSize, value);
        }

        // shallow water 

        private Color _shallowWaterColor = Color.FromArgb(168, 140, 191, 197);
        public Color ShallowWaterColor
        {
            get => _shallowWaterColor;
            set
            {
                if (SetProperty(ref _shallowWaterColor, value))
                {
                    _shallowWaterColorBrush.Color = value;
                }
            }
        }

        private SolidColorBrush _shallowWaterColorBrush = new(Color.FromArgb(168, 140, 191, 197));

        public Brush ShallowWaterColorBrush => _shallowWaterColorBrush;

        // deep water

        private Color _deepWaterColor = Color.FromArgb(168, 140, 191, 197);
        public Color DeepWaterColor
        {
            get => _deepWaterColor;
            set
            {
                if (SetProperty(ref _deepWaterColor, value))
                {
                    _deepWaterColorBrush.Color = value;
                }
            }
        }

        private SolidColorBrush _deepWaterColorBrush = new(Color.FromArgb(168, 140, 191, 197));

        public Brush DeepWaterColorBrush => _deepWaterColorBrush;

        // shoreline color

        private Color _shorelineColor = Colors.Tan;
        public Color ShorelineColor
        {
            get => _shorelineColor;
            set
            {
                if (SetProperty(ref _shorelineColor, value))
                {
                    _shorelineColorBrush.Color = value;
                }
            }
        }

        private SolidColorBrush _shorelineColorBrush = new(Colors.Tan);

        public Brush ShorelineColorBrush => _shorelineColorBrush;


        // river width

        private int _riverWidth = 16;
        public int RiverWidth
        {
            get => _riverWidth;
            set => SetProperty(ref _riverWidth, value);
        }

        // meander strength

        private float _meanderStrength = 1.6f;
        public float MeanderStrength
        {
            get => _meanderStrength;
            set => SetProperty(ref _meanderStrength, value);
        }

        // source fade-in

        private bool _sourceFadeIn = true;
        public bool SourceFadeIn
        {
            get => _sourceFadeIn;
            set => SetProperty(ref _sourceFadeIn, value);
        }

        // edit river points
        private bool _editRiverPoints = false;
        public bool EditRiverPoints
        {
            get => _editRiverPoints;
            set => SetProperty(ref _editRiverPoints, value);
        }

        // commands
        public ICommand SelectCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.ShapeSelect);
        });

        public ICommand WaterPaintCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.WaterPaint);
            _editor.ActivateTool(EditorToolType.WaterBodyTool, (IWaterBodySettings)this);
        });

        public ICommand CreateLakeCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.LakePaint);
            _editor.ActivateTool(EditorToolType.WaterBodyTool, (IWaterBodySettings)this);
        });

        public ICommand DrawRiverCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.RiverPaint);
            _editor.ActivateTool(EditorToolType.WaterBodyTool, (IWaterBodySettings)this);
        });

        public ICommand EraseCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.WaterErase);
            _editor.ActivateTool(EditorToolType.WaterBodyTool, (IWaterBodySettings)this);
        });

        public ICommand EditRiverCommand => new RelayCommand(() =>
        {
            if (_editor != null && _editor.SelectedShape is River r)
            {
                r.Editor.IsEditing = EditRiverPoints;

                if (r.Editor.IsEditing)
                {
                    r.WaterSystem?.BeginInteractive();
                    r.BeginInteractive();
                }
                else
                {
                    r.WaterSystem?.EndInteractive();
                    r.EndInteractive();

                    r.Editor.OnChanged!();
                }
            }
        });
    }

    public interface IWaterBodySettings
    {
        int WaterBrushSize { get; }
        int WaterEraserSize { get; }
        Color ShallowWaterColor { get; }
        Color DeepWaterColor { get; }
        Color ShorelineColor { get; }
        int RiverWidth { get; }
        float MeanderStrength { get; }
        bool SourceFadeIn { get; }
        bool EditRiverPoints { get; }

    }
}
