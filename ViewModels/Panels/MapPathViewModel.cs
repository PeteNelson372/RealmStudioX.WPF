using RealmStudioShapeRenderingLib;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Controls;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using SkiaSharp.Views.WPF;
using Svg.Skia;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class MapPathViewModel : ViewModelBase, IMapPathSettings
    {
        private readonly EditorController _editor;
        private readonly AssetManager _assetManager;
        public AssetBrowserViewModel PathTextureBrowser { get; }

        public MapPathViewModel(EditorController editor, AssetManager assetManager)
        {
            _editor = editor;
            _assetManager = assetManager;

            var browser = new AssetBrowser(_assetManager, AssetType.PathTexture);
            PathTextureBrowser = new AssetBrowserViewModel(browser);

            PathTextureBrowser.TextureSelectionChanged += PathValuesChanged;
        }

        // path style
        private PathType _pathStyle = PathType.SolidLinePath;
        public PathType PathStyle
        {
            get => _pathStyle;
            set
            {
                if (SetProperty(ref _pathStyle, value))
                {
                    PathValuesChanged();
                }
            }
        }

        // path width

        private int _pathWidth = 8;
        public int PathWidth
        {
            get => _pathWidth;
            set
            {
                if (SetProperty(ref _pathWidth, value))
                {
                    PathValuesChanged();
                }
            }
        }

        // path color

        private Color _pathColor = Color.FromRgb(75, 49, 26);
        public Color PathColor
        {
            get => _pathColor;
            set
            {
                if (SetProperty(ref _pathColor, value))
                {
                    _pathColorBrush.Color = value;
                    PathValuesChanged();
                }
            }
        }

        private SolidColorBrush _pathColorBrush = new(Color.FromRgb(75, 49, 26));

        public Brush PathColorBrush => _pathColorBrush;

        // path border color

        private Color _pathBorderColor = Color.FromRgb(75, 49, 26);
        public Color PathBorderColor
        {
            get => _pathBorderColor;
            set
            {
                if (SetProperty(ref _pathBorderColor, value))
                {
                    _pathBorderColorBrush.Color = value;
                    PathValuesChanged();
                }
            }
        }

        private SolidColorBrush _pathBorderColorBrush = new(Color.FromRgb(75, 49, 26));

        public Brush PathBorderColorBrush => _pathBorderColorBrush;

        // draw over symbols

        private bool _drawOverSymbols = false;
        public bool DrawOverSymbols
        {
            get => _drawOverSymbols;
            set => SetProperty(ref _drawOverSymbols, value);
        }

        // edit path points

        private bool _editPathPoints = false;
        public bool EditPathPoints
        {
            get => _editPathPoints;
            set => SetProperty(ref _editPathPoints, value);
        }

        // texture opacity

        private float _textureOpacity = 1.0f;
        public float TextureOpacity
        {
            get => _textureOpacity;
            set
            {
                if (SetProperty(ref _textureOpacity, value))
                {
                    PathValuesChanged();
                }
            }
        }

        // texture scale

        private float _textureScale = 1.0f;
        public float TextureScale
        {
            get => _textureScale;
            set
            {
                if (value > 0.0f)
                {
                    if (SetProperty(ref _textureScale, value))
                    {
                        PathValuesChanged();
                    }
                }
            }
        }

        // tower distance

        private float _towerDistance = 10f;
        public float TowerDistance
        {
            get => _towerDistance;
            set
            {
                if (SetProperty(ref _towerDistance, value))
                {
                    PathValuesChanged();
                }
            }
        }

        // tower size

        private float _towerSize = 1.2f;
        public float TowerSize
        {
            get => _towerSize;
            set
            {
                if (SetProperty(ref _towerSize, value))
                {
                    PathValuesChanged();
                }
            }
        }

        private bool _showCrenelations = true;
        public bool ShowCrenelations
        {
            get => _showCrenelations;
            set
            {
                if (SetProperty(ref _showCrenelations, value))
                {
                    PathValuesChanged();
                }
            }
        }

        private bool _useMarkers = false;
        public bool UseMarkers
        {
            get => _useMarkers;
            set
            {
                if (SetProperty(ref _useMarkers, value))
                {
                    PathValuesChanged();
                }
            }
        }

        private bool _useTexture = false;
        public bool UseTexture
        {
            get => _useTexture;
            set
            {
                if (SetProperty(ref _useTexture, value))
                {
                    PathValuesChanged();
                }
            }
        }

        public string? PathTextureId => PathTextureBrowser.SelectedAssetId;

        private void PathValuesChanged()
        {
            if (_assetManager == null)
                return;

            PathRenderStyle renderStyle = new PathRenderStyle()
            {
                Width = PathWidth,
                BorderColor = PathBorderColor.ToSKColor(),
                Color = PathColor.ToSKColor(),
                TextureId = (PathTextureBrowser.SelectedAssetId != null) ? PathTextureBrowser.SelectedAssetId : string.Empty,
                DrawCrenelations = ShowCrenelations,
                MapPathType = PathStyle,
                TextureOpacity = TextureOpacity,
                TextureScale = TextureScale,
                TowerDistance = TowerDistance,
                TowerSize = TowerSize,
            };

            IReadOnlyList<AssetDescriptor>? descriptors = null;

            switch (renderStyle.MapPathType)
            {
                case PathType.FootprintsPath:
                    {
                        renderStyle.UseMarkers = true;
                        descriptors = _assetManager.GetByName(AssetType.Vector, "Foot Prints");
                        renderStyle.MarkerSpacing = 2f;
                    }
                    break;

                case PathType.BearTracksPath:
                    {
                        renderStyle.UseMarkers = true;
                        descriptors = _assetManager.GetByName(AssetType.Vector, "Bear Tracks");
                        renderStyle.MarkerSpacing = 1.5f;
                    }
                    break;
                case PathType.BirdTracksPath:
                    {
                        renderStyle.UseMarkers = true;
                        descriptors = _assetManager.GetByName(AssetType.Vector, "Bird Tracks");
                        renderStyle.MarkerSpacing = 1.2f;
                    }
                    break;
                case PathType.TexturedPath:
                    {
                        renderStyle.UseTexture = true;
                    }
                    break;
                case PathType.BorderAndTexturePath:
                    {
                        renderStyle.UseTexture = true;
                    }
                    break;
            }

            if (renderStyle.UseMarkers && descriptors != null && descriptors.Count > 0)
            {
                AssetDescriptor marker = descriptors[0];
                SKSvg svg = new();
                renderStyle.Marker = svg.Load(marker.FilePath);
            }

            _editor.UpdateSelectedPath(renderStyle);
        }


        public ICommand SelectCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.ShapeSelect);
        });

        public ICommand DrawPathCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.PathPaint);
            _editor.ActivateTool(EditorToolType.MapPathTool, (IMapPathSettings)this);
        });

        public ICommand EditPathPointsCommand => new RelayCommand(() =>
        {
            if (_editor != null && _editor.SelectedShape is MapPath mp)
            {
                mp.Editor.IsEditing = EditPathPoints;

                if (!mp.Editor.IsEditing)
                {
                    mp.Editor.OnChanged!();
                }
            }
        });
    }

    public interface IMapPathSettings
    {
        PathType PathStyle { get; }
        int PathWidth { get; }
        Color PathColor { get; }
        Color PathBorderColor { get; }
        bool DrawOverSymbols { get; }
        bool EditPathPoints { get; }
        string? PathTextureId { get; }
        float TextureOpacity { get; }
        float TextureScale { get; }
        float TowerDistance { get; }
        float TowerSize { get; }
        bool ShowCrenelations { get; }
        bool UseMarkers { get; }
        bool UseTexture { get; }
    }
}
