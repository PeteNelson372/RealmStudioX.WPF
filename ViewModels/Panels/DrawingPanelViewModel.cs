using RealmStudioShapeRenderingLib;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Controls;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.IO;
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

        public ObservableCollection<BoxGridItem> BoxItems { get; } = [];

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

            TextureBrowser.TextureSelectionChanged += DrawnShapeValuesChanged;

            BrushBrowser = new(_assetManager, AssetType.Brush);



            BuildBrushPatterns();

        }

        private void DrawnShapeValuesChanged()
        {
            // update selected drawn shape
        }

        private void OnActiveDrawingLayerChanged(MapLayer layer)
        {
            SelectedDrawableMapLayer =
                DrawableMapLayers.FirstOrDefault(x => x.Index == layer.MapLayerOrder);
        }

        public ObservableCollection<BrushPatternItem>BrushPatterns{ get; } = [];

        private BrushPatternItem? _selectedBrushPattern;

        public BrushPatternItem? SelectedBrushPattern
        {
            get => _selectedBrushPattern;
            set => SetProperty(ref _selectedBrushPattern, value);
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

                }
            }
        }

        private DrawingFillType _selectedShapeFillType = DrawingFillType.None;

        public DrawingFillType SelectedShapeFillType
        {
            get => _selectedShapeFillType;
            set => SetProperty(ref _selectedShapeFillType, value);
        }


        private bool _fillDrawnShape = false;
        public bool FillDrawnShape
        {
            get => _fillDrawnShape;
            set
            {
                _fillDrawnShape = value;
            }
        }

        // brush velocity

        public float MinBrushVelocity { get; } = 0;
        public float MaxBrushVelocity { get; } = 1.0f;

        private float _brushVelocity = 1.0f;
        public float BrushVelocity
        {
            get => _brushVelocity;
            set
            {
                var clamped = Math.Clamp(value, MinBrushVelocity, MaxBrushVelocity);

                if (_brushVelocity != clamped)
                {
                    _brushVelocity = clamped;
                    OnPropertyChanged();

                }
            }
        }

        // stamp scale

        public float MinStampScale { get; } = 0;
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

                }
            }
        }

        private string? _selectedStampPath;

        public string? SelectedStampPath
        {
            get => _selectedStampPath;
            set => SetProperty(ref _selectedStampPath, value);
        }

        private BitmapImage? _stampImage;

        public BitmapImage? StampImage
        {
            get => _stampImage;
            set => SetProperty(ref _stampImage, value);
        }

        private bool _isBrushPopupOpen;

        public bool IsBrushPopupOpen
        {
            get => _isBrushPopupOpen;

            set => SetProperty(
                ref _isBrushPopupOpen,
                value);
        }

        // commands

        public ICommand SelectCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.ShapeSelect);
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

        public ICommand FillShapeCommand => new RelayCommand(() =>
        {
            _fillDrawnShape = SelectedShapeFillType != DrawingFillType.None;
        });

        public ICommand PlaceRectangleCommand => new RelayCommand(() =>
        {

        });

        public ICommand PlaceEllipseCommand => new RelayCommand(() =>
        {

        });

        public ICommand PlacePolygonCommand => new RelayCommand(() =>
        {

        });

        public ICommand PlaceStampCommand => new RelayCommand(() =>
        {

        });

        public ICommand EraseDrawingCommand => new RelayCommand(() =>
        {

        });

        public ICommand PixelEditCommand => new RelayCommand(() =>
        {

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

        });

        public ICommand PlaceTriangleCommand => new RelayCommand(() =>
        {

        });

        public ICommand PlaceRightTriangleCommand => new RelayCommand(() =>
        {

        });

        public ICommand PlaceDiamondCommand => new RelayCommand(() =>
        {

        });

        public ICommand PlacePentagonCommand => new RelayCommand(() =>
        {

        });

        public ICommand PlaceHexagonCommand => new RelayCommand(() =>
        {

        });

        public ICommand PlaceArrowCommand => new RelayCommand(() =>
        {

        });

        public ICommand PlaceFivePointStarCommand => new RelayCommand(() =>
        {

        });

        public ICommand PlaceSixPointStarCommand => new RelayCommand(() =>
        {

        });

        // private methods

        private void BuildBrushPatterns()
        {
            // TODO: this could maybe be refactored to read whatever brush bitmaps
            // are in the Assets/Brushes folder and create a brush pattern for them.
            // The challenge would be how to generate a name for the brush.

            var bpi = CreateBrushPattern("Solid Round", "brushes/solidround.png");

            if (bpi != null)
            {
                BrushPatterns.Add(bpi);
            }

            bpi = CreateBrushPattern("Soft Round", "brushes/softround.png");

            if (bpi != null)
            {
                BrushPatterns.Add(bpi);
            }

            bpi = CreateBrushPattern("Square", "brushes/square.png");

            if (bpi != null)
            {
                BrushPatterns.Add(bpi);
            }

            bpi = CreateBrushPattern("Chalk", "brushes/chalk.png");

            if (bpi != null)
            {
                BrushPatterns.Add(bpi);
            }

            bpi = CreateBrushPattern("Sponge", "brushes/sponge.png");

            if (bpi != null)
            {
                BrushPatterns.Add(bpi);
            }

            bpi = CreateBrushPattern("Grass", "brushes/grass.png");

            if (bpi != null)
            {
                BrushPatterns.Add(bpi);
            }

            bpi = CreateBrushPattern("Ink", "brushes/ink.png");

            if (bpi != null)
            {
                BrushPatterns.Add(bpi);
            }

            bpi = CreateBrushPattern("Dry Brush", "brushes/drybrush.png");

            if (bpi != null)
            {
                BrushPatterns.Add(bpi);
            }

            bpi = CreateBrushPattern("Stipple", "brushes/stipple.png");

            if (bpi != null)
            {
                BrushPatterns.Add(bpi);
            }

            bpi = CreateBrushPattern("Crosshatch", "brushes/crosshatch.png");

            if (bpi != null)
            {
                BrushPatterns.Add(bpi);
            }

            bpi = CreateBrushPattern("Pebble", "brushes/pebble.png");

            if (bpi != null)
            {
                BrushPatterns.Add(bpi);
            }

            bpi = CreateBrushPattern("Cloud", "brushes/cloud.png");

            if (bpi != null)
            {
                BrushPatterns.Add(bpi);
            }

            SelectedBrushPattern =
                BrushPatterns.FirstOrDefault();
        }

        private BrushPatternItem? CreateBrushPattern(string name, string assetId)
        {
            if (BrushBrowser.SelectById(assetId))
            {
                AssetDescriptor? brushAsset = BrushBrowser.GetCurrentAsset();

                if (brushAsset != null)
                {
                    string bitmapPath = brushAsset.FilePath;

                    MapBrush brush = new()
                    {
                        BrushName = name,

                        BrushPath = bitmapPath,

                        BrushBitmap =
                            SKBitmap.Decode(bitmapPath),

                        BrushColor = SKColors.Black,

                        BrushSize = new SKSize(32, 32)
                    };


                    return new BrushPatternItem
                    {
                        Name = name,

                        BrushDefinition = brush,

                        PreviewImage =
                            UserInterfaceUtilities.LoadBitmapImage(bitmapPath)
                    };
                }
            }

            return null;

        }
    }

    public interface IDrawingSettings
    {
        BrushPatternItem? SelectedBrushPattern { get; }
        DrawableMapLayerItem? SelectedDrawableMapLayer { get; }
        Color DrawingColor { get; }
        Color FillColor { get; }
        int LineBrushSize { get; }
        float TextureOpacity { get; }
        float TextureScale { get; }
        DrawingFillType SelectedShapeFillType { get; }
        bool FillDrawnShape { get; }
        float BrushVelocity { get; }
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

