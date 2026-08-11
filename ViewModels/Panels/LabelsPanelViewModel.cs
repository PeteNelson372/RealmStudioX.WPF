using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.Editor.Tools;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Dialogs;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class LabelsPanelViewModel : ViewModelBase, ILabelSettings, IBoxSettings
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        public MainWindowViewModel MainViewModel => _mainWindowViewModel;

        private readonly EditorController _editor;
        public EditorController Editor => _editor;

        private readonly AssetManager _assetManager;

        public ObservableCollection<LabelPreset> LabelPresets { get; } = [];

        public ObservableCollection<BoxGridItem> BoxItems { get; } = [];

        public LabelsPanelViewModel(MainWindowViewModel mainViewModel, EditorController editor, AssetManager assetManager)
        {
            _mainWindowViewModel = mainViewModel;
            _editor = editor;
            _assetManager = assetManager;

            AddBoxItems();
        }

        public void AddLabelPresets()
        {
            var presets = _assetManager.GetByType(AssetType.LabelPreset);

            foreach (var preset in presets)
            {
                if (File.Exists(preset.FilePath))
                {
                    try
                    {
                        LabelPreset? ps = MapFileMethods.ReadLabelPreset(preset.FilePath);
                        if (ps != null)
                        {
                            MainViewModel.ThemeManager.ResolveLabelPresetFont(ps);
                            LabelPresets.Add(ps);
                        }
                    }
                    catch (Exception ex)
                    {
                        RealmStudioXLogger.Exception($"An error occurred reading a label preset at {preset.FilePath}", ex);
                    }
                }
            }
        }

        // selected label preset
        private LabelPreset? _selectedLabelPreset;

        public LabelPreset? SelectedLabelPreset
        {
            get => _selectedLabelPreset;
            set
            {
                _selectedLabelPreset = value;

                OnPropertyChanged();

                if (_selectedLabelPreset != null)
                {
                    FontStyle = _selectedLabelPreset.LabelFont;
                    MainViewModel.FontPanelViewModel.SelectedFontFamily = _selectedLabelPreset.LabelFontFamily;
                    MainViewModel.FontPanelViewModel.SelectedFontSize = (int)_selectedLabelPreset.LabelFontSize;
                    MainViewModel.FontPanelViewModel.IsBold = _selectedLabelPreset.LabelFontBold;
                    MainViewModel.FontPanelViewModel.IsItalic = _selectedLabelPreset.LabelFontItalic;

                    LabelColor = _selectedLabelPreset.LabelColor.ToColor();
                    OutlineColor = _selectedLabelPreset.LabelOutlineColor.ToColor();
                    OutlineWidth = _selectedLabelPreset.LabelOutlineWidth;
                    GlowColor = _selectedLabelPreset.LabelGlowColor.ToColor();
                    GlowStrength = _selectedLabelPreset.LabelGlowStrength;
                }
            }
        }

        // label text

        public string LabelText
        {
            get
            {
                if (Editor.ActiveEditorTool is LabelTool lt && lt.IsEditing && lt.EditSession != null)
                {
                    return lt.EditSession.Text;
                }
                else if (Editor.SelectionService!.PrimarySelection != null && Editor.SelectionService!.PrimarySelection.ReferencedShape is MapLabel ml)
                {
                    return ml.Text;
                }
                else
                {
                    return string.Empty;
                }
            }

            set
            {
                if (Editor.ActiveEditorTool is LabelTool lt && lt.IsEditing && lt.EditSession != null)
                {
                    lt.EditSession.Text = value;
                    OnPropertyChanged(nameof(LabelText));
                    LabelValuesChanged();
                }
                else if (Editor.SelectionService!.PrimarySelection != null && Editor.SelectionService!.PrimarySelection.ReferencedShape is MapLabel ml)
                {
                    ml.Text = value;
                    OnPropertyChanged(nameof(LabelText));
                    LabelValuesChanged();
                }
            }
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

        // rotation

        public int MinRotation { get; } = 0;
        public int MaxRotation { get; } = 359;

        private int _rotation = 0;
        public int Rotation
        {
            get => _rotation;
            set
            {
                var clamped = Math.Clamp(value, MinRotation, MaxRotation);

                if (_rotation != clamped)
                {
                    _rotation = clamped;
                    OnPropertyChanged();
                    LabelValuesChanged();
                    BoxValuesChanged();
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
            _editor.ActivateTool(EditorToolType.SelectionTool);
        });

        public ICommand PlaceLabelCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawLabel);
            _editor.ActivateTool(EditorToolType.LabelTool, (ILabelSettings)this);
        });

        private string _newLabelPresetName = string.Empty;
        public string NewLabelPresetName
        {
            get { return _newLabelPresetName; }
            set
            {
                _newLabelPresetName = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddPresetCommand => new RelayCommand(() =>
        {
            NewLabelPresetDialog presetDialog = new()
            {
                DataContext = this
            };

            bool? result = presetDialog.ShowDialog();

            if (result == true)
            {
                if (!string.IsNullOrEmpty(NewLabelPresetName))
                {
                    if (UserInterfaceUtilities.IsValidFileName(NewLabelPresetName))
                    {
                        LabelPreset newPreset = new()
                        {
                            LabelPresetName = NewLabelPresetName,
                            LabelColor = LabelColor.ToSKColor(),
                            LabelOutlineColor = OutlineColor.ToSKColor(),
                            LabelOutlineWidth = OutlineWidth,
                            LabelGlowColor = GlowColor.ToSKColor(),
                            LabelGlowStrength = (int)GlowStrength,
                            LabelFontFamily = FontStyle.Family,
                            LabelFontSize = FontStyle.Size,
                            LabelFontBold = FontStyle.Bold,
                            LabelFontItalic = FontStyle.Italic,
                        };

                        string presetPath = Path.Combine(AssetManager.RootAssetDirectory, "LabelPresets", NewLabelPresetName) + RealmStudioFileFormat.RealmStudioLabelPresetExtension;

                        try
                        {
                            MapFileMethods.SerializeLabelPreset(newPreset, presetPath);
                            LabelPresets.Add(newPreset);

                            MessageDialog dlg = MessageDialogFactory.InformationDialog("Label Preset Saved", $"Label Preset {NewLabelPresetName} was saved.");
                            dlg.ShowDialog();
                        }
                        catch (Exception ex)
                        {
                            RealmStudioXLogger.Exception($"An error occurred while serializing label preset to {presetPath}", ex);

                            MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error Saving Label Preset",
                                "An error occurred while serializing label preset: " + ex.Message);
                            dlg.ShowDialog();
                        }

                    }
                }
            }
        });

        public ICommand RemovePresetCommand => new RelayCommand(() =>
        {
            if (SelectedLabelPreset != null && !SelectedLabelPreset.IsSystem)
            {
                MessageDialog deleteConfirmationDlg = MessageDialogFactory.DeleteConfirmationDialog("Confirm Deletion", $"Delete Label Preset {SelectedLabelPreset.LabelPresetName}? This operation cannot be undone.");
                deleteConfirmationDlg.ShowDialog();

                if (((MessageDialogViewModel)deleteConfirmationDlg.DataContext).Result == MessageDialogResult.Delete)
                {
                    string presetPath = Path.Combine(AssetManager.RootAssetDirectory, "LabelPresets", SelectedLabelPreset.LabelPresetName) + RealmStudioFileFormat.RealmStudioLabelPresetExtension;

                    try
                    {
                        if (File.Exists(presetPath))
                        {                            
                            File.Delete(presetPath);
                            LabelPresets.Remove(SelectedLabelPreset);

                            MessageDialog dlg = MessageDialogFactory.InformationDialog("Label Preset Removed", $"The Label Preset was removed.");
                            dlg.ShowDialog();
                        }
                    }
                    catch (Exception ex)
                    {
                        RealmStudioXLogger.Exception($"An error occurred while removing the label preset", ex);

                        MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error Removing Label Preset",
                            $"An error occurred while the removing label preset: " + ex.Message);
                        dlg.ShowDialog();
                    }
                }
            }
        });

        public ICommand GenerateNameCommand => new RelayCommand(() =>
        {
            List<INameGenerator> generators = AssetManager.GetAllNameGenerators();

            string generatedName = string.Empty;

            if (generators.Count > 0)
            {
                int guardCount = 0;
                int maxTries = 100;

                while (string.IsNullOrEmpty(generatedName) && guardCount < maxTries)
                {
                    guardCount++;
                    string name = NameManager.GenerateRandomPlaceName(generators);

                    if (!string.IsNullOrEmpty(name))
                    {
                        generatedName = name;
                    }
                }
            }

            if (!string.IsNullOrEmpty(generatedName))
            {
                LabelText = generatedName;
            }
        });

        private void LabelValuesChanged()
        {
            if (_assetManager == null)
                return;

            // apply changes to selected symbol
            _editor.UpdateSelectedLabel((ILabelSettings)this);
        }


        // MapBox methods, properties, and data
        
        private BoxGridItem? _selectedBox;

        public BoxGridItem? SelectedBox
        {
            get => _selectedBox;
            set => SetProperty(ref _selectedBox, value);
        }

        // box tint

        private Color _boxTint = Colors.White;
        public Color BoxTint
        {
            get => _boxTint;
            set
            {
                if (SetProperty(ref _boxTint, value))
                {
                    _boxTintBrush.Color = value;
                    BoxValuesChanged();
                }
            }
        }

        private SolidColorBrush _boxTintBrush = new(Colors.White);

        public Brush BoxTintBrush => _boxTintBrush;

        private void BoxValuesChanged()
        {
            if (SelectedBox == null)
                return;

            // apply changes to selected symbol
            _editor.UpdateSelectedBox((IBoxSettings) this);
        }

        public ICommand CreateBoxCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawBox);
            BoxTool? bt = (BoxTool?)_editor.ActivateTool(EditorToolType.BoxTool, (IBoxSettings)this);
        });

        internal void AddBoxItems()
        {
            var boxes = _assetManager.GetByType(AssetType.Box);

            BoxItems.Clear();

            if (boxes != null)
            {
                // TODO: get box assets from AssetManager
                foreach (var box in boxes)
                {
                    if (box.FilePath.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase))
                    {
                        MapBox? mapBox = MapFileMethods.ReadBoxAssetFromXml(box.FilePath);

                        if (mapBox != null)
                        {
                            SKBitmap? boxBitmap = SKBitmap.Decode(mapBox.BoxBitmapPath);
                            if (boxBitmap != null)
                            {
                                if (!BoxItems.Any(i =>
                                        string.Equals(
                                            i.BoxDefinition.BoxName,
                                            mapBox.BoxName,
                                            StringComparison.OrdinalIgnoreCase)))
                                {
                                    mapBox.BoxBitmap = boxBitmap.Copy();

                                    ImageSource? ims = boxBitmap.ToImageSource();

                                    if (ims != null)
                                    {
                                        BoxGridItem gridItem = new(mapBox, ims);
                                        BoxItems.Add(gridItem);
                                    }
                                }
                            }
                        }
                    }
                }
            }
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
        int Rotation { get; }
        float LabelScale { get; }
    }

    public interface IBoxSettings
    {
        Color BoxTint {  get; }
        BoxGridItem? SelectedBox {  get; }
        int Rotation { get; }
    }

    public class BoxGridItem
    {
        public ImageSource BoxImage { get; }
        public MapBox BoxDefinition { get; }

        public BoxGridItem(MapBox box,
                  ImageSource image)
        {
            BoxDefinition = box;
            BoxImage = image;
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

