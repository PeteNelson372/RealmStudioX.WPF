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
    /// Interaction logic for WaterSystemProperties.xaml
    /// </summary>
    public partial class WaterSystemProperties : ModalDialog, INotifyPropertyChanged
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public WaterSystem SelectedWaterSystem { get; private set; }

        public MainWindowViewModel MainWindowViewModel { get; private set; }

        private readonly MapObjectDescriptionService _descriptionService;

        public WaterSystemProperties(WaterSystem watersystem, MainWindowViewModel mainViewModel)
        {
            InitializeComponent();

            SelectedWaterSystem = watersystem;
            MainWindowViewModel = mainViewModel;

            _descriptionService = MainWindowViewModel.MapObjectDescriptionService;

            DataContext = this;
        }

        public string WaterSystemName
        {
            get => SelectedWaterSystem != null ? SelectedWaterSystem.Name : string.Empty;
            set
            {
                if (SelectedWaterSystem != null)
                {
                    SelectedWaterSystem.Name = value;
                    OnPropertyChanged();
                    MainWindowViewModel.CommandService.MarkProjectDataModified();
                }
            }
        }

        public string WaterSystemDescription
        {
            get => SelectedWaterSystem != null ? SelectedWaterSystem.Description : string.Empty;
            set
            {
                if (SelectedWaterSystem != null)
                {
                    SelectedWaterSystem.Description = value;
                    OnPropertyChanged();
                    MainWindowViewModel.CommandService.MarkProjectDataModified();
                }
            }
        }

        public ICommand CloseWaterSystemPropertiesCommand => new RelayCommand(() =>
        {
            MainWindowViewModel.CloseWaterSystemProperties();
        });

        private readonly ObjectCharacteristicsViewModel waterSystemCharacteristics = new();

        public ICommand SetWaterSystemCharacteristicsCommand => new RelayCommand(() =>
        {
            ObjectCharacteristics waterSystemObjectCharacteristicsDlg = new(waterSystemCharacteristics, MapObjectType.WaterSystem);
            waterSystemObjectCharacteristicsDlg.ShowDialog();
        });

        private bool _waterSystemNameLocked = false;

        public bool WaterSystemNameLocked
        {
            get { return _waterSystemNameLocked; }
            set
            {
                _waterSystemNameLocked = value;
                OnPropertyChanged();
            }
        }

        public ICommand LockWaterSystemNameCommand => new RelayCommand(() =>
        {
            WaterSystemNameLocked = !WaterSystemNameLocked;
        });

        public ICommand GenerateWaterSystemNameCommand => new RelayCommand(() =>
        {
            if (_waterSystemNameLocked)
            {
                return;
            }

            string generatedName = MainWindowViewModel.GenerateWaterFeatureName();

            if (!string.IsNullOrEmpty(generatedName) && SelectedWaterSystem != null)
            {
                WaterSystemName = generatedName;
                MainWindowViewModel.CommandService.MarkMapModified();
            }
        });

        public ICommand GetWaterSystemDescriptionCommand => new RelayCommand(async () =>
        {
            if (SelectedWaterSystem == null)
            {
                return;
            }

            if (waterSystemCharacteristics != null)
            {
                string query = _descriptionService.BuildAiQuery("WaterSystem",
                    SelectedWaterSystem.Name,
                    waterSystemCharacteristics.SelectedObjectType,
                    [.. waterSystemCharacteristics.ObjectCharacteristicsList]);

                try
                {
                    BeginWaterSystemPropertiesUpdates();

                    _descriptionService.ClearDescription();
                    await _descriptionService.GetMapObjectDescription(query);
                    string description = _descriptionService.ObjectDescription;

                    if (!string.IsNullOrEmpty(description))
                    {
                        WaterSystemDescription = description;
                        MainWindowViewModel.CommandService.MarkMapModified();
                    }
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("An error occurred retrieving an object description.", ex);
                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error retrieving landform description.", ex.Message);
                }
                finally
                {
                    WaterSystemPropertiesUpdatesComplete();
                }
            }
        });

        private void BeginWaterSystemPropertiesUpdates()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            GenerateWaterSystemDescriptionButton.IsEnabled = false;
            SetWaterSystemCharacteristicsButton.IsEnabled = false;
            CreateWaterSystemArticleButton.IsEnabled = false;
            WaterSystemPropertiesOkButton.IsEnabled = false;
        }

        private void WaterSystemPropertiesUpdatesComplete()
        {
            Mouse.OverrideCursor = null;

            GenerateWaterSystemDescriptionButton.IsEnabled = true;
            SetWaterSystemCharacteristicsButton.IsEnabled = true;
            CreateWaterSystemArticleButton.IsEnabled = true;
            WaterSystemPropertiesOkButton.IsEnabled = true;
        }


        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
