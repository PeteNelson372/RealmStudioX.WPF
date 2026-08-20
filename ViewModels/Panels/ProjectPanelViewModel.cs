using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.Editor.Services;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.Models.Map;
using RealmStudioX.WPF.Models.Startup;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Dialogs;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Xml;
using System.Xml.Serialization;
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

                        if (Project != null && newMap != null)
                        {
                            ImportMapIntoProject(Project, newMap);

                            _mainWindowViewModel.CommandService.MarkProjectDataModified();
                            _projectManager.NotifyProjectChanged();
                        }
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

        public void ImportMapIntoProject(RealmStudioProject project, RealmStudioMap newMap)
        {
            _mainWindowViewModel.OpenMap(project, newMap);

            // create a bitmap with the same aspect ratio as the map
            using SKBitmap previewFull = new(newMap.MapWidth, newMap.MapHeight);
            using SKCanvas canvas = new(previewFull);

            _editor.Scene!.RenderForExport(canvas);

            using SKBitmap preview = Utilities.ResizeBitmap(previewFull, 200, 200 * newMap.MapHeight / newMap.MapWidth);
            string mapPreviewFileName = newMap.MapId + ".png";

            MapProjectEntry entry = MapProjectHandler.CreateProjectEntry(newMap, preview);

            MapProjectMetadata projectMeta = project.Metadata;
            projectMeta.Modified = DateTime.Now;

            project.Maps.Add(entry);
            project.ActiveMapId = newMap.MapId;
        }

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

        internal RealmStudioMap? CreateMapFromMap(ResizeMapResult dlgResult,
            RealmStudioMap currentMap,
            bool includeTerrainSymbols,
            bool includeVegetationSymbols,
            bool includeStructureSymbols,
            bool includeMarkerSymbols,
            bool includeLabels,
            bool includeBoxes,
            bool includePaths,
            bool includeScale,
            bool includeGrid,
            bool includeRegions,
            bool includeDrawnShapes,
            bool includeHeightMap)
        {
            RealmStudioMap? resizedMap = MainWindowViewModel.CreateMap(dlgResult);

            if (resizedMap == null)
            {
                return null;
            }

            resizedMap.MarkChanged();

            bool scaleToFit = false;

            if (resizedMap.MapWidth < currentMap.MapWidth && resizedMap.MapHeight < currentMap.MapHeight)
            {
                scaleToFit = true;
                dlgResult.AnchorPoint = ResizeMapAnchorPoint.TopLeft;
            }

            // the location and size of each symbol, landform, painted color, paths, and labels
            // has to be determined based on the location, size, and scale of the current map
            // versus the new map
            float scaleX = resizedMap.MapWidth / dlgResult.SelectedArea.Width;
            float scaleY = resizedMap.MapHeight / dlgResult.SelectedArea.Height;

            resizedMap.MapAreaWidth = currentMap.MapAreaWidth * scaleX;
            resizedMap.MapAreaHeight = currentMap.MapAreaHeight * scaleY;

            float deltaX = -dlgResult.SelectedArea.Left * scaleX;
            float deltaY = -dlgResult.SelectedArea.Top * scaleY;

            SKRect selectedMapArea = dlgResult.SelectedArea;

            // determine the scale and translation based on the anchor point
            // CenterZoomed and DetailMap cases are the same and the
            // default calculations for scale and translation above are used

            switch (dlgResult.AnchorPoint)
            {
                case ResizeMapAnchorPoint.Center:
                    if (scaleToFit)
                    {
                        scaleX = resizedMap.MapWidth / dlgResult.SelectedArea.Width;
                        scaleY = resizedMap.MapHeight / dlgResult.SelectedArea.Height;

                        deltaX = 0;
                        deltaY = 0;
                    }
                    else
                    {
                        scaleX = 1.0F;
                        scaleY = 1.0F;

                        deltaX = -(currentMap.MapWidth - resizedMap.MapWidth) / 2;
                        deltaY = -(currentMap.MapHeight - resizedMap.MapHeight) / 2;
                    }
                    break;
                case ResizeMapAnchorPoint.TopLeft:
                    {
                        if (scaleToFit)
                        {
                            scaleX = resizedMap.MapWidth / dlgResult.SelectedArea.Width;
                            scaleY = resizedMap.MapHeight / dlgResult.SelectedArea.Height;

                            deltaX = 0;
                            deltaY = 0;
                        }
                        else
                        {
                            scaleX = 1.0F;
                            scaleY = 1.0F;

                            deltaX = 0;
                            deltaY = 0;
                        }
                    }
                    break;
                case ResizeMapAnchorPoint.TopCenter:
                    {
                        if (scaleToFit)
                        {
                            scaleX = resizedMap.MapWidth / dlgResult.SelectedArea.Width;
                            scaleY = resizedMap.MapHeight / dlgResult.SelectedArea.Height;

                            deltaX = 0;
                            deltaY = 0;
                        }
                        else
                        {
                            scaleX = 1.0F;
                            scaleY = 1.0F;

                            deltaX = -(currentMap.MapWidth - resizedMap.MapWidth) / 2;
                            deltaY = 0;
                        }
                    }
                    break;
                case ResizeMapAnchorPoint.TopRight:
                    {
                        if (scaleToFit)
                        {
                            scaleX = resizedMap.MapWidth / dlgResult.SelectedArea.Width;
                            scaleY = resizedMap.MapHeight / dlgResult.SelectedArea.Height;

                            deltaX = 0;
                            deltaY = 0;
                        }
                        else
                        {
                            scaleX = 1.0F;
                            scaleY = 1.0F;

                            deltaX = resizedMap.MapWidth - currentMap.MapWidth;
                            deltaY = 0;
                        }
                    }
                    break;
                case ResizeMapAnchorPoint.CenterLeft:
                    {
                        if (scaleToFit)
                        {
                            scaleX = resizedMap.MapWidth / dlgResult.SelectedArea.Width;
                            scaleY = resizedMap.MapHeight / dlgResult.SelectedArea.Height;
                            deltaX = 0;
                            deltaY = 0;
                        }
                        else
                        {
                            scaleX = 1.0F;
                            scaleY = 1.0F;
                            deltaX = 0;
                            deltaY = -(currentMap.MapHeight - resizedMap.MapHeight) / 2;
                        }
                    }
                    break;
                case ResizeMapAnchorPoint.CenterRight:
                    {
                        if (scaleToFit)
                        {
                            scaleX = resizedMap.MapWidth / dlgResult.SelectedArea.Width;
                            scaleY = resizedMap.MapHeight / dlgResult.SelectedArea.Height;
                            deltaX = 0;
                            deltaY = 0;
                        }
                        else
                        {
                            scaleX = 1.0F;
                            scaleY = 1.0F;
                            deltaX = resizedMap.MapWidth - currentMap.MapWidth;
                            deltaY = -(currentMap.MapHeight - resizedMap.MapHeight) / 2;
                        }
                    }
                    break;
                case ResizeMapAnchorPoint.BottomLeft:
                    {
                        if (scaleToFit)
                        {
                            scaleX = resizedMap.MapWidth / dlgResult.SelectedArea.Width;
                            scaleY = resizedMap.MapHeight / dlgResult.SelectedArea.Height;

                            deltaX = 0;
                            deltaY = 0;
                        }
                        else
                        {
                            scaleX = 1.0F;
                            scaleY = 1.0F;

                            deltaX = 0;
                            deltaY = resizedMap.MapHeight - currentMap.MapHeight;
                        }
                    }
                    break;
                case ResizeMapAnchorPoint.BottomCenter:
                    {
                        if (scaleToFit)
                        {
                            scaleX = resizedMap.MapWidth / dlgResult.SelectedArea.Width;
                            scaleY = resizedMap.MapHeight / dlgResult.SelectedArea.Height;
                            deltaX = 0;
                            deltaY = 0;
                        }
                        else
                        {
                            scaleX = 1.0F;
                            scaleY = 1.0F;
                            deltaX = -(currentMap.MapWidth - resizedMap.MapWidth) / 2;
                            deltaY = resizedMap.MapHeight - currentMap.MapHeight;
                        }
                    }
                    break;
                case ResizeMapAnchorPoint.BottomRight:
                    {
                        if (scaleToFit)
                        {
                            scaleX = resizedMap.MapWidth / dlgResult.SelectedArea.Width;
                            scaleY = resizedMap.MapHeight / dlgResult.SelectedArea.Height;

                            deltaX = 0;
                            deltaY = 0;
                        }
                        else
                        {
                            scaleX = 1.0F;
                            scaleY = 1.0F;

                            deltaX = resizedMap.MapWidth - currentMap.MapWidth;
                            deltaY = resizedMap.MapHeight - currentMap.MapHeight;
                        }
                    }
                    break;
            }


            // get the landforms and drawn shapes within or intersecting the selected area, then translate and scale them
            MapLayer landformLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.LANDFORMLAYER);
            MapLayer newRealmLandformLayer = MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.LANDFORMLAYER);

            for (int i = 0; i < landformLayer.Shapes.Count; i++)
            {
                if (landformLayer.Shapes[i] is Landform lf)
                {
                    SKPath lfContour = lf.HitPath;

                    foreach (SKPoint p in lfContour.Points)
                    {
                        if (selectedMapArea.Contains(p))
                        {
                            // landform path is in or intersects the selected area
                            SKPath transformedPath = new(lfContour);
                            transformedPath.Transform(SKMatrix.CreateScaleTranslation(scaleX, scaleY, deltaX, deltaY));

                            Landform newLandform = new();

                            newLandform.CloneSettingsFrom(lf);
                            newLandform.ReplaceGeometry(transformedPath);

                            newRealmLandformLayer.Add(newLandform);

                            break;
                        }
                    }

                }
                else if (landformLayer.Shapes[i] is IDrawnMapComponent idmc && landformLayer.Shapes[i] is MapComponent2D mc2d)
                {
                    if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                    {
                        MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                        if (newDmc != null)
                        {
                            newRealmLandformLayer.Add(newDmc);
                        }
                    }
                }
            }

            // go through the current map to get textures, painted colors, etc. and assign them to the detail map

            MapLayer baseLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.BASELAYER);
            MapLayer newRealmBaseLayer = MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.BASELAYER);

            resizedMap.Background = new MapBackgroundSettings()
            {
                TextureId = currentMap.Background.TextureId,
                Scale = (float)currentMap.Background.Scale,
                Mirror = currentMap.Background.Mirror,
                Enabled = currentMap.Background.Enabled
            };            

            for (int i = 0; i < baseLayer.Shapes.Count; i++)
            {
                if (baseLayer.Shapes[i] is IDrawnMapComponent idmc && baseLayer.Shapes[i] is MapComponent2D mc2d)
                {
                    if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                    {
                        MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                        if (newDmc != null)
                        {
                            newRealmBaseLayer.Add(newDmc);
                        }
                    }
                }
            }

            MapLayer oceanTextureLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.OCEANTEXTURELAYER);
            MapLayer newRealmOceanTextureLayer = MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.OCEANTEXTURELAYER);

            resizedMap.Ocean = new OceanSettings()
            {
                TextureId = currentMap.Background.TextureId,
                Scale = (float)currentMap.Background.Scale,
                Mirror = currentMap.Background.Mirror,
                EnableCoastlineBlur = currentMap.Ocean.EnableCoastlineBlur,
                TextureOpacity = currentMap.Ocean.TextureOpacity,
                ColorOverlayEnabled = currentMap.Ocean.ColorOverlayEnabled,
                OverlayColor = currentMap.Ocean.OverlayColor,
            };


            for (int i = 0; i < oceanTextureLayer.Shapes.Count; i++)
            {
                if (oceanTextureLayer.Shapes[i] is IDrawnMapComponent idmc && oceanTextureLayer.Shapes[i] is MapComponent2D mc2d)
                {
                    if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                    {
                        MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                        if (newDmc != null)
                        {
                            newRealmOceanTextureLayer.Add(newDmc);
                        }
                    }
                }
            }

            MapLayer oceanTextureOverlayLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.OCEANTEXTUREOVERLAYLAYER);
            MapLayer newRealmOceanTextureOverlayLayer = MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.OCEANTEXTUREOVERLAYLAYER);

            for (int i = 0; i < oceanTextureOverlayLayer.Shapes.Count; i++)
            {
                if (oceanTextureOverlayLayer.Shapes[i] is IDrawnMapComponent idmc && oceanTextureOverlayLayer.Shapes[i] is MapComponent2D mc2d)
                {
                    if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                    {
                        MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                        if (newDmc != null)
                        {
                            newRealmOceanTextureOverlayLayer.Add(newDmc);
                        }
                    }
                }
            }

            // ocean drawing layer
            MapLayer oceanDrawingLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.OCEANDRAWINGLAYER);
            MapLayer newRealmOceanDrawingLayer = MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.OCEANDRAWINGLAYER);

            for (int i = 0; i < oceanDrawingLayer.Shapes.Count; i++)
            {
                if (oceanDrawingLayer.Shapes[i] is IDrawnMapComponent idmc && oceanDrawingLayer.Shapes[i] is MapComponent2D mc2d)
                {
                    if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                    {
                        MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                        if (newDmc != null)
                        {
                            newRealmOceanDrawingLayer.Add(newDmc);
                        }
                    }
                }
            }

            // land drawing layer
            MapLayer landDrawingLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.LANDDRAWINGLAYER);
            MapLayer newRealmLandDrawingLayer = MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.LANDDRAWINGLAYER);

            for (int i = 0; i < landDrawingLayer.Shapes.Count; i++)
            {
                if (landDrawingLayer.Shapes[i] is IDrawnMapComponent idmc && landDrawingLayer.Shapes[i] is MapComponent2D mc2d)
                {
                    if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                    {
                        MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                        if (newDmc != null)
                        {
                            newRealmLandDrawingLayer.Add(newDmc);
                        }
                    }
                }
            }

            // water systems and water features; transform/scale them to the new map size and location,
            // then rebuild the water system and add the water systems to the new map


            Dictionary<WaterSystem, List<WaterBody>> waterSystemMap = [];            

            for (int i = 0; i < currentMap.WaterSystems.Count; i++)
            {
                List<WaterBody> wsWaterBodies = currentMap.WaterSystems[i].WaterBodies;

                for (int j = 0; j < wsWaterBodies.Count; j++)
                {
                    if (wsWaterBodies[j] is Lake lake)
                    {
                        if (lake.WaterSystem == null)
                        {
                            // try to find the watersystem for the lake

                            WaterSystem? foundSystem = FindWaterSystemForWaterBody(currentMap, lake);

                            if (foundSystem != null)
                            {
                                lake.WaterSystem = foundSystem;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        SKPath wfPath = lake.HitPath;

                        if (wfPath != null)
                        {
                            if (selectedMapArea.IntersectsWith(lake.Bounds))
                            {
                                // water feature path is in or intersects the selected area

                                SKPath transformedWfPath = new(wfPath);
                                transformedWfPath.Transform(SKMatrix.CreateScaleTranslation(scaleX, scaleY, deltaX, deltaY));

                                Lake l = new()
                                {
                                    Name = lake.Name,
                                    Description = lake.Description,
                                    RenderSettings = WaterRenderSettings.Clone(lake.RenderSettings)
                                };

                                l.RenderSettings.ShorelineWidth = Math.Max(lake.RenderSettings.ShorelineWidth * scaleX, 1);

                                l.ReplaceGeometry(transformedWfPath);
                                
                                if (!waterSystemMap.TryGetValue(lake.WaterSystem, out List<WaterBody>? waterBodies))
                                {
                                    waterBodies = [];
                                    waterSystemMap.Add(lake.WaterSystem, waterBodies);
                                }

                                waterBodies.Add(l);
                            }
                        }
                    }
                    else if (wsWaterBodies[j] is PaintedWaterBody waterBody)
                    {
                        if (waterBody.WaterSystem == null)
                        {
                            // try to find the watersystem for the painted water body

                            WaterSystem? foundSystem = FindWaterSystemForWaterBody(currentMap, waterBody);

                            if (foundSystem != null)
                            {
                                waterBody.WaterSystem = foundSystem;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        SKPath wfPath = waterBody.HitPath;

                        if (wfPath != null)
                        {
                            if (selectedMapArea.IntersectsWith(waterBody.Bounds))
                            {
                                // water feature path is in or intersects the selected area

                                SKPath transformedWfPath = new(wfPath);
                                transformedWfPath.Transform(SKMatrix.CreateScaleTranslation(scaleX, scaleY, deltaX, deltaY));

                                PaintedWaterBody paintedWaterBody = new()
                                {
                                    Name = waterBody.Name,
                                    Description = waterBody.Description,
                                    RenderSettings = WaterRenderSettings.Clone(waterBody.RenderSettings)
                                };

                                paintedWaterBody.RenderSettings.ShorelineWidth = Math.Max(paintedWaterBody.RenderSettings.ShorelineWidth * scaleX, 1);

                                paintedWaterBody.ReplaceGeometry(transformedWfPath);

                                if (!waterSystemMap.TryGetValue(waterBody.WaterSystem, out List<WaterBody>? waterBodies))
                                {
                                    waterBodies = [];
                                    waterSystemMap.Add(waterBody.WaterSystem, waterBodies);
                                }

                                waterBodies.Add(paintedWaterBody);
                            }
                        }
                    }
                    else if (wsWaterBodies[j] is River r)
                    {
                        if (r.WaterSystem == null)
                        {
                            // try to find the watersystem for the river

                            WaterSystem? foundSystem = FindWaterSystemForWaterBody(currentMap, r);

                            if (foundSystem != null)
                            {
                                r.WaterSystem = foundSystem;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        SKPath riverPath = r.HitPath;

                        if (selectedMapArea.IntersectsWith(r.Bounds) && riverPath.PointCount > 2)
                        {
                            List<SKPoint> newControlPoints = [.. r.ControlPoints.Select(cp => new SKPoint(cp.X * scaleX + deltaX, cp.Y * scaleY + deltaY))];

                            River newRealmRiver = new()
                            {
                                Name = r.Name,
                                Description = r.Description,
                                RenderSettings = WaterRenderSettings.Clone(r.RenderSettings),
                            };

                            newRealmRiver.RenderSettings.RiverWidth = Math.Max(newRealmRiver.RenderSettings.RiverWidth * scaleX, 1);
                            newRealmRiver.RenderSettings.ShorelineWidth = Math.Max(newRealmRiver.RenderSettings.ShorelineWidth * scaleX, 1);

                            newRealmRiver.ControlPoints.Clear();
                            newRealmRiver.ControlPoints.AddRange(newControlPoints);

                            newRealmRiver.Editor.Points = newControlPoints;
                            newRealmRiver.Editor.RebuildEditablePoints();

                            newRealmRiver.FinalizeShapeGeometry(resizedMap);

                            if (!waterSystemMap.TryGetValue(r.WaterSystem, out List<WaterBody>? waterBodies))
                            {
                                waterBodies = [];
                                waterSystemMap.Add(r.WaterSystem, waterBodies);
                            }

                            waterBodies.Add(newRealmRiver);
                        }
                    }
                }
            }

            // rebuild the water systems in the new map based on the new water features
            List<WaterSystem> oldWaterSystems = [.. currentMap.WaterSystems];

            foreach (WaterSystem oldWs in oldWaterSystems)
            {
                if (!waterSystemMap.TryGetValue(
                        oldWs,
                        out List<WaterBody>? waterBodiesForSystem))
                {
                    continue;
                }

                WaterSystem newWs = new()
                {
                    Name = oldWs.Name,
                    Description = oldWs.Description,
                    RenderSettings = WaterRenderSettings.Clone(oldWs.RenderSettings),
                };

                foreach (WaterBody wb in waterBodiesForSystem)
                {                    
                    newWs.WaterBodies.Add(wb);
                }

                newWs.FinalizeWaterSystem(resizedMap);

                newWs.GeometryModified();

                resizedMap.WaterSystems.Add(newWs);
            }



            // water layer drawn map components
            MapLayer waterLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.WATERLAYER);
            MapLayer newRealmWaterLayer = MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.WATERLAYER);

            for (int i = 0; i < waterLayer.Shapes.Count; i++)
            {
                if (waterLayer.Shapes[i] is IDrawnMapComponent idmc && waterLayer.Shapes[i] is MapComponent2D mc2d)
                {
                    if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                    {
                        MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                        if (newDmc != null)
                        {
                            newRealmWaterLayer.Add(newDmc);
                        }
                    }
                }
            }

            // water drawing layer
            MapLayer waterDrawingLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.WATERDRAWINGLAYER);
            MapLayer newRealmWaterDrawingLayer = MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.WATERDRAWINGLAYER);

            for (int i = 0; i < waterDrawingLayer.Shapes.Count; i++)
            {
                if (waterDrawingLayer.Shapes[i] is IDrawnMapComponent idmc && waterDrawingLayer.Shapes[i] is MapComponent2D mc2d)
                {
                    if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                    {
                        MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                        if (newDmc != null)
                        {
                            newRealmWaterDrawingLayer.Add(newDmc);
                        }
                    }
                }
            }

            // gather the symbols in the selected area
            MapLayer symbolLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.SYMBOLLAYER);
            MapLayer newRealmSymbolLayer = MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.SYMBOLLAYER);
            List<MapSymbol> gatheredSymbols = [];

            for (int i = 0; i < symbolLayer.Shapes.Count; i++)
            {
                if (symbolLayer.Shapes[i] is MapSymbol ms)
                {
                    if (selectedMapArea.Contains(ms.Location))
                    {
                        if (includeTerrainSymbols && ms.SymbolDefinition.SymbolType == MapSymbolType.Terrain)
                        {
                            gatheredSymbols.Add(ms);
                        }

                        if (includeVegetationSymbols && ms.SymbolDefinition.SymbolType == MapSymbolType.Vegetation)
                        {
                            gatheredSymbols.Add(ms);
                        }

                        if (includeStructureSymbols && ms.SymbolDefinition.SymbolType == MapSymbolType.Structure)
                        {
                            gatheredSymbols.Add(ms);
                        }

                        if (includeMarkerSymbols && ms.SymbolDefinition.SymbolType == MapSymbolType.Marker)
                        {
                            gatheredSymbols.Add(ms);
                        }
                    }
                }
                else if (symbolLayer.Shapes[i] is IDrawnMapComponent idmc && symbolLayer.Shapes[i] is MapComponent2D mc2d)
                {
                    if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                    {
                        MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                        if (newDmc != null)
                        {
                            newRealmSymbolLayer.Add(newDmc);
                        }
                    }
                }
            }

            // scale the symbols and add them to the detail map
            foreach (MapSymbol ms in gatheredSymbols)
            {
                IShapeState symbolState = ms.CaptureState();

                MapSymbol newSymbol = new()
                {
                    SymbolDefinition = ms.SymbolDefinition,
                };

                newSymbol.RestoreState(symbolState);

                newSymbol.Location = new SKPoint(ms.Location.X * scaleX + deltaX, ms.Location.Y * scaleY + deltaY);
                newSymbol.Scale = newSymbol.Scale * Math.Min(scaleX, scaleY);

                newSymbol.UpdateBounds();

                newRealmSymbolLayer.Add(newSymbol);
            }

            // get map paths
            if (includePaths)
            {
                List<MapPath> gatheredPaths = [];

                MapLayer pathLowerLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.PATHLOWERLAYER);
                MapLayer newRealmPathLowerLayer = MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.PATHLOWERLAYER);

                for (int i = 0; i < pathLowerLayer.Shapes.Count; i++)
                {
                    if (pathLowerLayer.Shapes[i] is MapPath mp)
                    {
                        foreach (SKPoint point in mp.ControlPoints)
                        {
                            if (selectedMapArea.Contains(point))
                            {
                                gatheredPaths.Add(mp);
                                break;
                            }
                        }
                    }
                    else if (pathLowerLayer.Shapes[i] is IDrawnMapComponent idmc && waterLayer.Shapes[i] is MapComponent2D mc2d)
                    {
                        if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                        {
                            MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                            if (newDmc != null)
                            {
                                newRealmPathLowerLayer.Add(newDmc);
                            }
                        }
                    }
                }

                MapLayer pathUpperLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.PATHUPPERLAYER);
                MapLayer newRealmPathUpperLayer = MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.PATHUPPERLAYER);

                for (int i = 0; i < pathUpperLayer.Shapes.Count; i++)
                {
                    if (pathUpperLayer.Shapes[i] is MapPath mp)
                    {
                        foreach (SKPoint point in mp.ControlPoints)
                        {
                            if (selectedMapArea.Contains(point))
                            {
                                gatheredPaths.Add(mp);
                                break;
                            }
                        }
                    }
                    else if (pathUpperLayer.Shapes[i] is IDrawnMapComponent idmc && waterLayer.Shapes[i] is MapComponent2D mc2d)
                    {
                        if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                        {
                            MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                            if (newDmc != null)
                            {
                                newRealmPathUpperLayer.Add(newDmc);
                            }
                        }
                    }
                }

                foreach (MapPath mp in gatheredPaths)
                {
                    if (mp.ControlPoints.Count > 0)
                    {
                        IShapeState pathState = mp.CaptureState();

                        MapPath newPath = new();

                        newPath.RestoreState(pathState);

                        List<SKPoint> newPoints = [];

                        foreach (SKPoint point in mp.ControlPoints)
                        {
                            SKPoint newPoint = new((point.X * scaleX) + deltaX, (point.Y * scaleY) + deltaY);
                            newPoints.Add(newPoint);
                        }

                        newPath.ControlPoints.Clear();
                        newPath.ControlPoints.AddRange(newPoints);

                        newPath.ResolveAssets(_mainWindowViewModel.AssetManager);

                        newPath.FinalizeShapeGeometry(resizedMap);

                        if (mp.DrawOverSymbols)
                        {
                            newRealmPathUpperLayer.Add(newPath);
                        }
                        else
                        {
                            newRealmPathLowerLayer.Add(newPath);
                        }
                    }
                }
            }

            if (includeLabels)
            {
                // get labels
                MapLayer labelLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.LABELLAYER);
                MapLayer newRealmLabelLayer = MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.LABELLAYER);
                List<MapLabel> gatheredLabels = [];

                for (int i = 0; i < labelLayer.Shapes.Count; i++)
                {
                    if (labelLayer.Shapes[i] is MapLabel ml)
                    {
                        SKRect mlBoundingRect = ml.Bounds;
                        if (selectedMapArea.IntersectsWith(mlBoundingRect))
                        {
                            gatheredLabels.Add(ml);
                        }
                    }
                    else if (labelLayer.Shapes[i] is IDrawnMapComponent idmc && labelLayer.Shapes[i] is MapComponent2D mc2d)
                    {
                        if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                        {
                            MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                            if (newDmc != null)
                            {
                                newRealmLabelLayer.Add(newDmc);
                            }
                        }
                    }
                }

                foreach (MapLabel ml in gatheredLabels)
                {
                    IShapeState labelState = ml.CaptureState();

                    MapLabel newLabel = new();

                    newLabel.RestoreState(labelState);

                    newLabel.FontStyle.Size *= Math.Min(scaleX, scaleY);

                    newLabel.OutlineWidth *= Math.Max(Math.Min(scaleX, scaleY), 1);
                    newLabel.GlowStrength *= Math.Max(Math.Min(scaleX, scaleY), 1);

                    newLabel.Location = new SKPoint(ml.Location.X * scaleX + deltaX, ml.Location.Y * scaleY + deltaY);
                    newLabel.BaselineLocation = new SKPoint(ml.BaselineLocation.X * scaleX + deltaX, ml.BaselineLocation.Y * scaleY + deltaY);

                    if (ml.CurvePath != null)
                    {
                        SKPath transformedLabelPath = new(ml.CurvePath);
                        transformedLabelPath.Transform(SKMatrix.CreateScaleTranslation(scaleX, scaleY, deltaX, deltaY));

                        newLabel.CurvePath = transformedLabelPath;
                    }

                    newRealmLabelLayer.Shapes.Add(newLabel);
                }
            }

            if (includeBoxes)
            {
                // get boxes
                MapLayer boxLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.BOXLAYER);
                MapLayer newRealmBoxLayer = MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.BOXLAYER);
                List<PlacedMapBox> gatheredBoxes = [];

                for (int i = 0; i < boxLayer.Shapes.Count; i++)
                {
                    if (boxLayer.Shapes[i] is PlacedMapBox box)
                    {
                        if (selectedMapArea.IntersectsWith(box.Bounds))
                        {
                            gatheredBoxes.Add(box);
                        }
                    }
                    else if (boxLayer.Shapes[i] is IDrawnMapComponent idmc && boxLayer.Shapes[i] is MapComponent2D mc2d)
                    {
                        if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                        {
                            MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                            if (newDmc != null)
                            {
                                newRealmBoxLayer.Add(newDmc);
                            }
                        }
                    }
                }

                foreach (PlacedMapBox box in gatheredBoxes)
                {
                    if (box.BoxBitmap != null)
                    {
                        SKBitmap resizedBitmap = Utilities.ResizeSKBitmap(box.BoxBitmap, new SKSizeI((int)(box.Bounds.Width * scaleX), (int)(box.Bounds.Height * scaleY)));

                        IShapeState boxState = box.CaptureState();

                        PlacedMapBox newBox = new(box)
                        {
                            BoxBitmap = resizedBitmap.Copy(),
                            BoxCenterLeft = box.BoxCenterLeft * scaleX,
                            BoxCenterTop = box.BoxCenterTop * scaleY,
                            BoxCenterRight = box.BoxCenterRight * scaleX,
                            BoxCenterBottom = box.BoxCenterBottom * scaleY,
                            Location = new SKPoint(box.Location.X * scaleX + deltaX, box.Location.Y * scaleY + deltaY)
                        };

                        SKRect newBounds = new(box.Bounds.Left * scaleX + deltaX,
                            box.Bounds.Top * scaleX + deltaX,
                            box.Bounds.Width * scaleX,
                            box.Bounds.Bottom * scaleY);

                        newBox.Scale = newBox.Scale * Math.Min(scaleX, scaleY);

                        newBox.Bounds = newBounds;

                        newBox.SetBoxBitmap(resizedBitmap);

                        newRealmBoxLayer.Shapes.Add(newBox);
                    }
                }
            }

            if (includeScale)
            {
                // get scale
                MapLayer scaleLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.OVERLAYLAYER);
                MapLayer newRealmScaleLayer = MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.OVERLAYLAYER);

                for (int i = 0; i < scaleLayer.Shapes.Count; i++)
                {
                    if (scaleLayer.Shapes[i] is MapScale ms)
                    {
                        MapScale newScale = new()
                        {
                            ScaleColor1 = ms.ScaleColor1,
                            ScaleColor2 = ms.ScaleColor2,
                            ScaleColor3 = ms.ScaleColor3,
                            ScaleSegmentCount = ms.ScaleSegmentCount,
                            ScaleLineWidth = (int)Math.Max(ms.ScaleLineWidth * Math.Min(scaleX, scaleY), 1),
                            ScaleDistance = ms.ScaleDistance,
                            ScaleDistanceUnit = ms.ScaleDistanceUnit,
                            ScaleNumbersDisplayType = ms.ScaleNumbersDisplayType,
                            ScaleFont = ms.ScaleFont,
                            ScaleFontColor = ms.ScaleFontColor,
                            ScaleOutlineWidth = (int)Math.Max(ms.ScaleOutlineWidth * Math.Min(scaleX, scaleY), 0),
                            ScaleOutlineColor = ms.ScaleOutlineColor,
                        };


                        // initial position of the scale is near the bottom-left corner of the map
                        SKPoint newLocation = new(100, resizedMap.MapHeight - 100);
                        newScale.Location = newLocation;
                        newScale.ScaleWidth = (int)(ms.ScaleWidth);
                        newScale.ScaleHeight = (int)(ms.ScaleHeight);

                        newScale.Bounds = new SKRect(newLocation.X, newLocation.Y, newScale.Location.X + newScale.ScaleWidth, newScale.Location.Y + newScale.ScaleHeight);

                        newRealmScaleLayer.Shapes.Add(newScale);
                    }
                    else if (scaleLayer.Shapes[i] is IDrawnMapComponent idmc && scaleLayer.Shapes[i] is MapComponent2D mc2d)
                    {
                        if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                        {
                            MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                            if (newDmc != null)
                            {
                                newRealmScaleLayer.Add(newDmc);
                            }
                        }
                    }
                }
            }

            if (includeGrid)
            {
                // get grid
                MapLayer defaultGridLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.DEFAULTGRIDLAYER);
                MapLayer aboveOceanGridLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.ABOVEOCEANGRIDLAYER);
                MapLayer belowSymbolsGridLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.BELOWSYMBOLSGRIDLAYER);

                for (int i = 0; i < defaultGridLayer.Shapes.Count; i++)
                {
                    if (defaultGridLayer.Shapes[i] is MapGrid mapGrid)
                    {
                        MapGrid newGrid = new()
                        {
                            GridEnabled = mapGrid.GridEnabled,
                            GridType = mapGrid.GridType,
                            GridColor = mapGrid.GridColor,
                            GridLayerIndex = mapGrid.GridLayerIndex,
                            GridSize = mapGrid.GridSize,
                            GridLineWidth = mapGrid.GridLineWidth,
                            ShowGridSize = mapGrid.ShowGridSize,
                            MapAreaWidth = resizedMap.MapAreaWidth,
                            MapAreaHeight = resizedMap.MapAreaHeight,
                            MapAreaUnits = mapGrid.MapAreaUnits,
                        };

                        MapBuilder.GetMapLayerByIndex(resizedMap, newGrid.GridLayerIndex).Add(newGrid);
                    }
                    else if (defaultGridLayer.Shapes[i] is IDrawnMapComponent idmc && defaultGridLayer.Shapes[i] is MapComponent2D mc2d)
                    {
                        if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                        {
                            MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                            if (newDmc != null)
                            {
                                MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.DEFAULTGRIDLAYER).Add(newDmc);
                            }
                        }
                    }
                }

                for (int i = 0; i < aboveOceanGridLayer.Shapes.Count; i++)
                {
                    if (aboveOceanGridLayer.Shapes[i] is MapGrid mapGrid)
                    {
                        MapGrid newGrid = new()
                        {
                            GridEnabled = mapGrid.GridEnabled,
                            GridType = mapGrid.GridType,
                            GridColor = mapGrid.GridColor,
                            GridLayerIndex = mapGrid.GridLayerIndex,
                            GridSize = mapGrid.GridSize,
                            GridLineWidth = mapGrid.GridLineWidth,
                            ShowGridSize = mapGrid.ShowGridSize,
                            MapAreaWidth = resizedMap.MapAreaWidth,
                            MapAreaHeight = resizedMap.MapAreaHeight,
                            MapAreaUnits = mapGrid.MapAreaUnits,
                        };

                        MapBuilder.GetMapLayerByIndex(resizedMap, newGrid.GridLayerIndex).Add(newGrid);
                    }
                    else if (aboveOceanGridLayer.Shapes[i] is IDrawnMapComponent idmc && aboveOceanGridLayer.Shapes[i] is MapComponent2D mc2d)
                    {
                        if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                        {
                            MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                            if (newDmc != null)
                            {
                                MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.ABOVEOCEANGRIDLAYER).Add(newDmc);
                            }
                        }
                    }
                }

                for (int i = 0; i < belowSymbolsGridLayer.Shapes.Count; i++)
                {
                    if (belowSymbolsGridLayer.Shapes[i] is MapGrid mapGrid)
                    {
                        MapGrid newGrid = new()
                        {
                            GridEnabled = mapGrid.GridEnabled,
                            GridType = mapGrid.GridType,
                            GridColor = mapGrid.GridColor,
                            GridLayerIndex = mapGrid.GridLayerIndex,
                            GridSize = mapGrid.GridSize,
                            GridLineWidth = mapGrid.GridLineWidth,
                            ShowGridSize = mapGrid.ShowGridSize,
                            MapAreaWidth = resizedMap.MapAreaWidth,
                            MapAreaHeight = resizedMap.MapAreaHeight,
                            MapAreaUnits = mapGrid.MapAreaUnits,
                        };

                        MapBuilder.GetMapLayerByIndex(resizedMap, newGrid.GridLayerIndex).Add(newGrid);
                    }
                    else if (belowSymbolsGridLayer.Shapes[i] is IDrawnMapComponent idmc && belowSymbolsGridLayer.Shapes[i] is MapComponent2D mc2d)
                    {
                        if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                        {
                            MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                            if (newDmc != null)
                            {
                                MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.BELOWSYMBOLSGRIDLAYER).Add(newDmc);
                            }
                        }
                    }
                }
            }

            if (includeRegions)
            {
                MapLayer regionLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.REGIONLAYER);
                MapLayer newRealmRegionLayer = MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.REGIONLAYER);

                for (int i = 0; i < regionLayer.Shapes.Count; i++)
                {
                    if (regionLayer.Shapes[i] is MapRegion mr)
                    {
                        foreach (MapRegionPoint mrp in mr.MapRegionPoints)
                        {
                            if (selectedMapArea.Contains(mrp.RegionPoint))
                            {
                                IShapeState rs = mr.CaptureState();

                                MapRegion newRegion = new();

                                newRegion.RestoreState(rs);

                                List<MapRegionPoint> regionPoints = new List<MapRegionPoint>();

                                foreach (MapRegionPoint point in newRegion.MapRegionPoints)
                                {
                                    MapRegionPoint newRegionPoint = new()
                                    {
                                        RegionPoint = new SKPoint((point.RegionPoint.X * scaleX) + deltaX, (point.RegionPoint.Y * scaleY) + deltaY),
                                    };

                                    regionPoints.Add(newRegionPoint);
                                }

                                newRegion.MapRegionPoints.Clear();
                                newRegion.MapRegionPoints.AddRange(regionPoints);

                                using SKPath path = Utilities.BuildClosedPath([.. newRegion.MapRegionPoints.Select(p => p.RegionPoint)]);

                                newRegion.BoundaryPath.Dispose();
                                newRegion.BoundaryPath = new(path);
                                newRegion.Bounds = newRegion.BoundaryPath.Bounds;

                                newRegion.ConstructRegionPaint();

                                newRealmRegionLayer.Add(newRegion);
                                break;
                            }
                        }
                    }
                    else if (regionLayer.Shapes[i] is IDrawnMapComponent idmc && regionLayer.Shapes[i] is MapComponent2D mc2d)
                    {
                        if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                        {
                            MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                            if (newDmc != null)
                            {
                                newRealmRegionLayer.Add(newDmc);
                            }
                        }
                    }
                }
            }

            if (includeHeightMap)
            {
                // TODO: height map not yet implemented
                MapLayer heightMapLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.HEIGHTMAPLAYER);

                if (heightMapLayer.Shapes.Count == 2)
                {
                    MapLayer newHeightMapLayer = MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.HEIGHTMAPLAYER);

                    using SKBitmap b = new(new SKImageInfo(resizedMap.MapWidth, resizedMap.MapHeight));
                    using SKCanvas canvas = new(b);

                    canvas.Clear(SKColors.Black);

                    for (int i = 0; i < landformLayer.Shapes.Count; i++)
                    {
                        if (landformLayer.Shapes[i] is Landform l)
                        {
                            //l.RenderLandformForHeightMap(canvas);
                        }
                    }

                    //MapImage landformImage = new()
                    //{
                    //    MapImageBitmap = b.Copy()
                    //};

                    //newHeightMapLayer.Shapes.Add(landformImage);

                    if (heightMapLayer.Shapes[1] is MapHeightMap mhm)
                    {
                        //Bitmap resizedBitmap = new(mhm.HeightMapImage.ToBitmap(), resizedMap.MapWidth, resizedMap.MapHeight);

                        //MapHeightMap heightMap = new()
                        //{
                        //    Width = resizedMap.MapWidth,
                        //    Height = resizedMap.MapHeight,
                        //    MapImageBitmap = resizedBitmap.ToSKBitmap(),
                        //};

                        //newHeightMapLayer.Shapes.Add(heightMap);
                    }
                }
            }

            if (includeDrawnShapes)
            {
                // get drawn shapes
                MapLayer drawingLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.DRAWINGLAYER);
                MapLayer newRealmDrawingLayer = MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.DRAWINGLAYER);
                for (int i = 0; i < drawingLayer.Shapes.Count; i++)
                {
                    if (drawingLayer.Shapes[i] is IDrawnMapComponent idmc && drawingLayer.Shapes[i] is MapComponent2D mc2d)
                    {
                        if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                        {
                            MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                            if (newDmc != null)
                            {
                                newRealmDrawingLayer.Add(newDmc);
                            }
                        }
                    }
                }

                newRealmDrawingLayer.InvalidateAllTiles();
            }


            // vignette
            MapLayer vignetteLayer = MapBuilder.GetMapLayerByIndex(currentMap, MapBuilder.VIGNETTELAYER);
            MapLayer newRealmVignetteLayer = MapBuilder.GetMapLayerByIndex(resizedMap, MapBuilder.VIGNETTELAYER);

            for (int i = 0; i < vignetteLayer.Shapes.Count; i++)
            {
                if (vignetteLayer.Shapes[i] is MapVignette mv)
                {
                    MapVignette vignette = new()
                    {
                        VignetteShape = mv.VignetteShape,
                        VignetteStrength = mv.VignetteStrength,
                        VignetteColor = mv.VignetteColor,
                    };

                    newRealmVignetteLayer.Add(vignette);

                }
                else if (vignetteLayer.Shapes[i] is IDrawnMapComponent idmc && vignetteLayer.Shapes[i] is MapComponent2D mc2d)
                {
                    if (selectedMapArea.IntersectsWith(mc2d.Bounds))
                    {
                        MapComponent2D? newDmc = UserInterfaceUtilities.CreateScaledTransformedDrawnComponent(mc2d, scaleX, scaleY, deltaX, deltaY, resizedMap.MapWidth, resizedMap.MapHeight);

                        if (newDmc != null)
                        {
                            newRealmVignetteLayer.Add(newDmc);
                        }
                    }
                }
            }

            return resizedMap;

        }  // end method

        public static WaterSystem? FindWaterSystemForWaterBody(RealmStudioMap map, WaterBody waterBody)
        {
            WaterSystem? foundSystem = null;

            foreach (WaterSystem ws in map.WaterSystems)
            {
                if (ws.WaterBodies.Contains(waterBody))
                {
                    foundSystem = ws;
                    break;
                }
            }

            return foundSystem;
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
