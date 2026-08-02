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
    /// Interaction logic for WaterBodyProperties.xaml
    /// </summary>
    public partial class WaterBodyProperties : ModalDialog, INotifyPropertyChanged
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public PaintedWaterBody SelectedWaterBody { get; private set; }

        public MainWindowViewModel MainWindowViewModel { get; private set; }

        private readonly MapObjectDescriptionService _descriptionService;

        public WaterBodyProperties(PaintedWaterBody waterBody, MainWindowViewModel mainViewModel)
        {
            InitializeComponent();

            SelectedWaterBody = waterBody;
            MainWindowViewModel = mainViewModel;

            _descriptionService = MainWindowViewModel.MapObjectDescriptionService;

            DataContext = this;
        }

        public string WaterBodyName
        {
            get => SelectedWaterBody != null ? SelectedWaterBody.Name : string.Empty;
            set
            {
                if (SelectedWaterBody != null)
                {
                    SelectedWaterBody.Name = value;
                    OnPropertyChanged();
                    MainWindowViewModel.CommandService.MarkProjectDataModified();
                }
            }
        }

        public string WaterBodyDescription
        {
            get => SelectedWaterBody != null ? SelectedWaterBody.Description : string.Empty;
            set
            {
                if (SelectedWaterBody != null)
                {
                    SelectedWaterBody.Description = value;
                    OnPropertyChanged();
                    MainWindowViewModel.CommandService.MarkProjectDataModified();
                }
            }
        }

        public ICommand CloseWaterBodyPropertiesCommand => new RelayCommand(() =>
        {
            MainWindowViewModel.CloseWaterBodyProperties();
        });

        private readonly ObjectCharacteristicsViewModel waterBodyCharacteristics = new();

        public ICommand SetWaterBodyCharacteristicsCommand => new RelayCommand(() =>
        {
            ObjectCharacteristics waterBodyObjectCharacteristicsDlg = new(waterBodyCharacteristics, MapObjectType.WaterFeature);
            waterBodyObjectCharacteristicsDlg.ShowDialog();
        });

        private bool _waterBodyNameLocked = false;

        public bool WaterBodyNameLocked
        {
            get { return _waterBodyNameLocked; }
            set
            {
                _waterBodyNameLocked = value;
                OnPropertyChanged();
            }
        }

        public ICommand LockWaterBodyNameCommand => new RelayCommand(() =>
        {
            WaterBodyNameLocked = !WaterBodyNameLocked;
        });

        public ICommand GenerateWaterBodyNameCommand => new RelayCommand(() =>
        {
            if (_waterBodyNameLocked)
            {
                return;
            }

            string generatedName = MainWindowViewModel.GenerateWaterFeatureName();

            if (!string.IsNullOrEmpty(generatedName) && SelectedWaterBody != null)
            {
                WaterBodyName = generatedName;
                MainWindowViewModel.CommandService.MarkMapModified();
            }
        });

        public ICommand GetWaterBodyDescriptionCommand => new RelayCommand(async () =>
        {
            if (SelectedWaterBody == null)
            {
                return;
            }

            if (waterBodyCharacteristics != null)
            {
                string query = _descriptionService.BuildAiQuery("WaterBody",
                    SelectedWaterBody.Name,
                    waterBodyCharacteristics.SelectedObjectType,
                    [.. waterBodyCharacteristics.ObjectCharacteristicsList]);

                try
                {
                    BeginWaterBodyPropertiesUpdates();

                    _descriptionService.ClearDescription();
                    await _descriptionService.GetMapObjectDescription(query);
                    string description = _descriptionService.ObjectDescription;

                    if (!string.IsNullOrEmpty(description))
                    {
                        WaterBodyDescription = description;
                        MainWindowViewModel.CommandService.MarkMapModified();
                    }
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("An error occurred retrieving an object description.", ex);
                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error retrieving water body description.", ex.Message);
                }
                finally
                {
                    WaterBodyPropertiesUpdatesComplete();
                }
            }
        });

        private void BeginWaterBodyPropertiesUpdates()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            GenerateWaterBodyDescriptionButton.IsEnabled = false;
            SetWaterBodyCharacteristicsButton.IsEnabled = false;
            CreateWaterBodyArticleButton.IsEnabled = false;
            WaterBodyPropertiesOkButton.IsEnabled = false;
        }

        private void WaterBodyPropertiesUpdatesComplete()
        {
            Mouse.OverrideCursor = null;

            GenerateWaterBodyDescriptionButton.IsEnabled = true;
            SetWaterBodyCharacteristicsButton.IsEnabled = true;
            CreateWaterBodyArticleButton.IsEnabled = true;
            WaterBodyPropertiesOkButton.IsEnabled = true;
        }


        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
