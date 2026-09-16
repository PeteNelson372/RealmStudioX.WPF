using RealmStudioShapeRenderingLib;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Dialogs;
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
        }

        MapHeightMap? _currentHeightMap = null;

        public void SetCurrentHeightMap()
        {
            MapLayer heightMapLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.HEIGHTMAPLAYER);

            if (heightMapLayer.Shapes.Count > 0)
            {
                _currentHeightMap = (MapHeightMap)heightMapLayer.Shapes[0];

                MinimumElevation = _currentHeightMap.MinimumElevation;
                MaximumElevation = _currentHeightMap.MaximumElevation;
                ElevationUnit = _currentHeightMap.ElevationUnit;

                SelectedPalette = _currentHeightMap.HeightMapPalette;

                _currentHeightMap.RebuildHypsometricColorLookup();
            }
        }

        public int MinHeightMapBrushSize { get; } = 4;
        public int MaxHeightMapBrushSize { get; } = 256;

        private int _heightMapBrushSize = 64;
        public int HeightMapBrushSize
        {
            get => _heightMapBrushSize;
            set
            {
                var clamped = Math.Clamp(value, MinHeightMapBrushSize, MaxHeightMapBrushSize);

                _heightMapBrushSize = clamped;

                OnPropertyChanged();
            }
        }

        private float _minimumElevation = -5000;
        public float MinimumElevation
        {
            get { return _minimumElevation; }
            set
            {
                if (value < _maximumElevation)
                {
                    SetProperty(ref _minimumElevation, value);

                    if (_currentHeightMap != null)
                    {
                        UpdateHeightMapProperties(_currentHeightMap);
                    }
                }
            }
        }

        private float _maximumElevation = 50000;
        public float MaximumElevation
        {
            get { return _maximumElevation; }
            set
            {
                if (value > _minimumElevation)
                {
                    SetProperty(ref _maximumElevation, value);

                    if (_currentHeightMap != null)
                    {
                        UpdateHeightMapProperties(_currentHeightMap);
                    }
                }
            }
        }

        private string _elevationUnit = "Feet";
        public string ElevationUnit
        {
            get => _elevationUnit;
            set
            {
                SetProperty(ref _elevationUnit, value);

                if (_currentHeightMap != null)
                {
                    UpdateHeightMapProperties(_currentHeightMap);
                }
            }
        }

        public float MinElevationChange { get; } = 1.0f;
        public float MaxElevationChange { get; } = 1000.0f;

        private float _elevationChange = 100.0f;

        public float ElevationChange
        {
            get => _elevationChange;
            set
            {
                var clamped = Math.Clamp(value, MinElevationChange, MaxElevationChange);
                SetProperty(ref _elevationChange, clamped);

                if (_currentHeightMap != null)
                {
                    UpdateHeightMapProperties(_currentHeightMap);
                }
            }
        }


        public int MinElevationScale { get; } = 1;
        public int MaxElevationScale { get; } = 1000;

        private int _elevationScale = 200;

        public int ElevationScale
        {
            get => _elevationScale;
            set
            {
                var clamped = Math.Clamp(value, MinElevationScale, MaxElevationScale);
                SetProperty(ref _elevationScale, clamped);

                if (_currentHeightMap != null)
                {
                    UpdateHeightMapProperties(_currentHeightMap);
                }
            }
        }

        public int MinSmoothingStrength { get; } = 1;
        public int MaxSmoothingStrength { get; } = 25;

        private int _smoothingStrength = 10;

        public int SmoothingStrength
        {
            get => _smoothingStrength;
            set
            {
                var clamped = Math.Clamp(value, MinSmoothingStrength, MaxSmoothingStrength);
                SetProperty(ref _smoothingStrength, clamped);
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

                        if (_currentHeightMap != null)
                        {
                            UpdateHeightMapProperties(_currentHeightMap);
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

        public ICommand IncreaseElevationCommand => new RelayCommand(() =>
        {
            if (_mainViewModel.RenderHeightMap && _editor.Scene != null)
            {
                _editor.SetDrawingMode(MapDrawingMode.MapHeightIncrease);

                _editor.ActivateTool(EditorToolType.HeightMapTool);
            }
        });

        public ICommand DecreaseElevationCommand => new RelayCommand(() =>
        {
            if (_mainViewModel.RenderHeightMap && _editor.Scene != null)
            {
                _editor.SetDrawingMode(MapDrawingMode.MapHeightDecrease);

                _editor.ActivateTool(EditorToolType.HeightMapTool);
            }
        });

        public ICommand SmoothElevationCommand => new RelayCommand(() =>
        {
            if (_mainViewModel.RenderHeightMap && _editor.Scene != null)
            {
                _editor.SetDrawingMode(MapDrawingMode.MapHeightSmooth);

                _editor.ActivateTool(EditorToolType.HeightMapTool);
            }
        });

        private void UpdateHeightMapProperties(MapHeightMap heightMap)
        {
            heightMap.MinimumElevation = MinimumElevation;
            heightMap.MaximumElevation = MaximumElevation;
            heightMap.ElevationUnit = ElevationUnit;
            heightMap.HeightMapPalette = _selectedPalette;
            heightMap.RebuildHypsometricColorLookup();
        }

        public ICommand Open3DViewCommand => new RelayCommand(() =>
        {
            if (_mainViewModel.RenderHeightMap && _editor.Scene != null && _currentHeightMap != null)
            {
                HeightMapDXModelViewer heightMap3DViewer = new(_mainViewModel, _currentHeightMap,
                    MinimumElevation,
                    MaximumElevation,
                    ElevationScale);

                heightMap3DViewer.Show();
            }
        });

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

