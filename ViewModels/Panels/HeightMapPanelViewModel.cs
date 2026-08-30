using RealmStudioShapeRenderingLib;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.Editor.Tools;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using System.IO;
using System.Windows.Input;

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
                        hmt.ActiveHeightMap.MinimumHeight = MinimumHeight;
                        hmt.ActiveHeightMap.MaximumHeight = MaximumHeight;
                        hmt.ActiveHeightMap.HeightUnit = HeightUnit;
                        hmt.ActiveHeightMap.HeightMapPalette = _selectedPalette;
                        hmt.ActiveHeightMap.RebuildHypsometricColorLookup();
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
                        hmt.ActiveHeightMap.MinimumHeight = MinimumHeight;
                        hmt.ActiveHeightMap.MaximumHeight = MaximumHeight;
                        hmt.ActiveHeightMap.HeightUnit = HeightUnit;
                        hmt.ActiveHeightMap.HeightMapPalette = _selectedPalette;
                        hmt.ActiveHeightMap.RebuildHypsometricColorLookup();
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
                    hmt.ActiveHeightMap.MinimumHeight = MinimumHeight;
                    hmt.ActiveHeightMap.MaximumHeight = MaximumHeight;
                    hmt.ActiveHeightMap.HeightUnit = HeightUnit;
                    hmt.ActiveHeightMap.HeightMapPalette = _selectedPalette;
                    hmt.ActiveHeightMap.RebuildHypsometricColorLookup();
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
                    hmt.ActiveHeightMap.MinimumHeight = MinimumHeight;
                    hmt.ActiveHeightMap.MaximumHeight = MaximumHeight;
                    hmt.ActiveHeightMap.HeightUnit = HeightUnit;
                    hmt.ActiveHeightMap.HeightMapPalette = _selectedPalette;
                    hmt.ActiveHeightMap.RebuildHypsometricColorLookup();
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
                            hmt.ActiveHeightMap.MinimumHeight = MinimumHeight;
                            hmt.ActiveHeightMap.MaximumHeight = MaximumHeight;
                            hmt.ActiveHeightMap.HeightUnit = HeightUnit;
                            hmt.ActiveHeightMap.HeightMapPalette = _selectedPalette;
                            hmt.ActiveHeightMap.RebuildHypsometricColorLookup();
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
    }
}

