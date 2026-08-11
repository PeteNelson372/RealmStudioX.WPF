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
    /// Interaction logic for RegionProperties.xaml
    /// </summary>
    public partial class RegionProperties : ModalDialog, INotifyPropertyChanged
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public MapRegion SelectedRegion { get; private set; }

        public MainWindowViewModel MainWindowViewModel { get; private set; }

        private readonly MapObjectDescriptionService _descriptionService;

        public RegionProperties(MapRegion region, MainWindowViewModel mainViewModel)
        {
            InitializeComponent();

            SelectedRegion = region;
            MainWindowViewModel = mainViewModel;

            _descriptionService = MainWindowViewModel.MapObjectDescriptionService;

            DataContext = this;
        }

        public string RegionName
        {
            get => SelectedRegion != null ? SelectedRegion.RegionName : string.Empty;
            set
            {
                if (SelectedRegion != null)
                {
                    SelectedRegion.RegionName = value;
                    OnPropertyChanged();
                    MainWindowViewModel.CommandService.MarkProjectDataModified();
                }
            }
        }

        public string RegionDescription
        {
            get => SelectedRegion != null ? SelectedRegion.RegionDescription : string.Empty;
            set
            {
                if (SelectedRegion != null)
                {
                    SelectedRegion.RegionDescription = value;
                    OnPropertyChanged();
                    MainWindowViewModel.CommandService.MarkProjectDataModified();
                }
            }
        }

        public ICommand CloseRegionPropertiesCommand => new RelayCommand(() =>
        {
            MainWindowViewModel.CloseMapRegionProperties();
        });

        private readonly ObjectCharacteristicsViewModel regionCharacteristics = new();

        public ICommand SetRegionCharacteristicsCommand => new RelayCommand(() =>
        {
            ObjectCharacteristics regionObjectCharacteristicsDlg = new(regionCharacteristics, MapObjectType.Region);
            regionObjectCharacteristicsDlg.ShowDialog();
        });

        private bool _regionNameLocked = false;

        public bool RegionNameLocked
        {
            get { return _regionNameLocked; }
            set
            {
                _regionNameLocked = value;
                OnPropertyChanged();
            }
        }

        public ICommand LockRegionNameCommand => new RelayCommand(() =>
        {
            RegionNameLocked = !RegionNameLocked;
        });

        public ICommand GenerateRegionNameCommand => new RelayCommand(() =>
        {
            if (_regionNameLocked)
            {
                return;
            }

            string generatedName = MainWindowViewModel.GenerateName();

            if (!string.IsNullOrEmpty(generatedName) && SelectedRegion != null)
            {
                RegionName = generatedName;
                MainWindowViewModel.CommandService.MarkMapModified();
            }
        });

        public ICommand GetRegionDescriptionCommand => new RelayCommand(async () =>
        {
            if (SelectedRegion == null)
            {
                return;
            }

            if (regionCharacteristics != null)
            {
                string query = _descriptionService.BuildAiQuery("MapRegion",
                    SelectedRegion.RegionName,
                    regionCharacteristics.SelectedObjectType,
                    [.. regionCharacteristics.ObjectCharacteristicsList]);

                try
                {
                    BeginRegionPropertiesUpdates();

                    _descriptionService.ClearDescription();
                    await _descriptionService.GetMapObjectDescription(query);
                    string description = _descriptionService.ObjectDescription;

                    if (!string.IsNullOrEmpty(description))
                    {
                        RegionDescription = description;
                    }
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("An error occurred retrieving an object description.", ex);
                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error retrieving region description.", ex.Message);
                }
                finally
                {
                    RegionPropertiesUpdatesComplete();
                }
            }
        });

        private void BeginRegionPropertiesUpdates()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            GenerateRegionDescriptionButton.IsEnabled = false;
            SetRegionCharacteristicsButton.IsEnabled = false;
            CreateRegionArticleButton.IsEnabled = false;
            RegionPropertiesOkButton.IsEnabled = false;
        }

        private void RegionPropertiesUpdatesComplete()
        {
            Mouse.OverrideCursor = null;

            GenerateRegionDescriptionButton.IsEnabled = true;
            SetRegionCharacteristicsButton.IsEnabled = true;
            CreateRegionArticleButton.IsEnabled = true;
            RegionPropertiesOkButton.IsEnabled = true;
        }


        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
