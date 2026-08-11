using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.Editor.Services;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.Models.Startup;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Dialogs;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using Cursors = System.Windows.Input.Cursors;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class ProjectPanelViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        public MainWindowViewModel MainViewModel => _mainWindowViewModel;

        private readonly EditorController _editor;

        private readonly ProjectManager _projectManager;

        private readonly MapObjectDescriptionService _descriptionService;

        public ProjectPanelViewModel(MainWindowViewModel mainViewModel, EditorController editor,
            ProjectManager projectManager, MapObjectDescriptionService descriptionService)
        {
            _mainWindowViewModel = mainViewModel;
            _editor = editor;
            _projectManager = projectManager;
            _descriptionService = descriptionService;

            _projectManager.ProjectChanged += OnProjectChanged;
        }


        private RealmStudioProject? _project;

        public RealmStudioProject? Project
        {
            get => _project;
            private set
            {
                _project = value;
                RefreshProject();
                OnPropertyChanged();
            }
        }

        public string ProjectName
        {
            get => Project != null ? Project.Metadata.ProjectName : string.Empty;
            set
            {
                if (Project != null)
                {
                    Project.Metadata.ProjectName = value;
                    OnPropertyChanged();
                    RefreshProjectMetadata();
                    MainViewModel.CommandService.MarkProjectDataModified();
                }
            }
        }

        private void OnProjectChanged(object? sender, EventArgs e)
        {
            RefreshProject();
        }

        public ObservableCollection<ProjectMapTileViewModel> MapTiles { get; } = [];

        public MapProjectMetadata? Metadata
        {
            get { return Project?.Metadata; }
        }

        public int MapCount => MapTiles?.Count ?? 0;

        private ProjectMapTileViewModel? _selectedMapTile;
        public ProjectMapTileViewModel? SelectedMapTile
        {
            get { return _selectedMapTile; }
            set
            {
                _selectedMapTile = value;
                OnPropertyChanged();
            }
        }

        public void LoadProject(RealmStudioProject project)
        {
            Project = project;

            RefreshProject();

            OnPropertyChanged(nameof(Project));
        }

        public void RefreshProject()
        {
            if (Project == null)
            {
                return;
            }

            RefreshMapTiles();

            RealmStudioMap newActiveMap = MainWindowViewModel.FindActiveMap(Project);

            _mainWindowViewModel.OpenMap(Project, newActiveMap);

            OnPropertyChanged(nameof(MapCount));
            OnPropertyChanged(nameof(Metadata));
        }

        public void RefreshProjectMetadata()
        {
            OnPropertyChanged(nameof(Project));
            OnPropertyChanged(nameof(Metadata));
        }

        public void RefreshMaps()
        {
            OnPropertyChanged(nameof(Project));
            OnPropertyChanged(nameof(SelectedMapTile));
            OnPropertyChanged(nameof(MapTiles));
        }

        public void RefreshMapTiles()
        {
            if (Project == null)
            {
                return;
            }

            MapTiles.Clear();

            foreach (MapProjectEntry entry in Project.Maps)
            {
                if (entry.Preview != null)
                {
                    MapTiles.Add(
                        new ProjectMapTileViewModel(entry, MainViewModel.CommandService));
                }
            }
        }

        public ICommand SaveProjectCommand => new RelayCommand(() =>
        {
            try
            {
                _mainWindowViewModel.SaveRealmProject();
            }
            catch (Exception ex)
            {
                RealmStudioXLogger.Exception("SaveProjectCommand", ex);
                MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error Saving Project", "An error occured saving the project. Check the log file for details.");
                dlg.ShowDialog();
            }
        });

        public ICommand CreateMapCommand => new RelayCommand(() =>
        {
            WindowManager wm = MainWindowViewModel.WindowManager;

            CreateMapDialog dialog = wm.Create<CreateMapDialog>();
            dialog.SetThemeManager(MainViewModel.ThemeManager);

            var result = dialog.ShowDialog();

            if (result != true || dialog.ViewModel.Result == null)
            {
                return;
            }

            if ((bool)result)
            {
                try
                {
                    CreateOpenPackageResult dlgResult = dialog.ViewModel.Result;

                    if (dlgResult != null && Project != null)
                    {
                        if (dlgResult.CreationOperation == RealmCreationOperation.CreateMap)
                        {
                            RealmStudioMap newMap = MainWindowViewModel.CreateMap(dlgResult);

                            MapProjectEntry entry = MapProjectHandler.CreateProjectEntry(newMap, null);

                            entry.Map = newMap;

                            Project.Maps.Add(entry);
                            Project.ActiveMapId = newMap.MapId;

                            _mainWindowViewModel.InitializeScene(newMap);

                            _mainWindowViewModel.CommandService.MarkProjectDataModified();
                            _projectManager.NotifyProjectChanged();

                            _mainWindowViewModel.MapName = Project.Metadata.ProjectName + ": " + newMap.MapName;
                            _mainWindowViewModel.MapSizeLabel = $"Map Size: {newMap.MapWidth} x {newMap.MapHeight}, Map Area: {newMap.MapAreaWidth} x {newMap.MapAreaHeight} {newMap.MapAreaUnits}";

                            _editor.UpdateMapScene();

                            _mainWindowViewModel.SetDrawingLayerLabel();

                            string? themeName = dlgResult.Theme;

                            if (!string.IsNullOrEmpty(themeName))
                            {
                                _mainWindowViewModel.FindAndApplyTheme(themeName);
                            }
                            else
                            {
                                _mainWindowViewModel.FindAndApplyDefaultTheme();
                            }

                            _editor.State.StatusMessage = $"Map {newMap.MapName} created.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("CreateMapCommand", ex);
                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error Creating Map", "An error occured creating the map. Check the log file for details.");
                    dlg.ShowDialog();
                }
            }
        });

        public ICommand OpenMapCommand => new RelayCommand(() =>
        {
            if (SelectedMapTile != null && Project != null)
            {
                try
                {
                    RealmStudioMap selectedMap = SelectedMapTile.MapProjectEntry.Map;

                    _mainWindowViewModel.OpenMap(Project, selectedMap);
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("OpenMapCommand", ex);
                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error Opening Map", "An error occured opening the map. Check the log file for details.");
                    dlg.ShowDialog();
                }
            }
        });

        public ICommand DeleteMapCommand => new RelayCommand(() =>
        {
            if (SelectedMapTile != null && Project != null)
            {
                try
                {
                    if (Project.Maps.Count <= 1)
                    {
                        // the project must contain at least 1 map
                        return;
                    }

                    MessageDialog dlg = MessageDialogFactory.DeleteConfirmationDialog("Delete Map", "Are you sure you want to delete the selected map?\nThe deletion can be undone.");

                    dlg.ShowDialog();

                    if (((MessageDialogViewModel)dlg.DataContext).Result == MessageDialogResult.Delete)
                    {
                        MapProjectEntry selectedMapEntry = SelectedMapTile.MapProjectEntry;

                        Cmd_DeleteMapFromProject cmd = new(_projectManager, Project, selectedMapEntry);

                        _mainWindowViewModel.CommandService.ActiveCommands.Execute(cmd);
                    }
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("DeleteMapCommand", ex);
                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error Deleting Map", "An error occured deleting the map from the project. CHeck the log file for details.");
                    dlg.ShowDialog();
                }
            }
        });

        public ICommand ImportMapCommand => new RelayCommand(() =>
        {
            OpenFileDialog ofd = new()
            {
                InitialDirectory = AssetManager.RootRealmsDirectory,
                Filter = "RealmStudioX Map|*.rsmx",
                Title = "Open RealmStudioX Map"
            };

            DialogResult result = ofd.ShowDialog();

            if (result == DialogResult.OK && Project != null)
            {
                try
                {
                    RealmStudioMap? newMap = MapFileMethods.OpenMap(ofd.FileName);

                    if (newMap != null)
                    {
                        // does the map already exist in the project?
                        foreach (MapProjectEntry mapEntry in Project.Maps)
                        {
                            if (mapEntry.MapId == newMap.MapId)
                            {
                                // TODO: allow the imported map to be renamed and given a new id, then imported?
                                // This would be useful if creating a base map, then creating additional maps (e.g. political boundaries, etc.) from it

                                // map already exists - cannot import it
                                MessageDialog dlg = MessageDialogFactory.ErrorDialog("Cannot Import Map", "The selected map is already in the map project.");
                                dlg.ShowDialog();

                                return;
                            }
                        }

                        _mainWindowViewModel.OpenMap(Project, newMap);

                        // create a bitmap with the same aspect ratio as the map
                        using SKBitmap previewFull = new(newMap.MapWidth, newMap.MapHeight);
                        using SKCanvas canvas = new(previewFull);

                        _editor.Scene!.RenderForExport(canvas);

                        using SKBitmap preview = Utilities.ResizeBitmap(previewFull, 200, 200 * newMap.MapHeight / newMap.MapWidth);
                        string mapPreviewFileName = newMap.MapId + ".png";

                        MapProjectEntry entry = MapProjectHandler.CreateProjectEntry(newMap, preview);

                        MapProjectMetadata projectMeta = Project.Metadata;
                        projectMeta.Modified = DateTime.Now;

                        Project.Maps.Add(entry);
                        Project.ActiveMapId = newMap.MapId;

                        _mainWindowViewModel.CommandService.MarkProjectDataModified();
                        _projectManager.NotifyProjectChanged();
                    }
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("ImportMapCommand", ex);
                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error Importing Map", "An error occured importing the map. Please verify that the map file is valid. Check the log file for details.");
                    dlg.ShowDialog();
                }
            }
        });

        public ICommand ExportMapCommand => new RelayCommand(() =>
        {
            if (SelectedMapTile != null && Project != null)
            {
                try
                {
                    RealmStudioMap selectedMap = SelectedMapTile.MapProjectEntry.Map;

                    string mapXml = MapFileMethods.SerializeMap(selectedMap);

                    string fileName = selectedMap.MapName + RealmStudioFileFormat.RawMapExtension;

                    SaveFileDialog saveMapDialog = new()
                    {
                        InitialDirectory = AssetManager.RootRealmsDirectory,
                        FileName = fileName,
                        Filter = "RealmStudioX Map|*.rsmx",
                        Title = "Save RealmStudioX Map"
                    };

                    saveMapDialog.ShowDialog();

                    // If the file name is not an empty string open it for saving.
                    if (saveMapDialog.FileName != "")
                    {
                        // Saves the Image via a FileStream created by the OpenFile method.
                        System.IO.FileStream fs =
                            (System.IO.FileStream)saveMapDialog.OpenFile();

                        MapFileMethods.SaveMap(selectedMap, fs);
                    }
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("ExportMapCommand", ex);
                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error Exporting Map", "An error occured exporting the map. Check the log file for details.");
                    dlg.ShowDialog();
                }
            }
        });


        public string SelectedMapName
        {
            get => SelectedMapTile?.MapProjectEntry.Map.MapName ?? "";

            set
            {
                if (SelectedMapTile == null)
                    return;

                SelectedMapTile.MapProjectEntry.Map.MapName = value;

                RefreshMaps();

                OnPropertyChanged();
            }
        }

        public ICommand RenameMapCommand => new RelayCommand(() =>
        {
            if (SelectedMapTile != null && Project != null)
            {
                RealmStudioMap selectedMap = SelectedMapTile.MapProjectEntry.Map;
                string oldMapName = selectedMap.MapName;

                RenameDialog renameDialog = new()
                {
                    DataContext = this
                };

                bool? result = renameDialog.ShowDialog();

                if (result == true)
                {
                    if (SelectedMapName != oldMapName && !string.IsNullOrEmpty(SelectedMapName))
                    {
                        if (UserInterfaceUtilities.IsValidFileName(SelectedMapName))
                        {
                            _mainWindowViewModel.CommandService.MarkProjectDataModified();
                            RefreshMapTiles();
                        }
                        else
                        {
                            SelectedMapName = oldMapName;
                        }
                    }
                }
            }
        });


        // -------------------------
        // Realm Properties
        // -------------------------

        RealmProperties? _realmPropertiesDlg = null;

        public void OpenProjectPropertiesDialog()
        {
            try
            {
                _realmPropertiesDlg = new()
                {
                    Owner = System.Windows.Application.Current.MainWindow,
                    DataContext = this
                };

                _realmPropertiesDlg.ShowDialog();
            }
            finally
            {
            }
        }

        public ICommand CloseRealmPropertiesCommand => new RelayCommand(() =>
        {
            CloseProjectPropertiesDialog();
        });

        private void BeginRealmPropertiesUpdates()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            if (_realmPropertiesDlg != null)
            {
                _realmPropertiesDlg.GenerateRealmDescriptionButton.IsEnabled = false;
                _realmPropertiesDlg.GenerateMapDescriptionButton.IsEnabled = false;
                _realmPropertiesDlg.SetRealmCharacteristicsButton.IsEnabled = false;
                _realmPropertiesDlg.SetMapCharacteristicsButton.IsEnabled = false;
                _realmPropertiesDlg.CreateRealmArticleButton.IsEnabled = false;
                _realmPropertiesDlg.CreateMapArticleButton.IsEnabled = false;

                _realmPropertiesDlg.RealmPropertiesOkButton.IsEnabled = false;
            }
        }

        private void RealmPropertiesUpdatesComplete()
        {
            Mouse.OverrideCursor = null;

            if (_realmPropertiesDlg != null)
            {
                _realmPropertiesDlg.GenerateRealmDescriptionButton.IsEnabled = true;
                _realmPropertiesDlg.GenerateMapDescriptionButton.IsEnabled = true;
                _realmPropertiesDlg.SetRealmCharacteristicsButton.IsEnabled = true;
                _realmPropertiesDlg.SetMapCharacteristicsButton.IsEnabled = true;
                _realmPropertiesDlg.CreateRealmArticleButton.IsEnabled = true;
                _realmPropertiesDlg.CreateMapArticleButton.IsEnabled = true;

                _realmPropertiesDlg.RealmPropertiesOkButton.IsEnabled = true;
            }
        }

        internal void CloseProjectPropertiesDialog()
        {
            _realmPropertiesDlg?.Close();
            _realmPropertiesDlg = null;
        }

        private readonly ObjectCharacteristicsViewModel realmCharacteristics = new();

        public ICommand SetRealmCharacteristicsCommand => new RelayCommand(() =>
        {
            ObjectCharacteristics realmObjectCharacteristicsDlg = new(realmCharacteristics, MapObjectType.Realm);
            realmObjectCharacteristicsDlg.ShowDialog();
        });

        public ICommand GetRealmDescriptionCommand => new RelayCommand(async () =>
        {
            if (Project == null)
            {
                return;
            }

            if (realmCharacteristics != null)
            {
                string query = _descriptionService.BuildAiQuery("RealmStudioProject",
                    ProjectName,
                    realmCharacteristics.SelectedObjectType,
                    [.. realmCharacteristics.ObjectCharacteristicsList]);

                try
                {
                    BeginRealmPropertiesUpdates();

                    _descriptionService.ClearDescription();
                    await _descriptionService.GetMapObjectDescription(query);
                    string description = _descriptionService.ObjectDescription;

                    if (!string.IsNullOrEmpty(description))
                    {
                        Project.Metadata.Description = description;
                        RefreshProjectMetadata();
                        RefreshMaps();
                        MainViewModel.CommandService.MarkProjectDataModified();
                    }
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("An error occurred retrieving an object description.", ex);
                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error retrieving realm project description.", ex.Message);
                }
                finally
                {
                    RealmPropertiesUpdatesComplete();
                }
            }
        });

        public ICommand LockRealmNameCommand => new RelayCommand(() =>
        {
            RealmNameLocked = !RealmNameLocked;
        });

        public ICommand GenerateRealmNameCommand => new RelayCommand(() =>
        {
            if (_realmNameLocked)
            {
                return;
            }

            string generatedName = MainWindowViewModel.GenerateName();

            if (!string.IsNullOrEmpty(generatedName) && Project != null)
            {
                ProjectName = generatedName;
            }
        });

        public ICommand GenerateMapNameCommand => new RelayCommand(() =>
        {
            if (_mapNameLocked)
            {
                return;
            }

            string generatedName = MainWindowViewModel.GenerateName();

            if (!string.IsNullOrEmpty(generatedName) && SelectedMapTile != null)
            {
                SelectedMapTile.MapName = generatedName;
                MainViewModel.CommandService.MarkMapModified();
            }
        });


        private bool _realmNameLocked = false;

        public bool RealmNameLocked
        {
            get { return _realmNameLocked; }
            set
            {
                _realmNameLocked = value;
                OnPropertyChanged();
            }
        }

        private bool _mapNameLocked = false;

        public bool MapNameLocked
        {
            get { return _mapNameLocked; }
            set
            {
                _mapNameLocked = value;
                OnPropertyChanged();
            }
        }

        public ICommand LockMapNameCommand => new RelayCommand(() =>
        {
            MapNameLocked = !MapNameLocked;
        });

        // -------------------------
        // Selected Map Characteristics and Description
        // -------------------------

        private readonly Dictionary<string, ObjectCharacteristicsViewModel> mapCharacteristicsList = [];

        public ICommand SetSelectedMapCharacteristicsCommand => new RelayCommand(() =>
        {
            if (SelectedMapTile == null)
            {
                return;
            }

            string mapId = SelectedMapTile.MapProjectEntry.MapId;
            ObjectCharacteristicsViewModel? mapCharacteristics;

            if (!mapCharacteristicsList.TryGetValue(mapId, out mapCharacteristics))
            {
                mapCharacteristics = new();
                mapCharacteristicsList.Add(mapId, mapCharacteristics);
            }

            ObjectCharacteristics selectedMapCharacteristicsDlg = new(mapCharacteristics, MapObjectType.Map);
            selectedMapCharacteristicsDlg.ShowDialog();
        });

        public ICommand GetSelectedMapDescriptionCommand => new RelayCommand(async () =>
        {
            if (SelectedMapTile == null)
            {
                return;
            }

            string mapId = SelectedMapTile.MapProjectEntry.MapId;
            ObjectCharacteristicsViewModel? mapCharacteristics;

            if (!mapCharacteristicsList.TryGetValue(mapId, out mapCharacteristics))
            {
                mapCharacteristics = new();
                mapCharacteristicsList.Add(mapId, mapCharacteristics);
            }

            if (mapCharacteristics != null)
            {
                string mapName = SelectedMapTile.MapProjectEntry.Map.MapName;
                string query = _descriptionService.BuildAiQuery("RealmStudioMap",
                    mapName,
                    mapCharacteristics.SelectedObjectType,
                    mapCharacteristics.ObjectCharacteristicsList.ToList());

                try
                {
                    BeginRealmPropertiesUpdates();

                    _descriptionService.ClearDescription();
                    await _descriptionService.GetMapObjectDescription(query);
                    string description = _descriptionService.ObjectDescription;

                    if (!string.IsNullOrEmpty(description))
                    {
                        SelectedMapTile.MapProjectEntry.Map.RealmDescription = description;
                        RefreshMaps();
                        MainViewModel.CommandService.MarkMapModified();
                    }
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("An error occurred retrieving an object description.", ex);
                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error retrieving map description.", ex.Message);
                }
                finally
                {
                    RealmPropertiesUpdatesComplete();
                }
            }
        });
    }

    public sealed class ProjectMapTileViewModel(MapProjectEntry entry, CommandService cmdService) : ViewModelBase
    {
        private readonly CommandService _cmdService = cmdService;

        public string MapName
        {
            get => entry.Map.MapName;
            set
            {
                if (entry.Map.MapName == value)
                {
                    return;
                }

                if (!UserInterfaceUtilities.IsValidFileName(value))
                {
                    return;
                }

                entry.Map.MapName = value;
                OnPropertyChanged();
                _cmdService.MarkMapModified();
            }
        }

        public string MapDescription
        {
            get => entry.Map.RealmDescription;
            set
            {
                if (entry.Map.RealmDescription == value)
                    return;

                entry.Map.RealmDescription = value;
                OnPropertyChanged();
                _cmdService.MarkMapModified();
            }
        }

        public RealmMapType RealmType => entry.Metadata.RealmType;

        public MapProjectEntry MapProjectEntry => entry;

        public string Dimensions => MapProjectEntry.Map.MapWidth.ToString() + "w x " + MapProjectEntry.Map.MapHeight.ToString() +"h";

        public ImageSource? PreviewImage => entry.Preview?.ToImageSource();
    }
}
