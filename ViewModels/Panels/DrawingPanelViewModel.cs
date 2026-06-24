using RealmStudioShapeRenderingLib;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.Editor.Tools;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Controls;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class DrawingPanelViewModel : ViewModelBase, IDrawingSettings
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        public MainWindowViewModel MainViewModel => _mainWindowViewModel;

        private readonly EditorController _editor;
        public EditorController Editor => _editor;

        private readonly AssetManager _assetManager;

        public AssetBrowserViewModel TextureBrowser { get; }

        public AssetBrowser BrushBrowser { get; }

        public DrawingPanelViewModel(MainWindowViewModel mainViewModel, EditorController editor, AssetManager assetManager)
        {
            _mainWindowViewModel = mainViewModel;
            _editor = editor;
            _assetManager = assetManager;

            SelectedDrawableMapLayer =
                DrawableMapLayers.FirstOrDefault(x => x.Index == MapBuilder.DRAWINGLAYER);

            _editor.ActiveDrawingLayerChanged += OnActiveDrawingLayerChanged;

            AssetBrowser browser = new(
                    _assetManager,
                    [
                        AssetType.BackgroundTexture,
                        AssetType.HatchTexture,
                        AssetType.LandTexture,
                        AssetType.WaterTexture,
                    ]);

            TextureBrowser = new AssetBrowserViewModel(browser);

            TextureBrowser.TextureSelectionChanged += SelectedTextureChanged;

            BrushBrowser = new(_assetManager, AssetType.Brush);

            BuildBrushPatterns();

            SelectedTextureChanged();

            UpdateDrawingParameters();
            UpdatePreparedBrush();
        }

        private void SelectedTextureChanged()
        {
            CurrentSelectedTexture = TextureBrowser.CurrentImage;
            CurrentSelectedTextureId = TextureBrowser.SelectedAssetId;
        }

        private void OnActiveDrawingLayerChanged(MapLayer layer)
        {
            SelectedDrawableMapLayer =
                DrawableMapLayers.FirstOrDefault(x => x.Index == layer.MapLayerOrder);
        }

        public ObservableCollection<BrushPatternItem> BrushPatterns { get; } = [];

        private BrushPatternItem? _selectedBrushPattern;

        public BrushPatternItem? SelectedBrushPattern
        {
            get => _selectedBrushPattern;
            set
            {
                SetProperty(ref _selectedBrushPattern, value);
                BrushSpacing = value?.BrushDefinition?.BrushSpacing ?? 10;
                UpdateDrawingParameters();
                UpdatePreparedBrush();
            }
        }

        public ObservableCollection<DrawableMapLayerItem> DrawableMapLayers { get; } = [];

        private DrawableMapLayerItem? _selectedDrawableMapLayer;

        public DrawableMapLayerItem? SelectedDrawableMapLayer
        {
            get => _selectedDrawableMapLayer;
            set
            {
                SetProperty(ref _selectedDrawableMapLayer, value);

                if (_selectedDrawableMapLayer != null)
                {
                    MapLayer layer = MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, _selectedDrawableMapLayer!.Index);

                    if (_selectedDrawableMapLayer.Index != _editor.ActiveDrawingLayer!.MapLayerOrder)
                    {
                        _editor.SetActiveDrawingLayer(layer);
                        UpdateDrawingParameters();
                    }
                }
            }
        }

        // drawing/painting color

        private Color _drawingColor = Colors.Black;
        public Color DrawingColor
        {
            get => _drawingColor;
            set
            {
                if (SetProperty(ref _drawingColor, value))
                {
                    _drawingColorBrush.Color = value;
                    UpdateDrawingParameters();
                    UpdatePreparedBrush();
                }
            }
        }

        private SolidColorBrush _drawingColorBrush = new(Colors.Black);
        public Brush DrawingColorBrush => _drawingColorBrush;

        // fill color

        private Color _fillColor = Colors.Transparent;
        public Color FillColor
        {
            get => _fillColor;
            set
            {
                if (SetProperty(ref _fillColor, value))
                {
                    _fillColorBrush.Color = value;
                    UpdateDrawingParameters();
                }
            }
        }

        private SolidColorBrush _fillColorBrush = new(Colors.Transparent);
        public Brush FillColorBrush => _fillColorBrush;

        // line/brush size

        public int MinLineBrushSize { get; } = 1;
        public int MaxLineBrushSize { get; } = 256;

        private int _lineBrushSize = 8;
        public int LineBrushSize
        {
            get => _lineBrushSize;
            set
            {
                var clamped = Math.Clamp(value, MinLineBrushSize, MaxLineBrushSize);

                if (_lineBrushSize != clamped)
                {
                    _lineBrushSize = clamped;
                    OnPropertyChanged();

                    // drawing parameter update for LineBrushSize is handled
                    // when the user stops dragging the slider, to avoid excessive updates while dragging
                    // see LineBrushSizeSlider_Loaded in DrawingPanel.xaml.cs for details
                }
            }
        }

        // fill texture
        private string? _currentSelectedTextureId = string.Empty;

        public string? CurrentSelectedTextureId
        {
            get => _currentSelectedTextureId;
            set
            {
                if (_currentSelectedTextureId != value)
                {
                    _currentSelectedTextureId = value;
                    UpdateDrawingParameters();
                }
            }
        }

        private SKImage? _currentSelectedTexture;

        public SKImage? CurrentSelectedTexture
        {
            get => _currentSelectedTexture;
            set
            {
                if (_currentSelectedTexture != value)
                {
                    _currentSelectedTexture = value;
                    UpdateDrawingParameters();
                }
            }
        }

        // fill texture opacity

        public float MinTextureOpacity { get; } = 0;
        public float MaxTextureOpacity { get; } = 1.0f;

        private float _textureOpacity = 1.0f;
        public float TextureOpacity
        {
            get => _textureOpacity;
            set
            {
                var clamped = Math.Clamp(value, MinTextureOpacity, MaxTextureOpacity);

                if (_textureOpacity != clamped)
                {
                    _textureOpacity = clamped;
                    OnPropertyChanged();
                    UpdateDrawingParameters();
                }
            }
        }

        // fill texture scale

        public float MinTextureScale { get; } = 0;
        public float MaxTextureScale { get; } = 1.0f;

        private float _textureScale = 1.0f;
        public float TextureScale
        {
            get => _textureScale;
            set
            {
                var clamped = Math.Clamp(value, MinTextureScale, MaxTextureScale);

                if (_textureScale != clamped)
                {
                    _textureScale = clamped;
                    OnPropertyChanged();
                    UpdateDrawingParameters();
                }
            }
        }

        private DrawingFillType _selectedShapeFillType = DrawingFillType.None;

        public DrawingFillType SelectedShapeFillType
        {
            get => _selectedShapeFillType;
            set => SetProperty(ref _selectedShapeFillType, value);
        }


        // brush spacing

        public int MinBrushSpacing { get; } = 1;
        public int MaxBrushSpacing { get; } = 5000;

        private int _brushSpacing = 10;
        public int BrushSpacing
        {
            get => _brushSpacing;
            set
            {
                var clamped = Math.Clamp(value, MinBrushSpacing, MaxBrushSpacing);

                if (_brushSpacing != clamped)
                {
                    _brushSpacing = clamped;
                    OnPropertyChanged();
                    UpdateDrawingParameters();
                }
            }
        }

        // stamp scale

        public float MinStampScale { get; } = 0.1f;
        public float MaxStampScale { get; } = 1.0f;

        private float _stampScale = 1.0f;
        public float StampScale
        {
            get => _stampScale;
            set
            {
                var clamped = Math.Clamp(value, MinStampScale, MaxStampScale);

                if (_stampScale != clamped)
                {
                    _stampScale = clamped;
                    OnPropertyChanged();
                    UpdateDrawingParameters();
                }
            }
        }

        // stamp rotation

        public int MinStampRotation { get; } = 0;
        public int MaxStampRotation { get; } = 359;

        private int _stampRotation = 0;
        public int StampRotation
        {
            get => _stampRotation;
            set
            {
                var clamped = Math.Clamp(value, MinStampRotation, MaxStampRotation);

                if (_stampRotation != clamped)
                {
                    _stampRotation = clamped;
                    OnPropertyChanged();
                    UpdateDrawingParameters();

                }
            }
        }

        // stamp opacity

        public float MinStampOpacity { get; } = 0;
        public float MaxStampOpacity { get; } = 1.0f;

        private float _stampOpacity = 1.0f;
        public float StampOpacity
        {
            get => _stampOpacity;
            set
            {
                var clamped = Math.Clamp(value, MinStampOpacity, MaxStampOpacity);

                if (_stampOpacity != clamped)
                {
                    _stampOpacity = clamped;
                    OnPropertyChanged();
                    UpdateDrawingParameters();
                }
            }
        }

        // shape rotation

        public int MinShapeRotation { get; } = 0;
        public int MaxShapeRotation { get; } = 359;

        private int _shapeRotation = 0;
        public int ShapeRotation
        {
            get => _shapeRotation;
            set
            {
                var clamped = Math.Clamp(value, MinShapeRotation, MaxShapeRotation);

                if (_shapeRotation != clamped)
                {
                    _shapeRotation = clamped;
                    OnPropertyChanged();
                    UpdateDrawingParameters();
                }
            }
        }

        private string? _selectedStampPath;

        public string? SelectedStampPath
        {
            get => _selectedStampPath;
            set
            {
                SetProperty(ref _selectedStampPath, value);
                UpdateDrawingParameters();
            }
        }

        private BitmapImage? _stampImage;

        public BitmapImage? StampImage
        {
            get => _stampImage;
            set
            {
                SetProperty(ref _stampImage, value);
                UpdateDrawingParameters();
            }
        }

        private bool _isBrushPopupOpen;

        public bool IsBrushPopupOpen
        {
            get => _isBrushPopupOpen;

            set => SetProperty(ref _isBrushPopupOpen, value);
        }

        // commands

        public ICommand SelectCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.ShapeSelect);
            _editor.ActivateTool(EditorToolType.SelectionTool);
        });

        public ICommand SelectBrushPatternCommand =>
            new RelayCommand(
                parameter =>
                {
                    if (parameter is not BrushPatternItem pattern)
                    {
                        return;
                    }

                    SelectedBrushPattern = pattern;

                    IsBrushPopupOpen = false;
                },
                usesParameter: true);

        public ICommand DrawCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawingLine);
            _editor.ActivateTool(EditorToolType.DrawingTool, (IDrawingSettings)this);
        });

        public ICommand PaintCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawingPaint);
            _editor.ActivateTool(EditorToolType.DrawingTool, (IDrawingSettings)this);
        });

        public ICommand PlaceRectangleCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawingRectangle);
            _editor.ActivateTool(EditorToolType.DrawingTool, (IDrawingSettings)this);
        });

        public ICommand PlaceEllipseCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawingEllipse);
            _editor.ActivateTool(EditorToolType.DrawingTool, (IDrawingSettings)this);
        });

        public ICommand PlacePolygonCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawingPolygon);
            _editor.ActivateTool(EditorToolType.DrawingTool, (IDrawingSettings)this);
        });

        public ICommand PlaceStampCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawingStamp);
            _editor.ActivateTool(EditorToolType.DrawingTool, (IDrawingSettings)this);
        });

        public ICommand EraseDrawingCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawingErase);
            _editor.ActivateTool(EditorToolType.DrawingTool, (IDrawingSettings)this);
        });

        public ICommand PixelEditCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawingPixelEdit);
            _editor.ActivateTool(EditorToolType.DrawingTool, (IDrawingSettings)this);
        });

        public ICommand SelectStampCommand => new RelayCommand(() =>
        {
            SelectedStampPath = UserInterfaceUtilities.SelectBitmapFile();

            if (!string.IsNullOrEmpty(SelectedStampPath))
            {
                // display the selected stamp in the image box
                BitmapImage image = UserInterfaceUtilities.LoadBitmapImage(SelectedStampPath);
                StampImage = image;
            }
        });

        public ICommand PlaceRoundedRectangleCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawingRoundedRectangle);
            _editor.ActivateTool(EditorToolType.DrawingTool, (IDrawingSettings)this);
        });

        public ICommand PlaceTriangleCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawingTriangle);
            _editor.ActivateTool(EditorToolType.DrawingTool, (IDrawingSettings)this);
        });

        public ICommand PlaceRightTriangleCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawingRightTriangle);
            _editor.ActivateTool(EditorToolType.DrawingTool, (IDrawingSettings)this);
        });

        public ICommand PlaceDiamondCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawingDiamond);
            _editor.ActivateTool(EditorToolType.DrawingTool, (IDrawingSettings)this);
        });

        public ICommand PlacePentagonCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawingPentagon);
            _editor.ActivateTool(EditorToolType.DrawingTool, (IDrawingSettings)this);
        });

        public ICommand PlaceHexagonCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawingHexagon);
            _editor.ActivateTool(EditorToolType.DrawingTool, (IDrawingSettings)this);
        });

        public ICommand PlaceArrowCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawingArrow);
            _editor.ActivateTool(EditorToolType.DrawingTool, (IDrawingSettings)this);
        });

        public ICommand PlaceFivePointStarCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawingFivePointStar);
            _editor.ActivateTool(EditorToolType.DrawingTool, (IDrawingSettings)this);
        });

        public ICommand PlaceSixPointStarCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawingSixPointStar);
            _editor.ActivateTool(EditorToolType.DrawingTool, (IDrawingSettings)this);
        });

        // private methods

        private void BuildBrushPatterns()
        {
            List<MapBrush> brushes = _assetManager.MapBrushes;

            foreach (MapBrush brush in brushes)
            {
                var bpi = CreateBrushPattern(brush);

                if (bpi != null)
                {
                    BrushPatterns.Add(bpi);
                }
            }

            SelectedBrushPattern = BrushPatterns.FirstOrDefault(b => string.Equals(
                b.Name,
                "Soft Round",
                StringComparison.OrdinalIgnoreCase));
        }

        private BrushPatternItem? CreateBrushPattern(MapBrush brush)
        {
            if (brush.BrushBitmaps != null && brush.BrushBitmaps.Count > 0)
            {
                return new BrushPatternItem
                {
                    Name = brush.BrushName,

                    BrushDefinition = brush,

                    PreviewImage = brush.BrushBitmaps[0]?.Copy().ToImageSource()
                };
            }

            return null;
        }

        public void UpdateDrawingParameters()
        {
            DrawingTool? dt = (DrawingTool?)_editor.ActivateTool(EditorToolType.DrawingTool, this);

            if (dt != null)
            {
                dt.UpdateDrawingParameters((IDrawingSettings)this);
            }
        }

        public void UpdatePreparedBrush()
        {
            DrawingTool? dt = (DrawingTool?)_editor.ActivateTool(EditorToolType.DrawingTool, this);

            if (dt != null)
            {
                PreparedBrush newPreparedBrush = new()
                {
                    SourceBrush = SelectedBrushPattern?.BrushDefinition,
                    Color = DrawingColor.ToSKColor(),
                    BrushSize = LineBrushSize,
                    BrushSpacing = BrushSpacing,
                };

                AssetInitializer.GetPreparedBrushBitmaps(newPreparedBrush);
                dt.CurrentPreparedBrush = newPreparedBrush;
            }
        }
    }

    public interface IDrawingSettings
    {
        BrushPatternItem? SelectedBrushPattern { get; }
        DrawableMapLayerItem? SelectedDrawableMapLayer { get; }
        Color DrawingColor { get; }
        Color FillColor { get; }
        int LineBrushSize { get; }
        string? CurrentSelectedTextureId { get; }
        SKImage? CurrentSelectedTexture { get; }
        float TextureOpacity { get; }
        float TextureScale { get; }
        DrawingFillType SelectedShapeFillType { get; }
        int BrushSpacing { get; }
        float StampScale { get; }
        int StampRotation { get; }
        float StampOpacity { get; }
        int ShapeRotation { get; }
        string? SelectedStampPath { get; }
        BitmapImage? StampImage { get; }
    }

    public class BrushPatternItem
    {
        public string Name { get; set; } = "";

        public ImageSource? PreviewImage { get; set; }

        public MapBrush? BrushDefinition { get; set; }
    }

    public class DrawableMapLayerItem
    {
        public string Name { get; set; } = "";

        public int Index { get; set; }
    }
}

