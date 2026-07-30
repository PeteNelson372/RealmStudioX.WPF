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
    /// Interaction logic for LandformProperties.xaml
    /// </summary>
    public partial class LandformProperties : ModalDialog, INotifyPropertyChanged
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public Landform SelectedLandform { get; private set; }

        public MainWindowViewModel MainWindowViewModel { get; private set; }

        private readonly MapObjectDescriptionService _descriptionService;

        public LandformProperties(Landform landform, MainWindowViewModel mainViewModel)
        {
            InitializeComponent();

            SelectedLandform = landform;
            MainWindowViewModel = mainViewModel;

            _descriptionService = MainWindowViewModel.MapObjectDescriptionService;

            DataContext = this;
        }

        public string LandformName
        {
            get => SelectedLandform != null ? SelectedLandform.LandformName : string.Empty;
            set
            {
                if (SelectedLandform != null)
                {
                    SelectedLandform.LandformName = value;
                    OnPropertyChanged();
                    MainWindowViewModel.CommandService.MarkProjectDataModified();
                }
            }
        }

        public string LandformDescription
        {
            get => SelectedLandform != null ? SelectedLandform.LandformDescription : string.Empty;
            set
            {
                if (SelectedLandform != null)
                {
                    SelectedLandform.LandformDescription = value;
                    OnPropertyChanged();
                    MainWindowViewModel.CommandService.MarkProjectDataModified();
                }
            }
        }

        public ICommand CloseLandformPropertiesCommand => new RelayCommand(() =>
        {
            MainWindowViewModel.CloseLandformProperties();
        });

        private readonly ObjectCharacteristicsViewModel landformCharacteristics = new();

        public ICommand SetLandformCharacteristicsCommand => new RelayCommand(() =>
        {
            ObjectCharacteristics landformObjectCharacteristicsDlg = new(landformCharacteristics, MapObjectType.Landform);
            landformObjectCharacteristicsDlg.ShowDialog();
        });

        private bool _landformNameLocked = false;

        public bool LandformNameLocked
        {
            get { return _landformNameLocked; }
            set
            {
                _landformNameLocked = value;
                OnPropertyChanged();
            }
        }

        public ICommand LockLandformNameCommand => new RelayCommand(() =>
        {
            LandformNameLocked = !LandformNameLocked;
        });

        public ICommand GenerateLandformNameCommand => new RelayCommand(() =>
        {
            if (_landformNameLocked)
            {
                return;
            }

            string generatedName = MainWindowViewModel.GenerateName();

            if (!string.IsNullOrEmpty(generatedName) && SelectedLandform != null)
            {
                LandformName = generatedName;
                MainWindowViewModel.CommandService.MarkMapModified();
            }
        });

        public ICommand GetLandformDescriptionCommand => new RelayCommand(async () =>
        {
            if (SelectedLandform == null)
            {
                return;
            }

            if (landformCharacteristics != null)
            {
                string query = _descriptionService.BuildAiQuery("Landform",
                    SelectedLandform.LandformName,
                    landformCharacteristics.SelectedObjectType,
                    [.. landformCharacteristics.ObjectCharacteristicsList]);

                try
                {
                    BeginLandformPropertiesUpdates();

                    _descriptionService.ClearDescription();
                    await _descriptionService.GetMapObjectDescription(query);
                    string description = _descriptionService.ObjectDescription;

                    if (!string.IsNullOrEmpty(description))
                    {
                        LandformDescription = description;
                    }
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("An error occurred retrieving an object description.", ex);
                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error retrieving landform description.", ex.Message);
                }
                finally
                {
                    LandformPropertiesUpdatesComplete();
                }
            }
        });

        private void BeginLandformPropertiesUpdates()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            GenerateLandformDescriptionButton.IsEnabled = false;
            SetLandformCharacteristicsButton.IsEnabled = false;
            CreateLandformArticleButton.IsEnabled = false;
            LandformPropertiesOkButton.IsEnabled = false;
        }

        private void LandformPropertiesUpdatesComplete()
        {
            Mouse.OverrideCursor = null;

            GenerateLandformDescriptionButton.IsEnabled = true;
            SetLandformCharacteristicsButton.IsEnabled = true;
            CreateLandformArticleButton.IsEnabled = true;
            LandformPropertiesOkButton.IsEnabled = true;
        }


        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
