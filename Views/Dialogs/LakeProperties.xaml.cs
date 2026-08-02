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
    /// Interaction logic for LakeProperties.xaml
    /// </summary>
    public partial class LakeProperties : ModalDialog, INotifyPropertyChanged
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public Lake SelectedLake { get; private set; }

        public MainWindowViewModel MainWindowViewModel { get; private set; }

        private readonly MapObjectDescriptionService _descriptionService;

        public LakeProperties(Lake lake, MainWindowViewModel mainViewModel)
        {
            InitializeComponent();

            SelectedLake = lake;
            MainWindowViewModel = mainViewModel;

            _descriptionService = MainWindowViewModel.MapObjectDescriptionService;

            DataContext = this;
        }

        public string LakeName
        {
            get => SelectedLake != null ? SelectedLake.Name : string.Empty;
            set
            {
                if (SelectedLake != null)
                {
                    SelectedLake.Name = value;
                    OnPropertyChanged();
                    MainWindowViewModel.CommandService.MarkProjectDataModified();
                }
            }
        }

        public string LakeDescription
        {
            get => SelectedLake != null ? SelectedLake.Description : string.Empty;
            set
            {
                if (SelectedLake != null)
                {
                    SelectedLake.Description = value;
                    OnPropertyChanged();
                    MainWindowViewModel.CommandService.MarkProjectDataModified();
                }
            }
        }

        public ICommand CloseLakePropertiesCommand => new RelayCommand(() =>
        {
            MainWindowViewModel.CloseLakeProperties();
        });

        private readonly ObjectCharacteristicsViewModel lakeCharacteristics = new();

        public ICommand SetLakeCharacteristicsCommand => new RelayCommand(() =>
        {
            ObjectCharacteristics lakeObjectCharacteristicsDlg = new(lakeCharacteristics, MapObjectType.Lake);
            lakeObjectCharacteristicsDlg.ShowDialog();
        });

        private bool _lakeNameLocked = false;

        public bool LakeNameLocked
        {
            get { return _lakeNameLocked; }
            set
            {
                _lakeNameLocked = value;
                OnPropertyChanged();
            }
        }

        public ICommand LockLakeNameCommand => new RelayCommand(() =>
        {
            LakeNameLocked = !LakeNameLocked;
        });

        public ICommand GenerateLakeNameCommand => new RelayCommand(() =>
        {
            if (_lakeNameLocked)
            {
                return;
            }

            string generatedName = MainWindowViewModel.GenerateWaterFeatureName();

            if (!string.IsNullOrEmpty(generatedName) && SelectedLake != null)
            {
                LakeName = generatedName;
                MainWindowViewModel.CommandService.MarkMapModified();
            }
        });

        public ICommand GetLakeDescriptionCommand => new RelayCommand(async () =>
        {
            if (SelectedLake == null)
            {
                return;
            }

            if (lakeCharacteristics != null)
            {
                string query = _descriptionService.BuildAiQuery("Lake",
                    SelectedLake.Name,
                    lakeCharacteristics.SelectedObjectType,
                    [.. lakeCharacteristics.ObjectCharacteristicsList]);

                try
                {
                    BeginLakePropertiesUpdates();

                    _descriptionService.ClearDescription();
                    await _descriptionService.GetMapObjectDescription(query);
                    string description = _descriptionService.ObjectDescription;

                    if (!string.IsNullOrEmpty(description))
                    {
                        LakeDescription = description;
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
                    LakePropertiesUpdatesComplete();
                }
            }
        });

        private void BeginLakePropertiesUpdates()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            GenerateLakeDescriptionButton.IsEnabled = false;
            SetLakeCharacteristicsButton.IsEnabled = false;
            CreateLakeArticleButton.IsEnabled = false;
            LakePropertiesOkButton.IsEnabled = false;
        }

        private void LakePropertiesUpdatesComplete()
        {
            Mouse.OverrideCursor = null;

            GenerateLakeDescriptionButton.IsEnabled = true;
            SetLakeCharacteristicsButton.IsEnabled = true;
            CreateLakeArticleButton.IsEnabled = true;
            LakePropertiesOkButton.IsEnabled = true;
        }


        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
