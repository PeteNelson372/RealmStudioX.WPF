
using RealmStudioShapeRenderingLib;
using RealmStudioX.WorldGeneration.LandformGeneration;
using RealmStudioX.WorldGeneration.LandformGeneration.Islands;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.Editor.Tools;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using SkiaSharp;
using System.Windows.Input;

namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    public class LandformGenerationViewModel : ViewModelBase
    {
        private readonly EditorController _editor;

        private RealmStudioMap _worldGenerationMap;

        public RealmStudioMap WorldGenerationMap
        {
            get => _worldGenerationMap;
            set => SetProperty(ref _worldGenerationMap, value);
        }

        public event Action<bool?>? RequestClose;

        public LandformGenerationResult? Result { get; private set; }

        // -------------------------------------------------
        // Commands
        // -------------------------------------------------

        public ICommand GenerateCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand RandomizeSeedCommand { get; }


        // -------------------------------------------------
        // Constructor
        // -------------------------------------------------

        public LandformGenerationViewModel(
            EditorController editor,
            RealmStudioMap map,
            SKRect selectedArea)
        {
            _editor = editor
                ?? throw new ArgumentNullException(nameof(editor));

            _worldGenerationMap = map
                ?? throw new ArgumentNullException(nameof(map));

            SelectedArea = selectedArea;

            GenerateCommand = new RelayCommand(Generate);

            CancelCommand = new RelayCommand(Cancel);

            RandomizeSeedCommand = new RelayCommand(RandomizeSeed);
        }

        // -------------------------------------------------
        // Landform type
        // -------------------------------------------------

        public IReadOnlyList<GeneratedLandformType> LandformTypes { get; } =
            Enum.GetValues<GeneratedLandformType>()
                .Where(t => t != GeneratedLandformType.NotSet)
                .ToArray();

        private GeneratedLandformType _selectedLandformType =
            GeneratedLandformType.Island;

        public GeneratedLandformType SelectedLandformType
        {
            get => _selectedLandformType;
            set
            {
                if (!SetProperty(ref _selectedLandformType, value))
                    return;

                OnPropertyChanged(nameof(IsIslandSelected));
                OnPropertyChanged(nameof(GenerationTypeStatus));
                OnPropertyChanged(nameof(CanGenerate));
                OnPropertyChanged(nameof(GenerationStatus));
            }
        }

        public bool IsIslandSelected =>
            SelectedLandformType == GeneratedLandformType.Island;

        public string GenerationTypeStatus =>
            SelectedLandformType switch
            {
                GeneratedLandformType.Island =>
                    "Generate a single irregular island within the selected map area.",

                GeneratedLandformType.Region =>
                    "Region generation is not yet implemented.",

                GeneratedLandformType.Continent =>
                    "Continent generation is not yet implemented.",

                GeneratedLandformType.Archipelago =>
                    "Archipelago generation is not yet implemented.",

                GeneratedLandformType.Atoll =>
                    "Atoll generation is not yet implemented.",

                GeneratedLandformType.World =>
                    "World generation is not yet implemented.",

                GeneratedLandformType.Icecap =>
                    "Icecap generation is not yet implemented.",

                _ =>
                    "Select a landform type."
            };

        // -------------------------------------------------
        // Generation area
        // -------------------------------------------------

        private SKRect _selectedArea = SKRect.Empty;

        public SKRect SelectedArea
        {
            get => _selectedArea;
            set
            {
                if (!SetProperty(ref _selectedArea, value))
                    return;

                _selectedLeft = value.Left;
                _selectedTop = value.Top;
                _selectedRight = value.Right;
                _selectedBottom = value.Bottom;

                OnPropertyChanged(nameof(SelectedLeft));
                OnPropertyChanged(nameof(SelectedTop));
                OnPropertyChanged(nameof(SelectedRight));
                OnPropertyChanged(nameof(SelectedBottom));

                OnPropertyChanged(nameof(GenerationAreaWidth));
                OnPropertyChanged(nameof(GenerationAreaHeight));
                OnPropertyChanged(nameof(GenerationAreaDimensions));
                OnPropertyChanged(nameof(CanGenerate));
                OnPropertyChanged(nameof(GenerationStatus));

                UpdateSelectionTool();
            }
        }

        private float _selectedLeft;

        public float SelectedLeft
        {
            get => _selectedLeft;
            set
            {
                if (SetProperty(ref _selectedLeft, value))
                    UpdateSelectedArea();
            }
        }

        private float _selectedTop;

        public float SelectedTop
        {
            get => _selectedTop;
            set
            {
                if (SetProperty(ref _selectedTop, value))
                    UpdateSelectedArea();
            }
        }

        private float _selectedRight;

        public float SelectedRight
        {
            get => _selectedRight;
            set
            {
                if (SetProperty(ref _selectedRight, value))
                    UpdateSelectedArea();
            }
        }

        private float _selectedBottom;

        public float SelectedBottom
        {
            get => _selectedBottom;
            set
            {
                if (SetProperty(ref _selectedBottom, value))
                    UpdateSelectedArea();
            }
        }

        public float GenerationAreaWidth => SelectedArea.Width;

        public float GenerationAreaHeight => SelectedArea.Height;

        public string GenerationAreaDimensions =>
            SelectedArea.Width > 0 && SelectedArea.Height > 0
                ? $"Area: {SelectedArea.Width:N0} × {SelectedArea.Height:N0} map units"
                : "Enter a valid generation area.";

        private void UpdateSelectedArea()
        {
            SelectedArea = new SKRect(
                SelectedLeft,
                SelectedTop,
                SelectedRight,
                SelectedBottom);
        }

        private void UpdateSelectionTool()
        {
            if (_editor.ActiveEditorTool is SelectionTool selectionTool)
            {
                selectionTool.SelectedArea = SelectedArea;
            }
        }

        // -------------------------------------------------
        // Island generation options
        // -------------------------------------------------

        private double _islandSize = 0.85;

        public double IslandSize
        {
            get => _islandSize;
            set
            {
                value = double.IsFinite(value)
                    ? Math.Clamp(value, 0.1, 1.0)
                    : 0.85;

                SetProperty(ref _islandSize, value);
            }
        }

        private double _islandShapeIrregularity = 0.25;

        public double IslandShapeIrregularity
        {
            get => _islandShapeIrregularity;
            set
            {
                value = double.IsFinite(value)
                    ? Math.Clamp(value, 0.0, 1.0)
                    : 0.25;

                SetProperty(ref _islandShapeIrregularity, value);
            }
        }

        private double _islandCoastlineDetail = 0.25;

        public double IslandCoastlineDetail
        {
            get => _islandCoastlineDetail;
            set
            {
                value = double.IsFinite(value)
                    ? Math.Clamp(value, 0.0, 1.0)
                    : 0.25;

                SetProperty(ref _islandCoastlineDetail, value);
            }
        }

        private int _islandCoastlinePointCount = 256;

        public int IslandCoastlinePointCount
        {
            get => _islandCoastlinePointCount;
            set
            {
                value = Math.Clamp(value, 64, 8192);
                SetProperty(ref _islandCoastlinePointCount, value);
            }
        }

        // -------------------------------------------------
        // Generation seed
        // -------------------------------------------------

        private int _seed = 12345;

        public int Seed
        {
            get => _seed;
            set => SetProperty(ref _seed, Math.Max(0, value));
        }

        private void RandomizeSeed()
        {
            Seed = Random.Shared.Next(0, int.MaxValue);
        }

        // -------------------------------------------------
        // Elevation options
        // -------------------------------------------------

        private bool _includeHeightMap = true;

        public bool IncludeHeightMap
        {
            get => _includeHeightMap;
            set => SetProperty(ref _includeHeightMap, value);
        }

        // -------------------------------------------------
        // Additional feature options
        // -------------------------------------------------

        private bool _includeTerrainSymbols = true;

        public bool IncludeTerrainSymbols
        {
            get => _includeTerrainSymbols;
            set => SetProperty(ref _includeTerrainSymbols, value);
        }

        private bool _includeVegetationSymbols = true;

        public bool IncludeVegetationSymbols
        {
            get => _includeVegetationSymbols;
            set => SetProperty(ref _includeVegetationSymbols, value);
        }

        private bool _includeStructureSymbols = true;

        public bool IncludeStructureSymbols
        {
            get => _includeStructureSymbols;
            set => SetProperty(ref _includeStructureSymbols, value);
        }

        private bool _includeMarkerSymbols;

        public bool IncludeMarkerSymbols
        {
            get => _includeMarkerSymbols;
            set => SetProperty(ref _includeMarkerSymbols, value);
        }

        private bool _includeRiversAndLakes = true;

        public bool IncludeRiversAndLakes
        {
            get => _includeRiversAndLakes;
            set => SetProperty(ref _includeRiversAndLakes, value);
        }

        private bool _includePaths;

        public bool IncludePaths
        {
            get => _includePaths;
            set => SetProperty(ref _includePaths, value);
        }

        private bool _includeLabels;

        public bool IncludeLabels
        {
            get => _includeLabels;
            set => SetProperty(ref _includeLabels, value);
        }

        // -------------------------------------------------
        // Generation state
        // -------------------------------------------------

        public bool CanGenerate =>
            SelectedLandformType == GeneratedLandformType.Island &&
            HasValidGenerationArea;

        private bool HasValidGenerationArea =>
            float.IsFinite(SelectedArea.Left) &&
            float.IsFinite(SelectedArea.Top) &&
            float.IsFinite(SelectedArea.Right) &&
            float.IsFinite(SelectedArea.Bottom) &&
            SelectedArea.Width > 0 &&
            SelectedArea.Height > 0;

        public string GenerationStatus =>
            !HasValidGenerationArea
                ? "Specify a valid generation area."
                : SelectedLandformType != GeneratedLandformType.Island
                    ? "The selected generation type is not yet available."
                    : "Ready to generate.";


        // -------------------------------------------------
        // GENERATE
        // -------------------------------------------------

        private void Generate()
        {
            if (!CanGenerate)
                return;

            // Currently, Island is the only implemented generator.

            LandformGenerationOptions options = new()
            {
                Seed = Seed,
                Size = (float)IslandSize,
                ShapeIrregularity = (float)IslandShapeIrregularity,
                CoastlineDetail = (float)IslandCoastlineDetail,
                CoastlinePointCount = IslandCoastlinePointCount
            };

            IslandGeometryGenerator generator = new();

            Result?.Dispose();

            Result = generator.Generate(
                SelectedArea,
                options);

            OnPropertyChanged(nameof(Result));

            RequestClose?.Invoke(true);
        }

        // -------------------------------------------------
        // CANCEL
        // -------------------------------------------------

        private void Cancel()
        {
            Result?.Dispose();
            Result = null;

            OnPropertyChanged(nameof(Result));

            RequestClose?.Invoke(false);
        }
    }
}
