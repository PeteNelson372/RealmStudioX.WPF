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
    public class SymbolsPanelViewModel : ViewModelBase, ISymbolSettings
    {
        private readonly EditorController _editor;
        private readonly AssetManager _assetManager;

        public SymbolsPanelViewModel(EditorController editor, AssetManager assetManager)
        {
            _editor = editor;
            _assetManager = assetManager;
        }

        private double _symbolScale = 1.0;
        public double SymbolScale
        {
            get => _symbolScale;
            set
            {
                if (_symbolScale != value && !_symbolScaleLocked)
                {
                    _symbolScale = value;
                    OnPropertyChanged();
                    SymbolValuesChanged();
                }
            }
        }

        private bool _symbolScaleLocked = false;
        public bool SymbolScaleLocked
        {
            get => _symbolScaleLocked;
            set
            {
                if (_symbolScaleLocked != value)
                {
                    _symbolScaleLocked = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _randomizeSymbolColors = false;
        public bool RandomizeSymbolColors
        {
            get => _randomizeSymbolColors;
            set
            {
                if (_randomizeSymbolColors != value)
                {
                    _randomizeSymbolColors = value;
                    OnPropertyChanged();
                }
            }
        }


        // symbol color 1

        private Color _symbolColor1 = Color.FromRgb(85, 44, 36);
        public Color SymbolColor1
        {
            get => _symbolColor1;
            set
            {
                if (SetProperty(ref _symbolColor1, value))
                {
                    _symbolColor1Brush.Color = value;
                    SymbolValuesChanged();
                }
            }
        }

        private SolidColorBrush _symbolColor1Brush = new(Color.FromRgb(85, 44, 36));

        public Brush SymbolColor1Brush => _symbolColor1Brush;


        // symbol color 2

        private Color _symbolColor2 = Color.FromRgb(53, 45, 32);
        public Color SymbolColor2
        {
            get => _symbolColor2;
            set
            {
                if (SetProperty(ref _symbolColor2, value))
                {
                    _symbolColor2Brush.Color = value;
                    SymbolValuesChanged();
                }
            }
        }

        private SolidColorBrush _symbolColor2Brush = new(Color.FromRgb(53, 45, 32));

        public Brush SymbolColor2Brush => _symbolColor2Brush;

        // symbol color 3

        private Color _symbolColor3 = Color.FromArgb(161, 214, 202, 171);
        public Color SymbolColor3
        {
            get => _symbolColor3;
            set
            {
                if (SetProperty(ref _symbolColor3, value))
                {
                    _symbolColor3Brush.Color = value;
                    SymbolValuesChanged();
                }
            }
        }

        private SolidColorBrush _symbolColor3Brush = new(Color.FromArgb(161, 214, 202, 171));

        public Brush SymbolColor3Brush => _symbolColor3Brush;


        private void SymbolValuesChanged()
        {
            if (_assetManager == null)
                return;

            // TODO: apply changes to selected symbols
        }


        public ICommand SelectCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.ShapeSelect);
        });

        public ICommand SymbolEraseCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.SymbolErase);
            _editor.ActivateTool(EditorToolType.SymbolTool, (IMapPathSettings)this);
        });

        public ICommand SymbolPaintCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.SymbolColor);
            _editor.ActivateTool(EditorToolType.SymbolTool, (IMapPathSettings)this);
        });

        public ICommand SelectStructuresCommand => new RelayCommand(() =>
        {

        });

        public ICommand SelectVegetationCommand => new RelayCommand(() =>
        {

        });

        public ICommand SelectTerrainCommand => new RelayCommand(() =>
        {

        });

        public ICommand SelectMarkersCommand => new RelayCommand(() =>
        {

        });

        public ICommand SelectOtherCommand => new RelayCommand(() =>
        {

        });

        public ICommand LockScaleCommand => new RelayCommand(() =>
        {
            SymbolScaleLocked = !SymbolScaleLocked;
        });

        public ICommand ResetColorsCommand => new RelayCommand(() =>
        {

        });
    }

    public interface ISymbolSettings
    {

    }
}
