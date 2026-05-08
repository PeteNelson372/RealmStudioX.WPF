using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RealmStudioX.WPF.ViewModels.Controls
{
    public class FontSelectionViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainViewModel;
        private readonly FontManager _fontManager;

        public FontManager FontManager => _fontManager;

        private readonly List<string> _allFonts = [];
        private readonly LinkedList<string> _recentFonts = new();

        public List<int> FontSizes { get; } =
            [
                5, 6, 7, 8, 9, 10, 11, 12,
                14, 16, 18, 20, 24, 28, 32, 34,
                36, 40, 42, 48, 56, 60, 72,
                80, 96, 120, 144
            ];

        public ObservableCollection<FontComboItem> FontItems { get; } = [];


        private string _selectedFontFamily = "Aladin";
        public string SelectedFontFamily
        {
            get => _selectedFontFamily;
            set => SetProperty(ref _selectedFontFamily, value);
        }

        private int _selectedFontSize = 14;
        public int SelectedFontSize
        {
            get => _selectedFontSize;
            set
            {
                if (SetProperty(ref _selectedFontSize, value))
                {
                    _selectedFontSize = value;
                }
            }
        }

        private bool _isBold = false;
        public bool IsBold
        {
            get => _isBold;
            set
            {
                if (SetProperty(ref _isBold, value))
                {
                    _isBold = value;
                }
            }
        }

        private bool _isItalic = false;
        public bool IsItalic
        {
            get => _isItalic;
            set
            {
                if (SetProperty(ref _isItalic, value))
                {
                    _isItalic = value;
                }
            }
        }

        private bool _isUnderline = false;
        public bool IsUnderline
        {
            get => _isUnderline;
            set
            {
                if (SetProperty(ref _isUnderline, value))
                {
                    _isUnderline = value;
                }
            }
        }

        private bool _isSuperscript = false;
        public bool IsSuperscript
        {
            get => _isSuperscript;
            set
            {
                if (SetProperty(ref _isSuperscript, value))
                {
                    _isSuperscript = value;
                }
            }
        }

        private bool _isSubscript = false;
        public bool IsSubscript
        {
            get => _isSubscript;
            set
            {
                if (SetProperty(ref _isSubscript, value))
                {
                    _isSubscript = value;
                }
            }
        }

        public FontSelectionViewModel(MainWindowViewModel mainWindowViewModel, FontManager fontManager)
        {
            _mainViewModel = mainWindowViewModel;
            _fontManager = fontManager;

            _allFonts.AddRange(_fontManager.GetAvailableFonts());

            BuildFontItems(_allFonts);

            SelectedFontFamily = "Aladin";
            SelectedFontSize = 14;

        }

        public void RefreshFontSelection()
        {
            OnPropertyChanged(nameof(SelectedFontFamily));
        }

        private void AddFontToRecentList(FontStyleModel fm)
        {
            _recentFonts.Remove(fm.Family);

            _recentFonts.AddLast(fm.Family);

            while (_recentFonts.Count > 6)
            {
                _recentFonts.RemoveFirst();
            }
        }


        public ICommand IncreaseFontSizeCommand => new RelayCommand(() =>
        {
            int currentFontSizeIndex = FontSizes.IndexOf(SelectedFontSize);

            int newFontSizeIndex = (currentFontSizeIndex < FontSizes.Count - 1) ? currentFontSizeIndex + 1 : currentFontSizeIndex;

            newFontSizeIndex = Math.Clamp(newFontSizeIndex, 0, FontSizes.Count - 1);

            int newFontSize = FontSizes[newFontSizeIndex];

            SelectedFontSize = newFontSize;
        });

        public ICommand DecreaseFontSizeCommand => new RelayCommand(() =>
        {
            int currentFontSizeIndex = FontSizes.IndexOf(SelectedFontSize);

            int newFontSizeIndex = (currentFontSizeIndex > 0) ? currentFontSizeIndex - 1 : currentFontSizeIndex;

            newFontSizeIndex = Math.Clamp(newFontSizeIndex, 0, FontSizes.Count - 1); 

            int newFontSize = FontSizes[newFontSizeIndex];

            SelectedFontSize = newFontSize;
        });

        public ICommand OkayCommand => new RelayCommand(() =>
        {
            FontDecorations decorations = FontDecorations.None;

            if (IsUnderline)
                decorations |= FontDecorations.Underline;

            if (IsSuperscript)
                decorations |= FontDecorations.Superscript;

            if (IsSubscript)
                decorations |= FontDecorations.Subscript;

            FontStyleModel fm = new()
            {
                Family = SelectedFontFamily,
                Size = SelectedFontSize,
                Bold = IsBold,
                Italic = IsItalic,
                Decorations = decorations
            };

            AddFontToRecentList(fm);

            _mainViewModel.LabelsViewModel.FontStyle = fm.Clone();

            _ = CloseAndRefreshAsync();
        });

        private async Task CloseAndRefreshAsync()
        {
            _mainViewModel.LabelsViewModel.IsFontPopupOpen = false;

            await Task.Yield();

            RebuildFontItems();
        }

        private void RebuildFontItems()
        {
            string? currentSelection = SelectedFontFamily;

            BuildFontItems(_allFonts);

            SelectedFontFamily = currentSelection;
        }

        private void BuildFontItems(List<string> fontSourceList)
        {
            FontItems.Clear();

            var bundledFonts = _fontManager
                .GetBundledFontFamilies()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // -------------------------------------------------
            // Recent Fonts
            // -------------------------------------------------

            if (_recentFonts.Count > 0)
            {
                FontItems.Add(FontComboItem.Header("Recent Fonts"));

                foreach (var f in _recentFonts.Where(fontSourceList.Contains))
                {
                    FontItems.Add(FontComboItem.Font(f));
                }

                FontItems.Add(FontComboItem.Separator());
            }

            // -------------------------------------------------
            // App Fonts
            // -------------------------------------------------

            var appFonts = fontSourceList
                .Where(f => bundledFonts.Contains(f))
                .ToList();

            if (appFonts.Count > 0)
            {
                FontItems.Add(FontComboItem.Header("App Fonts"));

                foreach (var f in appFonts)
                {
                    FontItems.Add(FontComboItem.Font(f));
                }

                FontItems.Add(FontComboItem.Separator());
            }

            // -------------------------------------------------
            // System Fonts
            // -------------------------------------------------

            var systemFonts = fontSourceList
                .Where(f => !bundledFonts.Contains(f))
                .ToList();

            if (systemFonts.Count > 0)
            {
                FontItems.Add(FontComboItem.Header("System Fonts"));

                foreach (var f in systemFonts)
                {
                    FontItems.Add(FontComboItem.Font(f));
                }
            }
        }
    }

    public enum FontItemType
    {
        Font,
        Header,
        Separator
    }

    public class FontComboItem
    {
        public FontItemType ItemType { get; init; }

        public string Text { get; init; } = string.Empty;

        public bool IsSelectable => ItemType == FontItemType.Font;

        public static FontComboItem Font(string name) =>
            new()
            {
                ItemType = FontItemType.Font,
                Text = name
            };

        public static FontComboItem Header(string text) =>
            new()
            {
                ItemType = FontItemType.Header,
                Text = text
            };

        public static FontComboItem Separator() =>
            new()
            {
                ItemType = FontItemType.Separator
            };

        public string? SelectionValue => ItemType == FontItemType.Font ? Text : null;

        public override string ToString()
        {
            return Text;
        }
    }
}
