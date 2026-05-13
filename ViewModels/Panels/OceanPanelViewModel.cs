using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Controls;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using SkiaSharp.Views.WPF;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class OceanPanelViewModel : ViewModelBase, IWindroseSettings
    {
        private EditorController _editor;
        private AssetManager _assetManager;

        public Action<TextureFillRequest>? FillRequested;
        public Action? ClearRequested;

        public AssetBrowserViewModel TextureBrowser { get; }

        public OceanPanelViewModel(EditorController editor, AssetManager assetManager)
        {
            _editor = editor;
            _assetManager = assetManager;
            var browser = new AssetBrowser(_assetManager, AssetType.WaterTexture);
            TextureBrowser = new AssetBrowserViewModel(browser);
        }

        // texture

        public ICommand ApplyTextureCommand => new RelayCommand(() =>
        {
            TextureFillRequest applyTextureRequest = new()
            {
                TextureId = TextureBrowser.SelectedAssetId,
                Scale = (float)TextureScale,
                Opacity = TextureOpacity,
                Mirror = MirrorTexture,
            };

            _editor.ApplyOceanTexture(applyTextureRequest);
        });

        public ICommand ClearTextureCommand => new RelayCommand(() =>
        {
            _editor.ClearOceanTexture();
        });



        // color

        public ICommand FillColorCommand => new RelayCommand(() =>
        {
            TextureFillRequest fillColorRequest = new()
            {
                Color = OceanColor.ToSKColor(),
            };

            _editor.FillOceanColor(fillColorRequest);
        });

        public ICommand ClearOceanColorCommand => new RelayCommand(() =>
        {
            _editor.ClearOceanColor();
        });

        private void PreviewChanged()
        {
            if (_assetManager == null)
                return;

            TextureFillRequest updateRequest = new()
            {
                TextureId = TextureBrowser.SelectedAssetId,
                Scale = (float)TextureScale,
                Opacity = TextureOpacity,
                Mirror = MirrorTexture,
                Color = OceanColor.ToSKColor(),
            };

            _editor.UpdateOceanPreview(updateRequest);
        }

        public float MinOpacity { get; } = 0f;
        public float MaxOpacity { get; } = 1f;

        private float _textureOpacity = 1.0f;
        public float TextureOpacity
        {
            get => _textureOpacity;
            set
            {
                var clamped = Math.Clamp(value, MinOpacity, MaxOpacity);

                if (SetProperty(ref _textureOpacity, clamped))
                {
                    PreviewChanged();
                }
            }
        }

        public float MinScale { get; } = 0f;
        public float MaxScale { get; } = 2f;

        private float _textureScale = 1.0f;
        public float TextureScale
        {
            get => _textureScale;
            set
            {
                var clamped = Math.Clamp(value, MinScale, MaxScale);

                if (SetProperty(ref _textureScale, clamped))
                {
                    PreviewChanged();
                }
            }
        }

        private bool _mirrorTexture = false;
        public bool MirrorTexture
        {
            get => _mirrorTexture;
            set
            {
                if (SetProperty(ref _mirrorTexture, value))
                {
                    PreviewChanged();
                }
            }
        }

        // ocean color

        private Color _oceanColor = Colors.White;
        public Color OceanColor
        {
            get => _oceanColor;
            set
            {
                if (SetProperty(ref _oceanColor, value))
                {
                    _oceanColorBrush.Color = value;
                }
            }
        }

        private SolidColorBrush _oceanColorBrush = new(Colors.White);

        public Brush OceanColorBrush => _oceanColorBrush;


        // WINDROSE

        // windrose color

        private Color _windroseColor = Color.FromArgb(127, 61, 55, 40);
        public Color WindroseColor
        {
            get => _windroseColor;
            set
            {
                if (SetProperty(ref _windroseColor, value))
                {
                    _windroseColorBrush.Color = value;
                }
            }
        }

        private SolidColorBrush _windroseColorBrush = new(Color.FromArgb(127, 61, 55, 40));

        public Brush WindroseColorBrush => _windroseColorBrush;

        // windrose directions

        public int MinDirections { get; } = 4;
        public int MaxDirections { get; } = 32;

        private int _windroseDirections = 16;
        public int WindroseDirections
        {
            get => _windroseDirections;
            set
            {
                var clamped = Math.Clamp(value, MinDirections, MaxDirections);

                SetProperty(ref _windroseDirections, clamped);
            }
        }

        // windrose line width

        public int MinLineWidth { get; } = 1;
        public int MaxLineWidth { get; } = 16;

        private int _windroseLineWidth = 2;
        public int WindroseLineWidth
        {
            get => _windroseLineWidth;
            set
            {
                var clamped = Math.Clamp(value, MinLineWidth, MaxLineWidth);

                SetProperty(ref _windroseLineWidth, clamped);
            }
        }

        // windrose inner radius

        public int MinInnerRadius { get; } = 0;
        public int MaxInnerRadius { get; } = 1024;

        private int _windroseInnerRadius = 0;
        public int WindroseInnerRadius
        {
            get => _windroseInnerRadius;
            set
            {
                var clamped = Math.Clamp(value, MinInnerRadius, MaxInnerRadius);
                SetProperty(ref _windroseInnerRadius, clamped);
            }
        }

        // windrose outer radius

        public int MinOuterRadius { get; } = 100;
        public int MaxOuterRadius { get; } = 20000;

        private int _windroseOuterRadius = 1000;
        public int WindroseOuterRadius
        {
            get => _windroseOuterRadius;
            set
            {
                var clamped = Math.Clamp(value, MinOuterRadius, MaxOuterRadius);
                SetProperty(ref _windroseOuterRadius, clamped);
            }
        }

        // windrose circles

        public int MinCircles { get; } = 0;
        public int MaxCircles { get; } = 2;

        private int _windroseCircles = 0;
        public int WindroseCircles
        {
            get => _windroseCircles;
            set
            {
                var clamped = Math.Clamp(value, MinCircles, MaxCircles);
                SetProperty(ref _windroseCircles, clamped);
            }
        }

        // windrose direction lines fade

        private bool _windroseFade= false;
        public bool WindroseFade
        {
            get => _windroseFade;
            set
            {
                SetProperty(ref _windroseFade, value);
            }
        }

        public ICommand PlaceWindroseCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.PlaceWindrose);
            _editor.ActivateTool(EditorToolType.WindroseTool, (IWindroseSettings)this);
        });

        public ICommand ClearWindroseCommand => new RelayCommand(() =>
        {
            _editor.ClearWindroses();
        });
    }

    public interface IWindroseSettings
    {
        Color WindroseColor { get; }
        int WindroseDirections { get; }
        int WindroseLineWidth {  get; }
        int WindroseInnerRadius { get; }
        int WindroseOuterRadius { get; }
        int WindroseCircles { get; }
        bool WindroseFade { get; }

    }
}
