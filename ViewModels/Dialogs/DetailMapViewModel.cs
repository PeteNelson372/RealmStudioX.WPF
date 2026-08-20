using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Models.Map;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using System.Windows.Input;
using SkiaSharp;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.Editor.Tools;

namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    public class DetailMapViewModel : ViewModelBase
    {
        private EditorController _editor;

        public ResizeMapResult? Result { get; private set; }

        public ICommand DetailMapCommand { get; }
        public ICommand CancelDetailCommand { get; }

        public event Action<bool?>? RequestClose;

        public DetailMapViewModel(EditorController editor, RealmStudioMap map)
        {
            _editor = editor;
            _mapToResize = map ?? throw new ArgumentNullException(nameof(map));
            DetailMapCommand = new RelayCommand(CreateDetailMap);
            CancelDetailCommand = new RelayCommand(Cancel);
        }

        private RealmStudioMap _mapToResize;

        public RealmStudioMap MapToResize
        {
            get => _mapToResize;
            set => SetProperty(ref _mapToResize, value);
        }

        private SKRect _selectedArea = SKRect.Empty;
        public SKRect SelectedArea
        {
            get => _selectedArea;
            set
            {
                SetProperty(ref _selectedArea, value);

                _selectedLeft = _selectedArea.Left;
                _selectedTop = _selectedArea.Top;
                _selectedRight = _selectedArea.Right;
                _selectedBottom = _selectedArea.Bottom;

                if (_editor.ActiveEditorTool is SelectionTool selectionTool)
                {
                    selectionTool.SelectedArea = _selectedArea;
                }
            }
        }

        private float _selectedLeft = 0;
        public float SelectedLeft
        {
            get => _selectedLeft;
            set
            {
                SetProperty(ref _selectedLeft, value);
                SelectedArea = new SKRect(SelectedLeft, SelectedTop, SelectedRight, SelectedBottom);
            }
        }

        private float _selectedTop = 0;
        public float SelectedTop
        {
            get => _selectedTop;
            set
            {
                SetProperty(ref _selectedTop, value);
                SelectedArea = new SKRect(SelectedLeft, SelectedTop, SelectedRight, SelectedBottom);
            }
        }

        private float _selectedRight = 0;
        public float SelectedRight
        {
            get => _selectedRight;
            set
            {
                SetProperty(ref _selectedRight, value);
                SelectedArea = new SKRect(SelectedLeft, SelectedTop, SelectedRight, SelectedBottom);
            }
        }

        private float _selectedBottom = 0;
        public float SelectedBottom
        {
            get => _selectedBottom;
            set
            {
                SetProperty(ref _selectedBottom, value);
                SelectedArea = new SKRect(SelectedLeft, SelectedTop, SelectedRight, SelectedBottom);
            }
        }

        private ResizeMapAnchorPoint _resizeAnchorPoint = ResizeMapAnchorPoint.CenterZoomed;

        public ResizeMapAnchorPoint ResizeAnchorPoint
        {
            get => _resizeAnchorPoint;
        }

        private string _mapName = string.Empty;
        public string MapName
        {
            get => _mapName;
            set => SetProperty(ref _mapName, value);
        }

        private int _detailMapWidth = 1920;
        public int DetailMapWidth
        {
            get => _detailMapWidth;
            set => SetProperty(ref _detailMapWidth, value);
        }

        private int _detailMapHeight = 1080;
        public int DetailMapHeight
        {
            get => _detailMapHeight;
            set => SetProperty(ref _detailMapHeight, value);
        }

        private float _aspectRatio = (float)1920 / 1080;
        public float AspectRatio
        {
            get => _aspectRatio;
            set => SetProperty(ref _aspectRatio, value);
        }

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

        private bool _includeMarkerSymbols = true;
        public bool IncludeMarkerSymbols
        {
            get => _includeMarkerSymbols;
            set => SetProperty(ref _includeMarkerSymbols, value);
        }

        private bool _includeLabels = true;
        public bool IncludeLabels
        {
            get => _includeLabels;
            set => SetProperty(ref _includeLabels, value);
        }

        private bool _includeBoxes = true;
        public bool IncludeBoxes
        {
            get => _includeBoxes;
            set => SetProperty(ref _includeBoxes, value);
        }

        private bool _includePaths = true;
        public bool IncludePaths
        {
            get => _includePaths;
            set => SetProperty(ref _includePaths, value);
        }

        private bool _includeScale = true;
        public bool IncludeScale
        {
            get => _includeScale;
            set => SetProperty(ref _includeScale, value);
        }

        private bool _includeGrid = true;
        public bool IncludeGrid
        {
            get => _includeGrid;
            set => SetProperty(ref _includeGrid, value);
        }

        private bool _includeRegions = true;
        public bool IncludeRegions
        {
            get => _includeRegions;
            set => SetProperty(ref _includeRegions, value);
        }

        private bool _includeDrawnShapes = true;
        public bool IncludeDrawnShapes
        {
            get => _includeDrawnShapes;
            set => SetProperty(ref _includeDrawnShapes, value);
        }

        private bool _includeHeightMap = true;
        public bool IncludeHeightMap
        {
            get => _includeHeightMap;
            set => SetProperty(ref _includeHeightMap, value);
        }

        private void CreateDetailMap()
        {
            Result = new ResizeMapResult()
            {
                Map = MapToResize,
                AnchorPoint = ResizeAnchorPoint,
                Width = DetailMapWidth,
                Height = DetailMapHeight,
                SelectedArea = new SKRect(SelectedLeft, SelectedTop, SelectedRight, SelectedBottom),
                IncludeTerrainSymbols = IncludeTerrainSymbols,
                IncludeVegetationSymbols = IncludeVegetationSymbols,
                IncludeStructureSymbols = IncludeStructureSymbols,
                IncludeMarkerSymbols = IncludeMarkerSymbols,
                IncludeLabels = IncludeLabels,
                IncludeBoxes = IncludeBoxes,
                IncludePaths = IncludePaths,
                IncludeScale = IncludeScale,
                IncludeGrid = IncludeGrid,
                IncludeRegions = IncludeRegions,
                IncludeDrawnShapes = IncludeDrawnShapes,
                IncludeHeightMap = IncludeHeightMap,
            };

            RequestClose?.Invoke(true);
        }

        // -------------------------
        // CANCEL
        // -------------------------
        private void Cancel()
        {
            Result = null;
            RequestClose?.Invoke(false);
        }
    }
}
