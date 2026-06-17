using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.Models.Startup;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Dialogs;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class ProjectPanelViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        public MainWindowViewModel MainViewModel => _mainWindowViewModel;

        private readonly EditorController _editor;

        private readonly ProjectManager _projectManager;

        public ProjectPanelViewModel(MainWindowViewModel mainViewModel, EditorController editor, ProjectManager projectManager)
        {
            _mainWindowViewModel = mainViewModel;
            _editor = editor;
            _projectManager = projectManager;

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

        private void OnProjectChanged(object? sender, EventArgs e)
        {
            RefreshProject();
        }

        public ObservableCollection<ProjectMapTileViewModel> MapTiles { get; } = [];

        public MapProjectMetadata? Metadata => Project?.Metadata;

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

            MapTiles.Clear();

            foreach (MapProjectEntry entry in Project.Maps)
            {
                if (entry.Preview != null)
                {
                    MapTiles.Add(
                        new ProjectMapTileViewModel(entry));
                }
            }

            RealmStudioMap newActiveMap = _mainWindowViewModel.FindActiveMap(Project);

            _mainWindowViewModel.OpenMap(Project, newActiveMap);

            OnPropertyChanged(nameof(MapCount));
            OnPropertyChanged(nameof(Metadata));
        }

        public ICommand SaveProjectCommand => new RelayCommand(() =>
        {
            _mainWindowViewModel.SaveRealmProject();
        });

        public ICommand CreateMapCommand => new RelayCommand(() =>
        {
            var dialog = new CreateMapDialog();
            var result = dialog.ShowDialog();

            if (result != true || dialog.ViewModel.Result == null)
            {
                return;
            }

            if ((bool)result)
            {
                CreateOpenPackageResult dlgResult = dialog.ViewModel.Result;

                if (dlgResult != null && Project != null)
                {
                    if (dlgResult.CreationOperation == RealmCreationOperation.CreateMap)
                    {
                        RealmStudioMap newMap = _mainWindowViewModel.CreateMap(dlgResult);

                        MapProjectEntry entry = MapProjectHandler.CreateProjectEntry(newMap, null);

                        entry.Map = newMap;

                        Project.Maps.Add(entry);

                        _mainWindowViewModel.InitializeScene(newMap);

                        _projectManager.NotifyProjectChanged();
                    }
                }

            }
        });

        public ICommand OpenMapCommand => new RelayCommand(() =>
        {
            if (SelectedMapTile != null && Project != null)
            {
                RealmStudioMap selectedMap = SelectedMapTile.MapProjectEntry.Map;

                _mainWindowViewModel.OpenMap(Project, selectedMap);
            }
        });

        public ICommand DeleteMapCommand => new RelayCommand(() =>
        {
            // TODO: confirm deletion
            if (SelectedMapTile != null && Project != null)
            {
                if (Project.Maps.Count <= 1)
                {
                    // the project must contain at least 1 map
                    return;
                }

                MapProjectEntry selectedMapEntry = SelectedMapTile.MapProjectEntry;

                Cmd_DeleteMapFromProject cmd = new(_projectManager, Project, selectedMapEntry);

                _editor.Commands.Execute(cmd);
            }
        });
    }

    public sealed class ProjectMapTileViewModel
    {
        private readonly MapProjectEntry _entry;

        public ProjectMapTileViewModel(MapProjectEntry entry)
        {
            _entry = entry;
        }

        public string MapName => _entry.Map.MapName;

        public RealmMapType RealmType => _entry.Metadata.RealmType;

        public MapProjectEntry MapProjectEntry => _entry;

        public ImageSource? PreviewImage => _entry.Preview?.ToImageSource();
    }
}
