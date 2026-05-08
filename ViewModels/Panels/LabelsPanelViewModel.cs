using RealmStudioShapeRenderingLib;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class LabelsPanelViewModel : ViewModelBase, ILabelSettings
    {
        private readonly EditorController _editor;
        public EditorController Editor => _editor;

        private readonly AssetManager _assetManager;

        public LabelsPanelViewModel(EditorController editor, AssetManager assetManager)
        {
            _editor = editor;
            _assetManager = assetManager;
        }

        // font style model
        private FontStyleModel _fontStyle = new();
        public FontStyleModel FontStyle
        {
            get => _fontStyle;
            set
            {
                if (SetProperty(ref _fontStyle, value))
                {
                    _fontStyle = value;
                    OnPropertyChanged(nameof(SelectedFontFamily));
                    LabelValuesChanged();
                }
            }
        }

        public string SelectedFontFamily
        {
            get => FontStyle?.Family ?? "Segoe UI";
        }

        // label color

        private Color _labelColor = Color.FromRgb(61,53,30);
        public Color LabelColor
        {
            get => _labelColor;
            set
            {
                if (SetProperty(ref _labelColor, value))
                {
                    _labelColorBrush.Color = value;
                    LabelValuesChanged();
                }
            }
        }

        private SolidColorBrush _labelColorBrush = new(Color.FromRgb(61, 53, 30));

        public Brush LabelColorBrush => _labelColorBrush;

        // outline color

        private Color _outlineColor = Color.FromArgb(161, 214, 202, 171);
        public Color OutlineColor
        {
            get => _outlineColor;
            set
            {
                if (SetProperty(ref _outlineColor, value))
                {
                    _outlineColorBrush.Color = value;
                    LabelValuesChanged();
                }
            }
        }

        private SolidColorBrush _outlineColorBrush = new(Color.FromArgb(161, 214, 202, 171));

        public Brush OutlineColorBrush => _outlineColorBrush;

        // outline width

        public float MinOutlineWidth { get; } = 0.0f;
        public float MaxOutlineWidth { get; } = 32.0f;

        private float _outlineWidth = 0.0f;
        public float OutlineWidth
        {
            get => _outlineWidth;
            set
            {
                var clamped = Math.Clamp(value, MinOutlineWidth, MaxOutlineWidth);

                if (_outlineWidth != clamped)
                {
                    _outlineWidth = clamped;
                    OnPropertyChanged();
                    LabelValuesChanged();
                }
            }
        }

        // glow color

        private Color _glowColor = Color.FromRgb(61, 53, 30);
        public Color GlowColor
        {
            get => _glowColor;
            set
            {
                if (SetProperty(ref _glowColor, value))
                {
                    _glowColorBrush.Color = value;
                    LabelValuesChanged();
                }
            }
        }

        private SolidColorBrush _glowColorBrush = new(Colors.White);

        public Brush GlowColorBrush => _glowColorBrush;

        // glow strength

        public float MinGlowStrength { get; } = 0.0f;
        public float MaxGlowStrength { get; } = 32.0f;

        private float _glowStrength = 0.0f;
        public float GlowStrength
        {
            get => _glowStrength;
            set
            {
                var clamped = Math.Clamp(value, MinGlowStrength, MaxGlowStrength);

                if (_glowStrength != clamped)
                {
                    _glowStrength = clamped;
                    OnPropertyChanged();
                    LabelValuesChanged();
                }
            }
        }

        // label rotation

        public int MinLabelRotation { get; } = 0;
        public int MaxLabelRotation { get; } = 359;

        private int _labelRotation = 0;
        public int LabelRotation
        {
            get => _labelRotation;
            set
            {
                var clamped = Math.Clamp(value, MinLabelRotation, MaxLabelRotation);

                if (_labelRotation != clamped)
                {
                    _labelRotation = clamped;
                    OnPropertyChanged();
                    LabelValuesChanged();
                }
            }
        }

        // label scale

        public float MinLabelScale { get; } = 0.01f;
        public float MaxLabelScale { get; } = 2.0f;

        private float _labelScale = 1.0f;
        public float LabelScale
        {
            get => _labelScale;
            set
            {
                var clamped = Math.Clamp(value, MinLabelScale, MaxLabelScale);

                if (_labelScale != clamped)
                {
                    _labelScale = clamped;
                    OnPropertyChanged();
                    LabelValuesChanged();
                }
            }
        }

        private bool _isFontPopupOpen = false;
        public bool IsFontPopupOpen
        {
            get => _isFontPopupOpen;
            set
            {
                if (_isFontPopupOpen != value)
                {
                    _isFontPopupOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand OpenFontPopupCommand => new RelayCommand(() =>
        {
            IsFontPopupOpen = !IsFontPopupOpen;
        });

        public ICommand SelectCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.ShapeSelect);
        });

        public ICommand PlaceLabelCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawLabel);
            _editor.ActivateTool(EditorToolType.LabelTool, (ILabelSettings)this);
        });

        public ICommand DrawLabelArcCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawArcLabelPath);
            _editor.ActivateTool(EditorToolType.LabelTool, (ILabelSettings)this);
        });

        public ICommand DrawLabelCurveCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawBezierLabelPath);
            _editor.ActivateTool(EditorToolType.LabelTool, (ILabelSettings)this);
        });

        private void LabelValuesChanged()
        {
            if (_assetManager == null)
                return;

            // apply changes to selected symbol
            _editor.UpdateSelectedLabel((ILabelSettings)this);
        }
    }

    public interface ILabelSettings
    {
        FontStyleModel FontStyle { get; }
        Color LabelColor { get; }
        Color OutlineColor { get; }
        float OutlineWidth { get; }
        Color GlowColor { get; }
        float GlowStrength { get; }
        int LabelRotation { get; }
        float LabelScale { get; }
    }
}
