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
    /// Interaction logic for RiverProperties.xaml
    /// </summary>
    public partial class RiverProperties : ModalDialog, INotifyPropertyChanged
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public River SelectedRiver { get; private set; }

        public MainWindowViewModel MainWindowViewModel { get; private set; }

        private readonly MapObjectDescriptionService _descriptionService;

        public RiverProperties(River river, MainWindowViewModel mainViewModel)
        {
            InitializeComponent();

            SelectedRiver = river;
            MainWindowViewModel = mainViewModel;

            _descriptionService = MainWindowViewModel.MapObjectDescriptionService;

            DataContext = this;
        }

        public string RiverName
        {
            get => SelectedRiver != null ? SelectedRiver.Name : string.Empty;
            set
            {
                if (SelectedRiver != null)
                {
                    SelectedRiver.Name = value;
                    OnPropertyChanged();
                    MainWindowViewModel.CommandService.MarkProjectDataModified();
                }
            }
        }

        public string RiverDescription
        {
            get => SelectedRiver != null ? SelectedRiver.Description : string.Empty;
            set
            {
                if (SelectedRiver != null)
                {
                    SelectedRiver.Description = value;
                    OnPropertyChanged();
                    MainWindowViewModel.CommandService.MarkProjectDataModified();
                }
            }
        }

        public ICommand CloseRiverPropertiesCommand => new RelayCommand(() =>
        {
            MainWindowViewModel.CloseRiverProperties();
        });

        private readonly ObjectCharacteristicsViewModel riverCharacteristics = new();

        public ICommand SetRiverCharacteristicsCommand => new RelayCommand(() =>
        {
            ObjectCharacteristics riverObjectCharacteristicsDlg = new(riverCharacteristics, MapObjectType.River);
            riverObjectCharacteristicsDlg.ShowDialog();
        });

        private bool _riverNameLocked = false;

        public bool RiverNameLocked
        {
            get { return _riverNameLocked; }
            set
            {
                _riverNameLocked = value;
                OnPropertyChanged();
            }
        }

        public ICommand LockRiverNameCommand => new RelayCommand(() =>
        {
            RiverNameLocked = !RiverNameLocked;
        });

        public ICommand GenerateRiverNameCommand => new RelayCommand(() =>
        {
            if (_riverNameLocked)
            {
                return;
            }

            string generatedName = MainWindowViewModel.GenerateWaterFeatureName();

            if (!string.IsNullOrEmpty(generatedName) && SelectedRiver != null)
            {
                RiverName = generatedName;
                MainWindowViewModel.CommandService.MarkMapModified();
            }
        });

        public ICommand GetRiverDescriptionCommand => new RelayCommand(async () =>
        {
            if (SelectedRiver == null)
            {
                return;
            }

            if (riverCharacteristics != null)
            {
                string query = _descriptionService.BuildAiQuery("River",
                    SelectedRiver.Name,
                    riverCharacteristics.SelectedObjectType,
                    [.. riverCharacteristics.ObjectCharacteristicsList]);

                try
                {
                    BeginRiverPropertiesUpdates();

                    _descriptionService.ClearDescription();
                    await _descriptionService.GetMapObjectDescription(query);
                    string description = _descriptionService.ObjectDescription;

                    if (!string.IsNullOrEmpty(description))
                    {
                        RiverDescription = description;
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
                    RiverPropertiesUpdatesComplete();
                }
            }
        });

        private void BeginRiverPropertiesUpdates()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            GenerateRiverDescriptionButton.IsEnabled = false;
            SetRiverCharacteristicsButton.IsEnabled = false;
            CreateRiverArticleButton.IsEnabled = false;
            RiverPropertiesOkButton.IsEnabled = false;
        }

        private void RiverPropertiesUpdatesComplete()
        {
            Mouse.OverrideCursor = null;

            GenerateRiverDescriptionButton.IsEnabled = true;
            SetRiverCharacteristicsButton.IsEnabled = true;
            CreateRiverArticleButton.IsEnabled = true;
            RiverPropertiesOkButton.IsEnabled = true;
        }


        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
