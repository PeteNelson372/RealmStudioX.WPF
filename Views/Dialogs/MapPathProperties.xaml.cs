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
    /// Interaction logic for MapPathProperties.xaml
    /// </summary>
    public partial class MapPathProperties : ModalDialog, INotifyPropertyChanged
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public MapPath SelectedPath { get; private set; }

        public MainWindowViewModel MainWindowViewModel { get; private set; }

        private readonly MapObjectDescriptionService _descriptionService;

        public MapPathProperties(MapPath path, MainWindowViewModel mainViewModel)
        {
            InitializeComponent();

            SelectedPath = path;
            MainWindowViewModel = mainViewModel;

            _descriptionService = MainWindowViewModel.MapObjectDescriptionService;

            DataContext = this;
        }

        public string PathName
        {
            get => SelectedPath != null ? SelectedPath.MapPathName : string.Empty;
            set
            {
                if (SelectedPath != null)
                {
                    SelectedPath.MapPathName = value;
                    OnPropertyChanged();
                    MainWindowViewModel.CommandService.MarkProjectDataModified();
                }
            }
        }

        public string PathDescription
        {
            get => SelectedPath != null ? SelectedPath.MapPathDescription : string.Empty;
            set
            {
                if (SelectedPath != null)
                {
                    SelectedPath.MapPathDescription = value;
                    OnPropertyChanged();
                    MainWindowViewModel.CommandService.MarkProjectDataModified();
                }
            }
        }

        public ICommand ClosePathPropertiesCommand => new RelayCommand(() =>
        {
            MainWindowViewModel.CloseMapPathProperties();
        });

        private readonly ObjectCharacteristicsViewModel pathCharacteristics = new();

        public ICommand SetPathCharacteristicsCommand => new RelayCommand(() =>
        {
            ObjectCharacteristics pathObjectCharacteristicsDlg = new(pathCharacteristics, MapObjectType.MapPath);
            pathObjectCharacteristicsDlg.ShowDialog();
        });

        private bool _pathNameLocked = false;

        public bool PathNameLocked
        {
            get { return _pathNameLocked; }
            set
            {
                _pathNameLocked = value;
                OnPropertyChanged();
            }
        }

        public ICommand LockPathNameCommand => new RelayCommand(() =>
        {
            PathNameLocked = !PathNameLocked;
        });

        public ICommand GeneratePathNameCommand => new RelayCommand(() =>
        {
            if (_pathNameLocked)
            {
                return;
            }

            string generatedName = MainWindowViewModel.GenerateName();

            if (!string.IsNullOrEmpty(generatedName) && SelectedPath != null)
            {
                PathName = generatedName;
                MainWindowViewModel.CommandService.MarkMapModified();
            }
        });

        public ICommand GetPathDescriptionCommand => new RelayCommand(async () =>
        {
            if (SelectedPath == null)
            {
                return;
            }

            if (pathCharacteristics != null)
            {
                string query = _descriptionService.BuildAiQuery("MapPath",
                    SelectedPath.MapPathName,
                    pathCharacteristics.SelectedObjectType,
                    [.. pathCharacteristics.ObjectCharacteristicsList]);

                try
                {
                    BeginPathPropertiesUpdates();

                    _descriptionService.ClearDescription();
                    await _descriptionService.GetMapObjectDescription(query);
                    string description = _descriptionService.ObjectDescription;

                    if (!string.IsNullOrEmpty(description))
                    {
                        PathDescription = description;
                    }
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("An error occurred retrieving an object description.", ex);
                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error retrieving path description.", ex.Message);
                }
                finally
                {
                    PathPropertiesUpdatesComplete();
                }
            }
        });

        private void BeginPathPropertiesUpdates()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            GeneratePathDescriptionButton.IsEnabled = false;
            SetPathCharacteristicsButton.IsEnabled = false;
            CreatePathArticleButton.IsEnabled = false;
            PathPropertiesOkButton.IsEnabled = false;
        }

        private void PathPropertiesUpdatesComplete()
        {
            Mouse.OverrideCursor = null;

            GeneratePathDescriptionButton.IsEnabled = true;
            SetPathCharacteristicsButton.IsEnabled = true;
            CreatePathArticleButton.IsEnabled = true;
            PathPropertiesOkButton.IsEnabled = true;
        }


        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
