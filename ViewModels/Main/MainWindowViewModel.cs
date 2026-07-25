using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.Editor.Services;
using RealmStudioX.WPF.Editor.Tools;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.Models.Startup;
using RealmStudioX.WPF.Models.UserInterface;
using RealmStudioX.WPF.ViewModels.Controls;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;

namespace RealmStudioX.WPF.ViewModels.Main
{
    public class MainWindowViewModel : ViewModelBase
    {
        public static WindowManager WindowManager => ((App)Application.Current).WindowManager;

        private readonly EditorController _editor;

        public EditorController Editor
        {
            get { return _editor; }
        }

        private readonly ProjectManager _projectManager;

        public ProjectManager ProjectManager
        {
            get => _projectManager;
        }

        private readonly AssetManager _assetManager;

        public AssetManager AssetManager => _assetManager;

        private readonly FontManager _fontManager;

        public readonly ThemeManager _themeManager;
        public ThemeManager ThemeManager => _themeManager;

        private SKRect _viewPortSize = SKRect.Empty;

        public double ViewportPixelWidth => _viewPortSize.Width;
        public double ViewportPixelHeight => _viewPortSize.Height;

        public ProjectPanelViewModel ProjectViewModel { get; }

        public BackgroundPanelViewModel BackgroundViewModel { get; }

        public OceanPanelViewModel OceanViewModel { get; }

        public LandformPanelViewModel LandformViewModel { get; }

        public WaterPanelViewModel WaterViewModel { get; }

        public PathPanelViewModel PathViewModel { get; }

        public SymbolsPanelViewModel SymbolsViewModel {  get; }

        public LabelsPanelViewModel LabelsViewModel { get; }

        public OverlaysPanelViewModel OverlaysViewModel { get; }

        public FontSelectionViewModel FontPanelViewModel { get; }

        public MapScaleViewModel ScaleViewModel { get; }

        public RegionPanelViewModel RegionViewModel { get; }

        public DrawingPanelViewModel DrawingViewModel { get; }

        public NameGenConfigViewModel NameGenConfigViewModel { get; }

        public CommandService CommandService { get; }

        public RecoveryService RecoveryService { get; }

        public SelectionService SelectionService { get; }

        public PaintService PaintService { get; }

        public LayoutService LayoutService { get; }

        public LayoutPathTool LayoutTool { get; }

        public LayoutOptions Layout { get; }

        public ExportService ExportService { get; }

        public event Action? RequestOpenNameGeneratorConfig;

        private readonly string autosaveRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RealmStudioX", "Autosave");

        public MainWindowViewModel(EditorController editor, AssetManager assetManager, FontManager fontManager, ThemeManager themeManager)
        {
            _editor = editor;
            _assetManager = assetManager;
            _fontManager = fontManager;
            _projectManager = new();

            CommandService = new(_projectManager.ProjectCommands, _editor.Commands);

            RecoveryService = new RecoveryService(autosaveRoot);

            ((App)Application.Current).RecoveryService = RecoveryService;

            Editor.SetCommandService(CommandService);

            CommandService.HasSavedChangesUpdate += RecoveryService.HasSavedChangesUpdate;

            SelectionService = new();

            Editor.SetSelectionService(SelectionService);
            
            PaintService = new(_assetManager, _editor, CommandService);

            Editor.SetPaintService(PaintService);

            LayoutService = new(this, Editor, SelectionService, CommandService);

            LayoutTool = new(Editor);
            Editor.SetLayoutTool(LayoutTool);
            Editor.SetLayoutService(LayoutService);

            ExportService = new(Editor);

            _themeManager = themeManager;

            // instantiate ViewModels for the panels; when adding a view model
            // remember to add a reference to it on the TabItem <panel:...> in MainTabs.xaml
            // and in MainWindow.xaml.cs ShowToolPanel() method

            // Project Panel
            ProjectViewModel = new ProjectPanelViewModel(this, _editor, _projectManager);

            // Background Panel
            BackgroundViewModel = new BackgroundPanelViewModel(_editor, assetManager);

            // Ocean Panel
            OceanViewModel = new OceanPanelViewModel(this, _editor, assetManager);

            // Landform Panel
            LandformViewModel = new LandformPanelViewModel(this,_editor, assetManager);

            // Water Body Panel
            WaterViewModel = new WaterPanelViewModel(this, _editor, assetManager);

            // Path Panel
            PathViewModel = new PathPanelViewModel(_editor, assetManager);

            // Symbols Panel
            SymbolsViewModel = new SymbolsPanelViewModel(_editor, assetManager);

            // Labels Panel
            LabelsViewModel = new LabelsPanelViewModel(this, _editor, assetManager);

            // Overlays Panel
            OverlaysViewModel = new OverlaysPanelViewModel(_editor, assetManager);

            // Font Panel (Font selection control)
            FontPanelViewModel = new FontSelectionViewModel(this, _fontManager);

            // Map Scale Control
            ScaleViewModel = new MapScaleViewModel(_editor);

            // Regions Panel
            RegionViewModel = new RegionPanelViewModel(_editor, assetManager);

            // Drawing Panel
            DrawingViewModel = new DrawingPanelViewModel(this, _editor, assetManager);

            NameGenConfigViewModel = new NameGenConfigViewModel(this);

            Layout = new LayoutOptions();

            _editor.SetLabelsViewModel(LabelsViewModel);

            MapName = "Default";
        }

        // -------------------------
        // UI State
        // -------------------------

        private RenderContext? _renderContext;
        public RenderContext? RenderContext
        {
            get => _renderContext;
            set => _renderContext = value;
        }

        private int _selectedTabIndex = -1;

        public int SelectedTabIndex
        {
            get
            {
                return _selectedTabIndex;
            }

            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged();
                CommandService.ProjectPanelSelected = _selectedTabIndex == 0;
            }
        }


        public bool AutoSaveEnabled
        {
            get => RecoveryService.AutoSaveEnabled;
            set => RecoveryService.AutoSaveEnabled = value;
        }

        private string _mapName = string.Empty;
        public string MapName
        {
            get => _mapName;
            set => SetProperty(ref _mapName, value);
        }

        private string _mapSizeLabel = string.Empty;
        public string MapSizeLabel
        {
            get => _mapSizeLabel;
            set => SetProperty(ref _mapSizeLabel, value);
        }

        private string _zoomLevelLabel = string.Empty;
        public string ZoomLevelLabel
        {
            get => _zoomLevelLabel;
            set => SetProperty(ref _zoomLevelLabel, value);
        }

        private string _drawingModeLabel = string.Empty;
        public string DrawingModeLabel
        {
            get => _drawingModeLabel;
            set => SetProperty(ref _drawingModeLabel, value);
        }

        private string _drawingLayerLabel = string.Empty;
        public string DrawingLayerLabel
        {
            get => _drawingLayerLabel;
            set => SetProperty(ref _drawingLayerLabel, value);
        }

        private string _drawingPointLabel = string.Empty;
        public string DrawingPointLabel
        {
            get => _drawingPointLabel;
            set => SetProperty(ref _drawingPointLabel, value);
        }

        private string _cursorPointLabel = string.Empty;
        public string CursorPointLabel
        {
            get => _cursorPointLabel;
            set => SetProperty(ref _cursorPointLabel, value);
        }

        public double MaxScrollX =>
            _editor.Scene?.Map == null ? 0: _editor.Scene.Map.MapWidth * Zoom;

        public double MaxScrollY =>
            _editor.Scene?.Map == null ? 0: _editor.Scene.Map.MapHeight * Zoom;

        public double Zoom
        {
            get => _editor.Scene?.Camera.Zoom ?? 1.0;
            set
            {
                if (_editor.Scene == null || _editor.Scene.Camera == null)
                    return;

                var camera = _editor.Scene.Camera;

                // clamp to 10% to 800%
                value = Math.Clamp(value, 0.1, 8.0);

                if (Math.Abs(camera.Zoom - value) < 0.0001)
                    return;

                camera.SetZoom((float)value, _editor.Scene.Map.MapWidth, _editor.Scene.Map.MapHeight);

                // Notify UI that Zoom changed
                OnPropertyChanged(nameof(Zoom));
                OnPropertyChanged(nameof(MaxScrollX));
                OnPropertyChanged(nameof(MaxScrollY));

                UpdateZoomLabel(camera.Zoom);
            }
        }

        public void UpdateZoomLabel(double zoom)
        {
            ZoomLevelLabel = $"Zoom: {(int)(zoom * 100)}%";
        }

        // -------------------------
        // Commands (menu and buttons)
        // -------------------------

        public ICommand NewOpenCommand => new RelayCommand(() =>
        {
            ShowCreateOpenDialog();
        });

        public void ShowCreateOpenDialog()
        {
            var dialog = new CreateOpenMapDialog();
            var result = dialog.ShowDialog();

            if (result != true || dialog.ViewModel.Result == null)
            {
                return;
            }

            if ((bool)result)
            {
                CreateOpenPackageResult dlgResult = dialog.ViewModel.Result;

                if (dlgResult.CreationOperation == RealmCreationOperation.CreateProject)
                {
                    if (dlgResult.IsNew)
                    {
                        CreateRealmProject(dlgResult);
                    }
                    else
                    {
                        OpenRealmProject(dlgResult);
                    }
                }
            }
        }

        public ICommand SaveCommand => new RelayCommand(() =>
        {
            SaveRealmProject();
        });

        public void SaveRealmProject()
        {
            try
            {
                // save the map project as a zip package
                RealmStudioProject? currentProject = ProjectManager.CurrentProject;

                if (currentProject == null)
                {
                    return;
                }

                RealmStudioMap map = _editor.Scene!.Map;

                currentProject.ActiveMapId = map.MapId;

                string mapFileName = map.MapId + RealmStudioFileFormat.RawMapExtension;

                if (string.IsNullOrEmpty(map.MapPath))
                {
                    string mapPath = Path.Join(AssetManager.RootRealmsDirectory, mapFileName);
                    map.MapPath = mapPath;
                }

                string mapPreviewFileName = map.MapId + ".png";

                MapProjectMetadata projectMeta = currentProject.Metadata!;

                if (string.IsNullOrWhiteSpace(projectMeta.ProjectId))
                {
                    projectMeta.ProjectId = Guid.NewGuid().ToString();

                    RealmStudioXLogger.Info(
                        $"Assigned ProjectId {projectMeta.ProjectId} " +
                        $"to legacy project '{projectMeta.ProjectName}'.");
                }

                MapProjectEntry? mapEntry = null;
                int entryIndex = -1;

                // find the project entry for the map
                for (int i = 0; i < currentProject.Maps.Count; i++)
                {
                    MapProjectEntry mpe = currentProject.Maps[i];

                    if (mpe.MapId == map.MapId)
                    {
                        mapEntry = mpe;
                        entryIndex = i;
                        break;
                    }
                }

                // create a bitmap with the same aspect ratio as the map
                SKBitmap preview = CreateMapPreview(map);

                if (mapEntry == null)
                {
                    mapEntry ??= MapProjectHandler.CreateProjectEntry(map, preview.Copy());
                }
                else
                {
                    mapEntry.Preview = preview.Copy();
                }

                MapMetadata mapMetadata = mapEntry.Metadata!;

                mapMetadata.PreviewFile = mapPreviewFileName;
                mapMetadata.Modified = DateTime.Now;
                projectMeta.Modified = DateTime.Now;

                mapEntry.Metadata = mapMetadata;

                if (entryIndex > -1)
                {
                    currentProject.Maps[entryIndex] = mapEntry;
                }
                else
                {
                    currentProject.Maps.Add(mapEntry);
                }

                string projectFileName = currentProject.Metadata!.ProjectName;
                string mapProjectPath = Path.Join(AssetManager.RootRealmsDirectory, projectFileName + RealmStudioFileFormat.PackageExtension);

                MapFileMethods.SaveProject(mapProjectPath, currentProject);

                ProjectViewModel.LoadProject(currentProject);

                CommandService.MarkSaved();

                _editor.State.StatusMessage = $"Project {currentProject.Metadata.ProjectName} saved.";

            }
            catch (Exception ex)
            {
                MessageDialogFactory.ErrorDialog("Error Saving Realm Project", "An error occurred while saving the project. Check the log file for details.");
                RealmStudioXLogger.Exception("SaveRealmProject", ex);
            }
        }

        private SKBitmap CreateMapPreview(RealmStudioMap map)
        {
            using SKBitmap previewFull = new(map.MapWidth, map.MapHeight);
            using SKCanvas canvas = new(previewFull);

            _editor.Scene!.RenderForExport(canvas);

            SKBitmap preview = Utilities.ResizeBitmap(previewFull, 200, 200 * map.MapHeight / map.MapWidth);

            return preview;
        }

        public ICommand ExportCommand => new RelayCommand(() =>
        {
            ExportDialog exportDlg = new();

            RealmExportViewModel realmExportVM = new(ExportService);

            exportDlg.DataContext = realmExportVM;

            var result = exportDlg.ShowDialog();

            if (result != null)
            {
                exportDlg.Close();
            }
        });

        public ICommand ResetZoomCommand => new RelayCommand(() =>
        {
            _editor.Scene?.Camera?.Reset(_editor.Scene.Map.MapWidth, _editor.Scene.Map.MapHeight);
        });


        public ICommand OpenNameGeneratorConfigCommand => new RelayCommand(() =>
        {
            RequestOpenNameGeneratorConfig?.Invoke();
        });

        public ICommand ExitCommand => new RelayCommand(() =>
        {
            if (!TryShutdown())
            {
                return;
            }

            Application.Current.Shutdown();
        });

        public ICommand UndoCommand => new RelayCommand(() =>
        {
            CommandService.ActiveCommands.Undo();
        });

        public ICommand RedoCommand => new RelayCommand(() =>
        {
            CommandService.ActiveCommands.Redo();
        });

        public ICommand AreaSelectCommand => new RelayCommand(() =>
        {
            SelectionService.ClearSelection();
            Editor.SetDrawingMode(MapDrawingMode.RealmAreaSelect);
            _editor.ActivateTool(EditorToolType.SelectionTool);
        });

        public ICommand LassoSelectCommand => new RelayCommand(() =>
        {
            SelectionService.ClearSelection();
            Editor.SetDrawingMode(MapDrawingMode.RealmLassoSelect);
            _editor.ActivateTool(EditorToolType.SelectionTool);
        });

        // -------------------------
        // Layout Methods
        // -------------------------
        public ICommand AlignLeftCommand => new RelayCommand(() =>
        {
            LayoutService.AlignLeft();
        });

        public ICommand AlignCenterCommand => new RelayCommand(() =>
        {
            LayoutService.AlignCenter();
        });

        public ICommand AlignRightCommand => new RelayCommand(() =>
        {
            LayoutService.AlignRight();
        });

        public ICommand AlignTopCommand => new RelayCommand(() =>
        {
            LayoutService.AlignTop();
        });

        public ICommand AlignMiddleCommand => new RelayCommand(() =>
        {
            LayoutService.AlignMiddle();
        });

        public ICommand AlignBottomCommand => new RelayCommand(() =>
        {
            LayoutService.AlignBottom();
        });

        public ICommand LayoutOnPathCommand => new RelayCommand(() =>
        {
            LayoutService.LayoutOnPath();
        });

        public ICommand DrawPathCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawFreeformLayoutPath);
            _editor.ActivateTool(EditorToolType.LayoutPathTool);
        });

        public ICommand DrawArcCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.DrawArcLayoutPath);
            _editor.ActivateTool(EditorToolType.LayoutPathTool);
        });

        private bool _applyToBackground = true;
        public bool ApplyToBackground
        {
            get => _applyToBackground;
            set
            {
                _applyToBackground = value;
                ThemeManager.ApplyToBackground = value;
            }
        }

        public bool ApplyToOcean
        {
            get => ThemeManager.ApplyToOcean;
            set
            {
                ThemeManager.ApplyToOcean = value;
            }
        }

         public bool ApplyToLandforms
        {
            get => ThemeManager.ApplyToLandforms;
            set
            {
                ThemeManager.ApplyToLandforms = value;
            }
        }

        public bool ApplyToFreshwater
        {
            get => ThemeManager.ApplyToFreshwater;
            set
            {
                ThemeManager.ApplyToFreshwater = value;
            }
        }

        public bool ApplyToPaths
        {
            get => ThemeManager.ApplyToPaths;
            set
            {
                ThemeManager.ApplyToPaths = value;
            }
        }

        public bool ApplyToSymbolColors
        {
            get => ThemeManager.ApplyToSymbolColors;
            set
            {
                ThemeManager.ApplyToSymbolColors = value;
            }
        }

        public bool ApplyToLabels
        {
            get => ThemeManager.ApplyToLabels;
            set
            {
                ThemeManager.ApplyToLabels = value;
            }
        }

        public bool ApplyToLabelPresets
        {
            get => ThemeManager.ApplyToLabelPresets;
            set
            {
                ThemeManager.ApplyToLabelPresets = value;
            }
        }

        private string? _selectedTheme;
        public string? SelectedTheme
        {
            get => _selectedTheme;
            set => SetProperty(ref _selectedTheme, value);
        }

        SaveApplyThemeDialog? saveApplyThemeDlg = null;

        public ICommand OpenThemeDialogCommand => new RelayCommand(() =>
        {
            saveApplyThemeDlg = new()
            {
                DataContext = this
            };

            saveApplyThemeDlg.ShowDialog();
        });

        public ICommand CancelThemeDialogCommand => new RelayCommand(() =>
        {
            saveApplyThemeDlg?.Close();
            saveApplyThemeDlg = null;
        });

        public ICommand ApplyThemeCommand => new RelayCommand(() =>
        {
            FindAndApplyTheme(SelectedTheme);

            saveApplyThemeDlg?.Close();
            saveApplyThemeDlg = null;
        });

        public ICommand CreateThemeCommand => new RelayCommand(() =>
        {
            ThemeNameDialog themeNameDialog = new()
            {
                DataContext = this
            };

            bool? result = themeNameDialog.ShowDialog();

            if (result == true)
            {
                if (!string.IsNullOrEmpty(SelectedTheme) && !ThemeManager.ThemeNames.Contains(SelectedTheme))
                {
                    if (UserInterfaceUtilities.IsValidFileName(SelectedTheme))
                    {
                        // create and save a new theme based on the current settings in the UI
                        MapTheme newTheme = new()
                        {
                            ThemeName = SelectedTheme,
                            IsDefaultTheme = false,
                            IsSystemTheme = false,
                            BackgroundTextureId = BackgroundViewModel.TextureBrowser.SelectedAssetId,
                            BackgroundTextureScale = BackgroundViewModel.TextureScale,
                            MirrorBackgroundTexture = BackgroundViewModel.MirrorTexture,
                            OceanTextureId = OceanViewModel.TextureBrowser.SelectedAssetId,
                            OceanTextureScale = OceanViewModel.TextureScale,
                            OceanTextureOpacity = OceanViewModel.TextureOpacity,
                            MirrorOceanTexture = OceanViewModel.MirrorTexture,
                            OceanColorOverlayEnabled = OceanViewModel.OceanColor.ToSKColor() != SKColors.Transparent,
                            OceanOverlayColor = OceanViewModel.OceanColor.ToSKColor(),
                            UseLandformTextureBackground = LandformViewModel.TextureFill,
                            LandformBackgroundColor = LandformViewModel.LandformBackgroundColor.ToSKColor(),
                            LandformOutlineColor = LandformViewModel.LandformOutlineColor.ToSKColor(),
                            LandformTextureId = LandformViewModel.TextureBrowser.SelectedAssetId,
                            LandformOutlineWidth = LandformViewModel.LandformOutlineWidth,
                            LandformShadingDepth = LandformViewModel.LandformShadingDepth,
                            CoastlineStyle = LandformViewModel.SelectedCoastlineStyle,
                            CoastlineEffectDistance = LandformViewModel.CoastlineEffectDistance,
                            EnableCoastlineBlur = true,
                            CoastlineColor = LandformViewModel.CoastlineColor.ToSKColor(),
                            ShorelineColor = WaterViewModel.ShorelineColor.ToSKColor(),
                            DeepWaterColor = WaterViewModel.DeepWaterColor.ToSKColor(),
                            ShallowWaterColor = WaterViewModel.ShallowWaterColor.ToSKColor(),
                            PathColor = PathViewModel.PathColor.ToSKColor(),
                            LabelFontFamily = LabelsViewModel.FontStyle.Family,
                            LabelFontSize = LabelsViewModel.FontStyle.Size,
                            LabelFontBold = LabelsViewModel.FontStyle.Bold,
                            LabelFontItalic = LabelsViewModel.FontStyle.Italic,
                            LabelColor = LabelsViewModel.LabelColor.ToSKColor(),
                            LabelOutlineColor = LabelsViewModel.OutlineColor.ToSKColor(),
                            LabelOutlineWidth = LabelsViewModel.OutlineWidth,
                            LabelGlowColor = LabelsViewModel.GlowColor.ToSKColor(),
                            LabelGlowStrength = (int)LabelsViewModel.GlowStrength,
                            VignetteColor = BackgroundViewModel.VignetteColor.ToSKColor(),
                            VignetteShape = BackgroundViewModel.VignetteType,
                            VignetteStrength = (int)BackgroundViewModel.VignetteStrength,
                            CustomSymbolColors = [SymbolsViewModel.SymbolColor1.ToSKColor(), SymbolsViewModel.SymbolColor2.ToSKColor(), SymbolsViewModel.SymbolColor3.ToSKColor()],
                        };

                        string themePath = Path.Combine(_themeManager.ThemesFolder, newTheme.ThemeName) + RealmStudioFileFormat.RealmStudioThemeExtension;

                        try
                        {
                            MapFileMethods.SerializeTheme(newTheme, themePath);
                            _themeManager.ThemeNames.Add(newTheme.ThemeName);
                            _themeManager.Themes.Add(newTheme);

                            MessageDialog dlg = MessageDialogFactory.InformationDialog("Theme Saved", $"Theme {newTheme.ThemeName} saved.");
                            dlg.ShowDialog();
                        }
                        catch (Exception ex)
                        {
                            MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error Saving Theme", "An error occured saving the theme: " + ex.Message);
                            dlg.ShowDialog();
                        }
                    }
                }
            }


        });

        // -------------------------
        // Other Methods
        // -------------------------

        public void FindAndApplyTheme(string? themeName)
        {
            if (string.IsNullOrEmpty(themeName))
            {
                return;
            }

            LabelsViewModel.LabelPresets.Clear();

            foreach (var tn in ThemeManager.ThemeNames)
            {
                if (string.Equals(tn, themeName, StringComparison.OrdinalIgnoreCase))
                {
                    MapTheme? theme = ThemeManager.LoadThemeByName(themeName);

                    if (theme != null)
                    {
                        ThemeManager.ResolveThemeAssets(theme, this);
                        ThemeManager.ApplyTheme(theme, this);
                    }
                }
            }

            LabelsViewModel.AddLabelPresets();
        }

        public void FindAndApplyDefaultTheme()
        {
            LabelsViewModel.LabelPresets.Clear();

            foreach (var th in ThemeManager.Themes)
            {
                if (th.IsDefaultTheme)
                {
                    MapTheme? theme = ThemeManager.LoadThemeByName(th.ThemeName);

                    if (theme != null)
                    {
                        ThemeManager.ResolveThemeAssets(theme, this);
                        ThemeManager.ApplyTheme(theme, this);
                    }
                }
            }

            LabelsViewModel.AddLabelPresets();
        }

        public bool TryShutdown()
        {
            bool shutdown = false;

            if (ProjectManager.CurrentProject == null)
            {
                shutdown = true;
            }

            if (!CommandService.HasUnsavedChanges)
            {
                shutdown = true;
            }

            if (!shutdown)
            {
                // Save / Don't Save / Cancel
                MessageDialog dlg = MessageDialogFactory.SaveConfirmationDialog("Save Project", "There are unsaved changes. Save the project?");

                dlg.ShowDialog();

                switch (((MessageDialogViewModel)dlg.DataContext).Result)
                {
                    case MessageDialogResult.Yes:
                        SaveRealmProject();
                        shutdown = true;
                        break;
                    case MessageDialogResult.No:
                        shutdown = true;
                        break;
                    case MessageDialogResult.Cancel:
                        shutdown = false;
                        break;
                }
            }

            if (shutdown && ProjectManager.CurrentProject != null)
            {
                RecoveryService.RemoveRecoveryPackages(ProjectManager.CurrentProject);
            }

            return shutdown;
        }

        public void OnDrawingModeChanged(MapDrawingMode mode)
        {
            DrawingModeLabel = SetDrawingModeLabel();
        }

        public void CreateRealmProject(CreateOpenPackageResult result)
        {
            if (!result.IsNew)
            {
                OpenRealmProject(result);
            }
            else
            {
                if (string.IsNullOrEmpty(result.MapName))
                {
                    result.MapName = "Default";
                }

                if (string.IsNullOrEmpty(result.FilePath))
                {
                    result.FilePath = result.MapName + RealmStudioFileFormat.RawMapExtension;
                }

                // Create new realm project and map
                RealmStudioMap map = CreateMap(result);

                RealmStudioProject mapProject = new();

                MapProjectMetadata metadata = new()
                {
                    ProjectName = result.MapName,
                    ProjectFilePath = result.FilePath,
                    Description = map.RealmDescription,
                    RealmType = result.ProjectType,
                    Created = DateTime.Now,
                    Modified = DateTime.Now,
                };


                mapProject.Metadata = metadata;

                mapProject.ActiveMapId = map.MapId;

                MapProjectEntry entry = MapProjectHandler.CreateProjectEntry(map, null);

                mapProject.Maps.Add(entry);

                RealmStudioXLogger.Info($"Creating and Opening project: {mapProject.Metadata.ProjectName}, Id: {mapProject.Metadata.ProjectId}");
                RealmStudioXLogger.Info($"Creating and Opening map: {map.MapName}, Id: {map.MapId}");

                ProjectManager.OpenProject(mapProject);

                ProjectViewModel.LoadProject(mapProject);

                InitializeScene(map);

                MapName = mapProject.Metadata.ProjectName + ": " + map.MapName;
                MapSizeLabel = $"Map Size: {map.MapWidth} x {map.MapHeight}, Map Area: {map.MapAreaWidth} x {map.MapAreaHeight} {map.MapAreaUnits}";

                _editor.UpdateMapScene();

                SetDrawingLayerLabel();

                string? themeName = result.Theme;

                if (!string.IsNullOrEmpty(themeName))
                {
                    FindAndApplyTheme(themeName);
                }
                else
                {
                    FindAndApplyDefaultTheme();
                }

                _editor.State.StatusMessage = $"Project {mapProject.Metadata.ProjectName} created and opened.";
            }
        }

        public static RealmStudioMap CreateMap(CreateOpenPackageResult result)
        {
            if (string.IsNullOrEmpty(result.MapName))
            {
                result.MapName = "Default";
            }

            if (string.IsNullOrEmpty(result.FilePath))
            {
                result.FilePath = result.MapName + RealmStudioFileFormat.RawMapExtension;
            }

            RealmStudioMap map = MapBuilder.CreateMap(result.FilePath, result.MapName, result.Width, result.Height, result.MapAreaWidth, result.MapAreaHeight, result.MapAreaUnits);
            map.RealmType = result.MapType;

            return map;
        }

        public void OpenRealmProject(CreateOpenPackageResult result)
        {
            RealmStudioMap? map = null;
            RealmStudioProject? project = null;

            if (result.IsNew)
            {
                CreateRealmProject(result);
            }
            else
            {
                if (result.Project == null)
                {
                    return;
                }

                // Load existing map project
                project = result.Project;
                
                if (project != null)
                {
                    map = FindActiveMap(project);
                }
            }

            if (project != null && map != null)
            {
                if (string.IsNullOrWhiteSpace(project.Metadata!.ProjectId))
                {
                    project.Metadata.ProjectId =
                        Guid.NewGuid().ToString();

                    RealmStudioXLogger.Info(
                        $"Assigned ProjectId {project.Metadata.ProjectId} " +
                        $"to project '{project.Metadata.ProjectName}'.");
                }

                RealmStudioXLogger.Info($"Opening project: {project.Metadata.ProjectName}, Id: {project.Metadata.ProjectId}");
                RealmStudioXLogger.Info($"Opening map: {map.MapName}, Id: {map.MapId}");

                // check for recovery files
                List<RecoveryPackage> recoveryPackages = RecoveryService.GetRecoveryPackages(project);

                RealmStudioXLogger.Info($"Located {recoveryPackages.Count} recovery packages for the project.");

                ProjectManager.OpenProject(project);

                ProjectViewModel.LoadProject(project);

                OpenMap(project, map);

                if (string.IsNullOrEmpty(project.Metadata!.ProjectFilePath) && !string.IsNullOrEmpty(result.FilePath))
                {
                    project.Metadata.ProjectFilePath = result.FilePath;
                }

                FindAndApplyDefaultTheme();

                _editor.State.StatusMessage = $"Project {project.Metadata.ProjectName} opened.";

                ProcessRecoveryPackages(project, recoveryPackages);
            }
        }

        private void ProcessRecoveryPackages(RealmStudioProject project, List<RecoveryPackage> recoveryPackages)
        {
            foreach (RecoveryPackage recoveryPackage in recoveryPackages)
            {
                MessageDialog dlg = MessageDialogFactory.MapRecoveryDialog("Recovery File Found",
                    $"A recovery package for map {recoveryPackage.Map.MapName} has been found. Would you like to restore the map, import it as a new map, or ignore it?");

                dlg.ShowDialog();

                switch (((MessageDialogViewModel)dlg.DataContext).Result)
                {
                    case MessageDialogResult.Restore:
                        RestoreMap(project, recoveryPackage);
                        CommandService.MarkProjectDataModified();
                        break;
                    case MessageDialogResult.Import:
                        ImportMapFromRecovery(project, recoveryPackage);
                        CommandService.MarkProjectDataModified();
                        break;
                    case MessageDialogResult.Ignore:
                        break;
                }
            }
        }

        public void RestoreMap(RealmStudioProject project,  RecoveryPackage recoveryPackage)
        {
            RealmStudioMap map = recoveryPackage.Map;
            MapProjectEntry? mapEntry = null;
            int entryIndex = -1;

            // find the project entry for the map
            for (int i = 0; i < project.Maps.Count; i++)
            {
                MapProjectEntry mpe = project.Maps[i];

                if (mpe.MapId == map.MapId)
                {
                    mapEntry = mpe;
                    entryIndex = i;
                    break;
                }
            }

            // create a bitmap with the same aspect ratio as the map
            SKBitmap preview = CreateMapPreview(map);

            if (mapEntry == null)
            {
                mapEntry ??= MapProjectHandler.CreateProjectEntry(map, preview.Copy());
            }
            else
            {
                mapEntry.Preview = preview.Copy();
            }

            string mapPreviewFileName = map.MapId + ".png";
            MapMetadata mapMetadata = mapEntry.Metadata;

            mapMetadata.PreviewFile = mapPreviewFileName;
            mapMetadata.Modified = DateTime.Now;
            project.Metadata.Modified = DateTime.Now;

            mapEntry.Metadata = mapMetadata;
            mapEntry.Map = map;

            if (entryIndex > -1)
            {
                project.Maps[entryIndex] = mapEntry;
            }
            else
            {
                project.Maps.Add(mapEntry);
            }

            OpenMap(project, map);
        }

        public void ImportMapFromRecovery(RealmStudioProject project, RecoveryPackage recoveryPackage)
        {
            RealmStudioMap map = recoveryPackage.Map;
            map.MapName = $"{map.MapName} (Recovered: {DateTime.Now})";
            map.MapId = Guid.NewGuid().ToString();      // since the recovered map is being imported, give it a new Guid id

            // create a bitmap with the same aspect ratio as the map
            SKBitmap preview = CreateMapPreview(map);

            MapProjectEntry mapEntry = MapProjectHandler.CreateProjectEntry(map, preview.Copy());

            string mapPreviewFileName = map.MapId + ".png";
            MapMetadata mapMetadata = mapEntry.Metadata;

            mapMetadata.PreviewFile = mapPreviewFileName;
            mapMetadata.Modified = DateTime.Now;
            project.Metadata.Modified = DateTime.Now;

            mapEntry.Metadata = mapMetadata;

            project.Maps.Add(mapEntry);

            ProjectManager.OpenProject(project);

            ProjectViewModel.LoadProject(project);

            OpenMap(project, map);
        }

        public static RealmStudioMap FindActiveMap(RealmStudioProject project)
        {
            RealmStudioMap? map = null;
            string activeMapId = project.ActiveMapId;

            // get the active map

            foreach (MapProjectEntry entry in project.Maps)
            {
                if (entry.MapId == activeMapId)
                {
                    map = entry.Map;
                    break;
                }
            }

            // couldn't find an active map, so use the first map in the project
            if (map == null)
            {
                map = project.Maps[0].Map;
                project.ActiveMapId = project.Maps[0].MapId;
            }

            return map;
        }

        public void OpenMap(RealmStudioProject project, RealmStudioMap map)
        {
            InitializeScene(map);

            MapName = project.Metadata.ProjectName + ": " + map.MapName;
            MapSizeLabel = $"Map Size: {map.MapWidth} x {map.MapHeight}, Map Area: {map.MapAreaWidth} x {map.MapAreaHeight} {map.MapAreaUnits}";

            ScaleViewModel.UnitLabel = map.MapAreaUnits;
            ScaleViewModel.FontStyle = new FontStyleModel
            {
                Family = "Segoe UI",
                Size = 14,
            };

            FinalizeMapLoad(map);

            _editor.Scene!.MarkLandClipPathModified();
            _editor.Scene!.MarkWaterSystemClipPathModified();

            PaintService.SetMapDimensions(map.MapWidth, map.MapHeight);

            _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.DRAWINGLAYER));
            
            _editor.SetDrawingMode(MapDrawingMode.None);

            _editor.Commands.ClearAll();

            RecoveryService.SelectedProject = project;
            RecoveryService.SelectedMap = map;

            SelectionService.ClearSelection();
            SelectedTabIndex = 1;

            _editor.State.StatusMessage = $"Map {map.MapName} opened.";

            _editor.RequestRedraw();
        }

        public void FinalizeMapLoad(RealmStudioMap map)
        {
            PlacedMapFrame? pmf = null;
            MapGrid? grid = null;

            // go through the map and load textures and bitmaps, etc.
            // load shape assets
            AssetInitializer.InitializeMapShapeAssets(map, _assetManager, _fontManager);

            // finalize the geometry of the shapes
            foreach (MapLayer layer in map.MapLayers)
            {
                foreach (MapComponent2D shape  in layer.Shapes)
                {
                    shape.FinalizeShapeGeometry(map);

                    // special handling to set frame
                    if (shape is PlacedMapFrame frame && frame.FrameDefinition != null)
                    {
                        pmf = frame;
                    }

                    // special handling to set grid
                    if (shape is MapGrid mg)
                    {
                        grid = mg;
                    }
                }

                layer.RebuildIndexes();
            }

            foreach (WaterSystem ws in map.WaterSystems)
            {
                ws.FinalizeWaterSystem(map);
            }

            // background
            if (!string.IsNullOrEmpty(map.Background.TextureId))
            {
                TextureFillRequest fillRequest = new()
                {
                    TextureId = map.Background.TextureId,
                    Scale = (float)map.Background.Scale,
                    Mirror = map.Background.Mirror,
                };

                _editor.FillBackground(fillRequest);
            }

            // ocean texture
            if (!string.IsNullOrEmpty(map.Ocean.TextureId))
            {
                TextureFillRequest applyTextureRequest = new()
                {
                    TextureId = map.Ocean.TextureId,
                    Scale = (float)map.Ocean.Scale,
                    Opacity = map.Ocean.TextureOpacity,
                    Mirror = map.Ocean.Mirror,
                    Color = map.Ocean.OverlayColor,
                };

                _editor.ApplyOceanTexture(applyTextureRequest);
            }

            // set the frame
            if (pmf != null && pmf.FrameDefinition != null)
            {
                _editor.SetFrame(pmf.FrameDefinition, pmf.FrameTint, pmf.FrameScale);
            }

            // set the grid (the view model has to be set with the grid values)
            if (grid != null)
            {
                OverlaysViewModel.GridColor = grid.GridColor.ToColor();
                OverlaysViewModel.GridEnabled = true;
                OverlaysViewModel.GridLineWidth = grid.GridLineWidth;
                OverlaysViewModel.GridLayer = grid.GridLayerIndex;
                OverlaysViewModel.GridSize = grid.GridSize;
                OverlaysViewModel.GridType = grid.GridType;
                OverlaysViewModel.ShowGridSize = grid.ShowGridSize;
            }

            MapLayer landDrawingLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.LANDDRAWINGLAYER);

            foreach (MapComponent2D shape in landDrawingLayer.Shapes)
            {
                if (shape is PaintedLine pl)
                {
                    pl.RequiresLandformClipping = true;
                }
            }

            MapLayer waterDrawingLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.WATERDRAWINGLAYER);

            foreach (MapComponent2D shape in waterDrawingLayer.Shapes)
            {
                if (shape is PaintedLine pl)
                {
                    pl.RequiresWaterSystemClipping = true;
                }
            }


            foreach (MapLayer layer in map.MapLayers)
            {
                layer.InvalidateAllTiles();
            }

            foreach (WaterSystem waterSystem in _editor.Scene!.Map.WaterSystems)
            {
                waterSystem.InvalidateRenderCache();
            }
        }

        public void InitializeScene(RealmStudioMap map)
        {
            ArgumentNullException.ThrowIfNull(_renderContext);

            MapScene newScene = new(map, _fontManager)
            {
                RenderContext = _renderContext
            };

            newScene.Camera.Viewport = new SKRect(0, 0, map.MapWidth, map.MapHeight);
            _editor.SetScene(newScene);

            AttachScene(newScene);
        }

        public void AttachScene(MapScene scene)
        {
            // Unhook old if needed (optional for now)

            var camera = scene.Camera;

            camera.ViewChanged += OnCameraChanged;

            // Sync immediately
            OnCameraChanged();
        }

        public void SetViewPortSize(SKRect rect)
        {
            _viewPortSize = rect;

            OnPropertyChanged(nameof(ViewportPixelWidth));
            OnPropertyChanged(nameof(ViewportPixelHeight));
            OnPropertyChanged(nameof(MaxScrollX));
            OnPropertyChanged(nameof(MaxScrollY));

        }

        private void OnCameraChanged()
        {
            OnPropertyChanged(nameof(ScrollX));
            OnPropertyChanged(nameof(ScrollY));
            OnPropertyChanged(nameof(MaxScrollX));
            OnPropertyChanged(nameof(MaxScrollY));
            OnPropertyChanged(nameof(ViewportPixelWidth));
            OnPropertyChanged(nameof(ViewportPixelHeight));
        }

        public string SetDrawingModeLabel()
        {
            string modeText = "Drawing Mode: ";

            modeText += _editor.CurrentDrawingMode switch
            {
                MapDrawingMode.None => "None",
                MapDrawingMode.LandPaint => "Paint Landform",
                MapDrawingMode.LandErase => "Erase Landform",
                MapDrawingMode.LandColorErase => "Erase Landform Color",
                MapDrawingMode.LandColor => "Color Landform",
                MapDrawingMode.OceanErase => "Erase Ocean",
                MapDrawingMode.OceanPaint => "Paint Ocean",
                MapDrawingMode.ColorSelect => "Select Color",
                MapDrawingMode.LandformSelect => "Select Landform",
                MapDrawingMode.LandformHeightMapSelect => "Select Landform",
                MapDrawingMode.WaterPaint => "Paint Water Feature",
                MapDrawingMode.WaterErase => "Erase Water Feature",
                MapDrawingMode.WaterColor => "Color Water Feature",
                MapDrawingMode.WaterColorErase => "Erase Water Feature Color",
                MapDrawingMode.LakePaint => "Paint Lake",
                MapDrawingMode.RiverPaint => "Paint River",
                MapDrawingMode.RiverEdit => "Edit River",
                MapDrawingMode.WaterFeatureSelect => "Select Water Feature",
                MapDrawingMode.PathPaint => "Draw Path",
                MapDrawingMode.PathSelect => "Select Path",
                MapDrawingMode.PathEdit => "Edit Path",
                MapDrawingMode.SymbolErase => "Erase Symbol",
                MapDrawingMode.SymbolPlace => "Place Symbol",
                MapDrawingMode.SymbolSelect => "Select Symbol",
                MapDrawingMode.SymbolColor => "Color Symbol",
                MapDrawingMode.DrawFreeformLayoutPath => "Draw Freeform Layout Path",
                MapDrawingMode.DrawArcLayoutPath => "Draw Arc Layout Path",
                MapDrawingMode.DrawLabel => "Place Label",
                MapDrawingMode.LabelSelect => "Select Label",
                MapDrawingMode.DrawBox => "Draw Box",
                MapDrawingMode.PlaceWindrose => "Place Windrose",
                MapDrawingMode.SelectMapScale => "Move Map Scale",
                MapDrawingMode.DrawMapMeasure => "Draw Map Measure",
                MapDrawingMode.RegionPaint => "Draw Region",
                MapDrawingMode.RegionSelect => "Select Region",
                MapDrawingMode.RealmAreaSelect => "Select in Rectangular Area",
                MapDrawingMode.RealmLassoSelect => "Select in Drawn Area",
                MapDrawingMode.HeightMapPaint => "Paint Height Map",
                MapDrawingMode.MapHeightIncrease => "Increase Map Height",
                MapDrawingMode.MapHeightDecrease => "Decrease Map Height",
                MapDrawingMode.DrawingLine => "Draw Line",
                MapDrawingMode.DrawingErase => "Erase",
                MapDrawingMode.DrawingPaint => "Paint",
                MapDrawingMode.DrawingRectangle => "Draw Rectangle",
                MapDrawingMode.DrawingEllipse => "Draw Ellipse",
                MapDrawingMode.DrawingPolygon => "Draw Polygon",
                MapDrawingMode.DrawingStamp => "Stamp",
                MapDrawingMode.DrawingDiamond => "Draw Diamond",
                MapDrawingMode.DrawingRoundedRectangle => "Draw Rounded Rectangle",
                MapDrawingMode.DrawingTriangle => "Draw Triangle",
                MapDrawingMode.DrawingRightTriangle => "Draw Right Triangle",
                MapDrawingMode.DrawingHexagon => "Draw Hexagon",
                MapDrawingMode.DrawingPentagon => "Draw Pentagon",
                MapDrawingMode.DrawingArrow => "Draw Arrow",
                MapDrawingMode.DrawingFivePointStar => "Draw 5-Point Star",
                MapDrawingMode.DrawingSixPointStar => "Draw 6-Point Star",
                MapDrawingMode.DrawingSelect => "Select Drawn Object",
                MapDrawingMode.InteriorFloorPaint => "Paint Interior Floor",
                MapDrawingMode.ShapeSelect => "Select Any Shape",
                _ => "Undefined",
            };

            // get the selected brush and add the brush name to the modeText
            modeText += ". Selected Brush: " + DrawingViewModel?.SelectedBrushPattern?.Name;

            return modeText;
        }

        internal void SetDrawingLayerLabel()
        {
            if (_editor.ActiveDrawingLayer != null)
            {
                DrawingLayerLabel = _editor.ActiveDrawingLayer.MapLayerName.ToString().ToUpperInvariant();
            }
            else
            {
                DrawingLayerLabel = "NONE";
            }
        }

        public double ScrollX
        {
            get => -_editor.Scene?.Camera.Pan.X ?? 0;
            set
            {
                var cam = _editor.Scene?.Camera;

                if (cam == null)
                    return;

                var clamped = Math.Clamp(value, 0, MaxScrollX);

                cam.SetPan(new SKPoint(-(float)clamped, cam.Pan.Y),
                           _viewPortSize.Width, _viewPortSize.Height);

                OnPropertyChanged();
            }
        }

        public double ScrollY
        {
            get => -_editor.Scene?.Camera.Pan.Y ?? 0;

            set
            {
                var cam = _editor.Scene?.Camera;

                if (cam == null)
                    return;

                var clamped = Math.Clamp(value, 0, MaxScrollY);

                cam.SetPan(
                    new SKPoint(cam.Pan.X, -(float)clamped),
                    _viewPortSize.Width, _viewPortSize.Height);

                OnPropertyChanged();
            }
        }

        public static ImageSource ToImageSource(SKBitmap bitmap)
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(data.ToArray());

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = stream;
            bmp.EndInit();
            bmp.Freeze(); // IMPORTANT for performance

            return bmp;
        }
    }
}
