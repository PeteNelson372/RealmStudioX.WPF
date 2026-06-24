using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class SymbolsPanelViewModel : ViewModelBase, ISymbolSettings
    {
        private readonly EditorController _editor;
        public EditorController Editor => _editor;

        private readonly AssetManager _assetManager;

        public List<string> SelectedCollections = [];
        public List<string> SelectedTags = [];

        public List<MapSymbolDefinition>? FilteredSymbols = null;

        public ObservableCollection<SymbolGridItem> SymbolGridItems { get; } = [];

        public List<MapSymbolCollection> SymbolCollections { get; set; }

        public ObservableCollection<CheckableItemViewModel<MapSymbolCollection>> Collections { get; } = [];

        public List<string>? SymbolTags { get; set; }
        public ObservableCollection<CheckableItemViewModel<string>> Tags { get; } = [];

        public SymbolsPanelViewModel(EditorController editor, AssetManager assetManager)
        {
            _editor = editor;
            _assetManager = assetManager;

            // initialize members that require _assetManager
            SymbolCollections = _assetManager.SymbolCollections;

            _editor.SymbolSelectionService.SelectionChanged += () =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var item in SymbolGridItems)
                    {
                        item.IsPrimary =
                            _editor.SymbolSelectionService.PrimarySelectedSymbol?.Id == item.SymbolDefinition.Id;

                        item.IsSecondary =
                            _editor.SymbolSelectionService.SecondarySelectedSymbols
                                .Any(s => s.Id == item.SymbolDefinition.Id);
                    }

                    System.Windows.Data.CollectionViewSource
                        .GetDefaultView(SymbolGridItems)
                        .Refresh();
                });
            };

            Collections.Clear();

            foreach (var c in SymbolCollections)
            {
                var item = new CheckableItemViewModel<MapSymbolCollection>(c, x => x.Name);

                item.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(CheckableItemViewModel<MapSymbolCollection>.IsChecked))
                    {
                        // Update selected collections
                        SelectedCollections.Clear();

                        foreach (var selected in Collections.Where(x => x.IsChecked))
                        {
                            SelectedCollections.Add(selected.Value.Name);
                        }

                        AddGridItems();
                    }
                };

                Collections.Add(item);
            }

            SymbolTags = AssetManager.SymbolTags.ToList();

            Tags.Clear();

            if (SymbolTags != null)
            {
                foreach (var t in SymbolTags)
                {
                    var item = new CheckableItemViewModel<string>(t);

                    item.PropertyChanged += (_, e) =>
                    {
                        if (e.PropertyName == nameof(CheckableItemViewModel<MapSymbolCollection>.IsChecked))
                        {
                            // Update selected tags
                            SelectedTags.Clear();

                            foreach (var selected in Tags.Where(x => x.IsChecked))
                            {
                                SelectedTags.Add(selected.Value);
                            }

                            AddGridItems();
                        }
                    };

                    Tags.Add(item);
                }
            }

        }

        private MapSymbolType _selectedSymbolType = MapSymbolType.NotSet;
        public MapSymbolType SelectedSymbolType
        {
            get => _selectedSymbolType;
            set
            {
                if (value != _selectedSymbolType)
                {
                    _selectedSymbolType = value;
                    OnPropertyChanged();
                }
            }
        }

        public double MinSymbolScale { get; } = 0.01;
        public double MaxSymbolScale { get; } = 2.0;

        private double _symbolScale = 1.0;
        public double SymbolScale
        {
            get => _symbolScale;
            set
            {
                var clamped = Math.Clamp(value, MinSymbolScale, MaxSymbolScale);

                if (_symbolScale != clamped && !_symbolScaleLocked)
                {
                    _symbolScale = clamped;
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

        // use area brush

        private bool _useAreaBrush = false;
        public bool UseAreaBrush
        {
            get => _useAreaBrush;
            set
            {
                if (_useAreaBrush != value)
                {
                    _useAreaBrush = value;
                    OnPropertyChanged();
                }
            }
        }

        // area brush size

        private int _areaBrushSize = 64;
        public int AreaBrushSize
        {
            get => _areaBrushSize;
            set
            {
                if (_areaBrushSize != value)
                {
                    _areaBrushSize = value;
                    OnPropertyChanged();
                }
            }
        }

        // mirror symbol

        private bool _mirrorSymbol = false;
        public bool MirrorSymbol
        {
            get => _mirrorSymbol;
            set
            {
                if (_mirrorSymbol != value)
                {
                    _mirrorSymbol = value;
                    OnPropertyChanged();
                    SymbolValuesChanged();
                }
            }
        }

        // symbol rotation

        private int _symbolRotation = 0;
        public int SymbolRotation
        {
            get => _symbolRotation;
            set
            {
                if (_symbolRotation != value && value >= 0 && value <= 359)
                {
                    _symbolRotation = value;
                    OnPropertyChanged();
                    SymbolValuesChanged();
                }
            }
        }

        // symbol placement rate

        private double _symbolPlacementRate = 1.0;
        public double SymbolPlacementRate
        {
            get => _symbolPlacementRate;
            set
            {
                if (_symbolPlacementRate != value)
                {
                    _symbolPlacementRate = value;
                    OnPropertyChanged();
                }
            }
        }

        // symbol placement density

        private double _symbolPlacementDensity = 1.0;
        public double SymbolPlacementDensity
        {
            get => _symbolPlacementDensity;
            set
            {
                if (_symbolPlacementDensity != value)
                {
                    _symbolPlacementDensity = value;
                    OnPropertyChanged();
                }
            }
        }

        // symbol filter text

        private string _symbolFilterText = string.Empty;
        private string _filterText = string.Empty;
        public string SymbolFilterText
        {
            get => _symbolFilterText;
            set
            {
                if (_symbolFilterText != value)
                {
                    _symbolFilterText = value;
                    OnPropertyChanged();

                    if (_symbolFilterText.Length >= 3)
                    {
                        _filterText = _symbolFilterText;
                        AddGridItems(); // re-filter symbols
                    }
                    else
                    {
                        _filterText = string.Empty;
                        AddGridItems();
                    }
                }
            }
        }


        private void SymbolValuesChanged()
        {
            if (_assetManager == null)
                return;

            // apply changes to selected symbol
            _editor.UpdateSelectedSymbol((ISymbolSettings)this);
        }


        public ICommand SelectCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.ShapeSelect);
            _editor.ActivateTool(EditorToolType.SelectionTool);
        });

        public ICommand SymbolEraseCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.SymbolErase);
            _editor.ActivateTool(EditorToolType.SymbolTool, (ISymbolSettings)this);
        });

        public ICommand SymbolPaintCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.SymbolColor);
            _editor.ActivateTool(EditorToolType.SymbolTool, (ISymbolSettings)this);
        });

        public ICommand SelectStructuresCommand => new RelayCommand(() =>
        {
            SelectedSymbolType = MapSymbolType.Structure;

            _editor.SetDrawingMode(MapDrawingMode.SymbolPlace);
            _editor.ActivateTool(EditorToolType.SymbolTool, (ISymbolSettings)this);

            AddGridItems();

        });

        public ICommand SelectVegetationCommand => new RelayCommand(() =>
        {
            SelectedSymbolType = MapSymbolType.Vegetation;
            _editor.SetDrawingMode(MapDrawingMode.SymbolPlace);
            _editor.ActivateTool(EditorToolType.SymbolTool, (ISymbolSettings)this);

            AddGridItems();
        });

        public ICommand SelectTerrainCommand => new RelayCommand(() =>
        {
            SelectedSymbolType = MapSymbolType.Terrain;
            _editor.SetDrawingMode(MapDrawingMode.SymbolPlace);
            _editor.ActivateTool(EditorToolType.SymbolTool, (ISymbolSettings)this);

            AddGridItems();
        });

        public ICommand SelectMarkersCommand => new RelayCommand(() =>
        {
            SelectedSymbolType = MapSymbolType.Marker;
            _editor.SetDrawingMode(MapDrawingMode.SymbolPlace);
            _editor.ActivateTool(EditorToolType.SymbolTool, (ISymbolSettings)this);

            AddGridItems();
        });

        public ICommand SelectOtherCommand => new RelayCommand(() =>
        {
            SelectedSymbolType = MapSymbolType.Other;
            _editor.SetDrawingMode(MapDrawingMode.SymbolPlace);
            _editor.ActivateTool(EditorToolType.SymbolTool, (ISymbolSettings)this);

            AddGridItems();
        });

        public ICommand LockScaleCommand => new RelayCommand(() =>
        {
            SymbolScaleLocked = !SymbolScaleLocked;
        });

        public ICommand ResetColorsCommand => new RelayCommand(() =>
        {
            SymbolColor1 = Color.FromRgb(85, 44, 36);
            SymbolColor2 = Color.FromRgb(53, 45, 32);
            SymbolColor3 = Color.FromArgb(161, 214, 202, 171);
        });


        // symbol selection
        private SymbolQuery? _currentQuery;


        internal void AddGridItems()
        {
            FilteredSymbols = GetFilteredMapSymbols();

            SymbolGridItems.Clear();

            if (FilteredSymbols != null)
            {
                foreach (var symbol in FilteredSymbols)
                {
                    SKBitmap? pbm = _assetManager.SymbolThumbnailCache.GetOrCreate(symbol, 52);

                    if (pbm != null)
                    {
                        SymbolGridItem gridItem = new(symbol, pbm.ToImageSource(), _editor.SymbolSelectionService);
                        SymbolGridItems.Add(gridItem);
                    }
                }
            }
        }

        internal List<MapSymbolDefinition>? GetFilteredMapSymbols()
        {
            if (_selectedSymbolType == MapSymbolType.NotSet)
            {
                return [];
            }

            List<MapSymbolDefinition>? filteredSymbols = GetFilteredSymbolList(_selectedSymbolType, SelectedCollections, SelectedTags, _filterText);

            return filteredSymbols;
        }

        internal List<MapSymbolDefinition>? GetFilteredSymbolList(MapSymbolType selectedSymbolType, List<string> selectedCollections, List<string> selectedTags, string filterText = "")
        {
            SymbolQuery q = new()
            {
                Type = selectedSymbolType,
                Collections = selectedCollections,
                Tags = selectedTags,
                TextFilter = filterText,
            };

            _currentQuery = q.Clone();
            return (List<MapSymbolDefinition>?)_assetManager.QuerySymbols(q);
        }

        private static bool AreQueriesEqual(SymbolQuery q, SymbolQuery currentQuery)
        {
            return q.Type == currentQuery.Type
                && q.TextFilter == currentQuery.TextFilter
                && q.Collections.SequenceEqual(currentQuery.Collections)
                && q.Tags.SequenceEqual(currentQuery.Tags);
        }

        public class SymbolGridItem
        {
            private readonly SymbolSelectionService _selection;

            public ImageSource SymbolImage { get; }
            public MapSymbolDefinition SymbolDefinition { get; }

            public SymbolGridItem(MapSymbolDefinition def,
                      ImageSource image,
                      SymbolSelectionService selection)
            {
                SymbolDefinition = def;
                SymbolImage = image;
                _selection = selection;
            }

            private bool _isPrimary;
            public bool IsPrimary
            {
                get => _isPrimary;
                set
                {
                    if (_isPrimary != value)
                    {
                        _isPrimary = value;
                        OnPropertyChanged(nameof(IsPrimary));
                    }
                }
            }

            private bool _isSecondary;
            public bool IsSecondary
            {
                get => _isSecondary;
                set
                {
                    if (_isSecondary != value)
                    {
                        _isSecondary = value;
                        OnPropertyChanged(nameof(IsSecondary));
                    }
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string? name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public interface ISymbolSettings
    {
        MapSymbolType SelectedSymbolType { get; }
        double SymbolScale { get; }
        bool SymbolScaleLocked { get; }
        bool RandomizeSymbolColors { get; }
        Color SymbolColor1 { get; }
        Color SymbolColor2 { get; }
        Color SymbolColor3 { get; }
        bool UseAreaBrush { get; }
        int AreaBrushSize { get; }
        bool MirrorSymbol { get; }
        int SymbolRotation { get; }
        double SymbolPlacementRate { get; }
        double SymbolPlacementDensity { get; }
    }
}
