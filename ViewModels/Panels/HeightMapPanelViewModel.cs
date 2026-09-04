using RealmStudioShapeRenderingLib;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.Editor.Tools;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class HeightMapPanelViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainViewModel;

        private readonly EditorController _editor;

        private List<HypsometricPalette> _hypsometricPalettes = [];

        public List<HypsometricPalette> HypsometricPalettes => _hypsometricPalettes;

        public HeightMapPanelViewModel(MainWindowViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _editor = mainViewModel.Editor;

            var paletteBrowser = new AssetBrowser(_mainViewModel.AssetManager, AssetType.HeightMapPalette);

            IReadOnlyList<AssetDescriptor> paletteDescriptors = paletteBrowser.Assets;

            for (int i = 0; i < paletteDescriptors.Count; i++)
            {
                AssetDescriptor descriptor = paletteDescriptors[i];
                if (descriptor.Type == AssetType.HeightMapPalette)
                {
                    string xml = File.ReadAllText(descriptor.FilePath);
                    HypsometricPalette palette = MapFileMethods.DeserializeObject<HypsometricPalette>(xml);

                    if (palette != null)
                    {
                        _hypsometricPalettes.Add(palette);

                        // TODO: allow the user to select a default palette in the settings,
                        // and use that instead of hardcoding the Natural Earth palette

                        // use the Natural Earth palette as the default
                        if (palette.Id.Equals("b83f2d91-6a47-4e15-9c72-f08a35d614be"))
                        {
                            SelectedPalette = palette;

                            Tints.Clear();

                            Tints = [.. SelectedPalette.Tints];
                        }
                    }
                }
            }

            // height change is initially set to 1% of range
            HeightChange = (Math.Abs(MaximumHeight) + Math.Abs(MinimumHeight)) * 0.01f;
        }

        private float _minimumHeight = -5000;
        public float MinimumHeight
        {
            get { return _minimumHeight; }
            set
            {
                if (value < _maximumHeight)
                {
                    SetProperty(ref _minimumHeight, value);

                    if (_editor.ActiveEditorTool is HeightMapTool hmt && hmt.ActiveHeightMap != null)
                    {
                        UpdateHeightMapProperties(hmt.ActiveHeightMap);
                    }
                }
            }
        }

        private float _maximumHeight = 50000;
        public float MaximumHeight
        {
            get { return _maximumHeight; }
            set
            {
                if (value > _minimumHeight)
                {
                    SetProperty(ref _maximumHeight, value);

                    if (_editor.ActiveEditorTool is HeightMapTool hmt && hmt.ActiveHeightMap != null)
                    {
                        UpdateHeightMapProperties(hmt.ActiveHeightMap);
                    }
                }
            }
        }

        private string _heightUnit = "Feet";
        public string HeightUnit
        {
            get => _heightUnit;
            set
            {
                SetProperty(ref _heightUnit, value);

                if (_editor.ActiveEditorTool is HeightMapTool hmt && hmt.ActiveHeightMap != null)
                {
                    UpdateHeightMapProperties(hmt.ActiveHeightMap);
                }
            }
        }

        private float _heightChange = 100.0f;

        public float HeightChange
        {
            get => _heightChange;
            set
            {
                SetProperty(ref _heightChange, value);

                if (_editor.ActiveEditorTool is HeightMapTool hmt && hmt.ActiveHeightMap != null)
                {
                    UpdateHeightMapProperties(hmt.ActiveHeightMap);
                }
            }
        }

        private HypsometricPalette? _selectedPalette;
        public HypsometricPalette? SelectedPalette
        {
            get { return _selectedPalette; }
            set
            {
                if (value != null)
                {
                    SetProperty(ref _selectedPalette, value);

                    if (_selectedPalette != null)
                    {
                        Tints.Clear();
                        Tints = [.. _selectedPalette.Tints];

                        _editor.ActivateTool(EditorToolType.HeightMapTool);

                        if (_editor.ActiveEditorTool is HeightMapTool hmt && hmt.ActiveHeightMap != null)
                        {
                            UpdateHeightMapProperties(hmt.ActiveHeightMap);
                        }
                    }
                }
            }
        }

        private HypsometricPalette? _userSelectedPalette;
        public HypsometricPalette? UserSelectedPalette
        {
            get { return _userSelectedPalette; }
            set
            {
                SetProperty(ref _userSelectedPalette, value);
            }
        }

        private List<HypsometricTint> _tints = [];

        public List<HypsometricTint> Tints
        {
            get { return _tints; }
            set { SetProperty(ref _tints, value); }
        }

        public ICommand IncreaseHeightCommand => new RelayCommand(() =>
        {
            if (_mainViewModel.RenderHeightMap && _editor.Scene != null)
            {
                _editor.SetDrawingMode(MapDrawingMode.MapHeightIncrease);

                _editor.ActivateTool(EditorToolType.HeightMapTool);
            }
        });

        public ICommand DecreaseHeightCommand => new RelayCommand(() =>
        {
            if (_mainViewModel.RenderHeightMap && _editor.Scene != null)
            {
                _editor.SetDrawingMode(MapDrawingMode.MapHeightDecrease);

                _editor.ActivateTool(EditorToolType.HeightMapTool);
            }
        });

        private void UpdateHeightMapProperties(MapHeightMap heightMap)
        {
            heightMap.MinimumHeight = MinimumHeight;
            heightMap.MaximumHeight = MaximumHeight;
            heightMap.HeightUnit = HeightUnit;
            heightMap.HeightMapPalette = _selectedPalette;
            heightMap.RebuildHypsometricColorLookup();
        }

        //
        // contour lines
        //

        private bool _showContourLines = false;
        public bool ShowContourLines
        {
            get => _showContourLines;
            set => SetProperty(ref _showContourLines, value);
        }

        private float _contourInterval = 1000.0f;
        public float ContourInterval
        {
            get => _contourInterval;
            set => SetProperty(ref _contourInterval, value);
        }

        // major contour line color

        private Color _majorLineColor = Colors.Gray;
        public Color MajorLineColor
        {
            get => _majorLineColor;
            set
            {
                if (SetProperty(ref _majorLineColor, value))
                {
                    _majorLineColorBrush.Color = value;
                }
            }
        }

        private readonly SolidColorBrush _majorLineColorBrush = new(Colors.Gray);

        public Brush MajorLineColorBrush => _majorLineColorBrush;

        //  contour line color

        private Color _lineColor = Colors.LightGray;
        public Color LineColor
        {
            get => _lineColor;
            set
            {
                if (SetProperty(ref _lineColor, value))
                {
                    _lineColorBrush.Color = value;
                }
            }
        }

        private readonly SolidColorBrush _lineColorBrush = new(Colors.LightGray);

        public Brush LineColorBrush => _lineColorBrush;

        private int _majorLineWidth = 2;
        public int MajorLineWidth
        {
            get => _majorLineWidth;
            set => SetProperty(ref _majorLineWidth, value);
        }

        private int _contourLineWidth = 1;
        public int ContourLineWidth
        {
            get => _contourLineWidth;
            set => SetProperty(ref _contourLineWidth, value);
        }

        private int _majorContourInterval = 5;
        public int MajorContourInterval
        {
            get => _majorContourInterval;
            set => SetProperty(ref _majorContourInterval, value);
        }

        private bool _showContourLabels = false;
        public bool ShowContourLabels
        {
            get => _showContourLabels;
            set => SetProperty(ref _showContourLabels, value);
        }
    }
}

