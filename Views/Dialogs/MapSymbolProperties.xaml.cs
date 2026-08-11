using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.WPF.Editor.Services;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Cursors = System.Windows.Input.Cursors;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for MapSymbolProperties.xaml
    /// </summary>
    public partial class MapSymbolProperties : ModalDialog, INotifyPropertyChanged
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public MapSymbol SelectedMapSymbol { get; private set; }

        public MainWindowViewModel MainWindowViewModel { get; private set; }

        private readonly MapObjectDescriptionService _descriptionService;

        public MapSymbolProperties(MapSymbol mapSymbol, MainWindowViewModel mainViewModel)
        {
            InitializeComponent();

            SelectedMapSymbol = mapSymbol;
            MainWindowViewModel = mainViewModel;

            _descriptionService = MainWindowViewModel.MapObjectDescriptionService;

            DataContext = this;
        }

        public string MapSymbolName
        {
            get => SelectedMapSymbol != null ? SelectedMapSymbol.Name : string.Empty;
            set
            {
                if (SelectedMapSymbol != null)
                {
                    SelectedMapSymbol.Name = value;
                    OnPropertyChanged();
                    MainWindowViewModel.CommandService.MarkProjectDataModified();
                }
            }
        }

        public string MapSymbolDescription
        {
            get => SelectedMapSymbol != null ? SelectedMapSymbol.Description : string.Empty;
            set
            {
                if (SelectedMapSymbol != null)
                {
                    SelectedMapSymbol.Description = value;
                    OnPropertyChanged();
                    MainWindowViewModel.CommandService.MarkProjectDataModified();
                }
            }
        }

        public ICommand CloseMapSymbolPropertiesCommand => new RelayCommand(() =>
        {
            MainWindowViewModel.CloseMapSymbolProperties();
        });

        private readonly ObjectCharacteristicsViewModel mapSymbolCharacteristics = new();

        public ICommand SetMapSymbolCharacteristicsCommand => new RelayCommand(() =>
        {
            ObjectCharacteristics mapSymbolObjectCharacteristicsDlg = new(mapSymbolCharacteristics, MapObjectType.Symbol);
            mapSymbolObjectCharacteristicsDlg.ShowDialog();
        });

        private bool _mapSymbolNameLocked = false;

        public bool MapSymbolNameLocked
        {
            get { return _mapSymbolNameLocked; }
            set
            {
                _mapSymbolNameLocked = value;
                OnPropertyChanged();
            }
        }

        public ICommand LockMapSymbolNameCommand => new RelayCommand(() =>
        {
            MapSymbolNameLocked = !MapSymbolNameLocked;
        });

        public ICommand GenerateMapSymbolNameCommand => new RelayCommand(() =>
        {
            if (_mapSymbolNameLocked)
            {
                return;
            }

            string generatedName = MainWindowViewModel.GenerateName();

            if (!string.IsNullOrEmpty(generatedName) && SelectedMapSymbol != null)
            {
                MapSymbolName = generatedName;
                MainWindowViewModel.CommandService.MarkMapModified();
            }
        });

        public ICommand GetMapSymbolDescriptionCommand => new RelayCommand(async () =>
        {
            if (SelectedMapSymbol == null)
            {
                return;
            }

            if (mapSymbolCharacteristics != null)
            {
                string query = _descriptionService.BuildAiQuery("MapSymbol",
                    SelectedMapSymbol.Name,
                    mapSymbolCharacteristics.SelectedObjectType,
                    [.. mapSymbolCharacteristics.ObjectCharacteristicsList]);

                try
                {
                    BeginMapSymbolPropertiesUpdates();

                    _descriptionService.ClearDescription();
                    await _descriptionService.GetMapObjectDescription(query);
                    string description = _descriptionService.ObjectDescription;

                    if (!string.IsNullOrEmpty(description))
                    {
                        MapSymbolDescription = description;
                    }
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("An error occurred retrieving an object description.", ex);
                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error retrieving map symbol description.", ex.Message);
                }
                finally
                {
                    MapSymbolPropertiesUpdatesComplete();
                }
            }
        });

        private void BeginMapSymbolPropertiesUpdates()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            GenerateSymbolDescriptionButton.IsEnabled = false;
            SetSymbolCharacteristicsButton.IsEnabled = false;
            CreateSymbolArticleButton.IsEnabled = false;
            MapSymbolPropertiesOkButton.IsEnabled = false;
        }

        private void MapSymbolPropertiesUpdatesComplete()
        {
            Mouse.OverrideCursor = null;

            GenerateSymbolDescriptionButton.IsEnabled = true;
            SetSymbolCharacteristicsButton.IsEnabled = true;
            CreateSymbolArticleButton.IsEnabled = true;
            MapSymbolPropertiesOkButton.IsEnabled = true;
        }


        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
