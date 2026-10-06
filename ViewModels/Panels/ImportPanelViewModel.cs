using Microsoft.ML.OnnxRuntime.Tensors;
using RealmStudioImageAnalysisLib;
using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.Properties;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Dialogs;
using SkiaSharp;
using System.IO;
using System.Windows.Input;
using System.Windows.Shapes;
using Cursors = System.Windows.Input.Cursors;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class ImportPanelViewModel : ViewModelBase
    {
        private readonly static string rootRealmStudioXDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments),
                "RealmStudioX");

        private static string assetsDirectory =
            Path.Combine(
                rootRealmStudioXDirectory,
                "Assets");

        private static string imageAnalysisDirectory =
            Path.Combine(
                assetsDirectory,
                "ImageAnalysis");

        private readonly MainWindowViewModel _mainViewModel;

        private readonly EditorController _editor;

        private string _imageFileName = string.Empty;

        public string ImageFileName
        {
            get { return _imageFileName; }
            set { SetProperty(ref _imageFileName, value); }
        }

        private SKBitmap? _imageBitmap = null;

        public SKBitmap? ImageBitmap
        {
            get { return _imageBitmap; }
            set { SetProperty(ref _imageBitmap, value); }
        }

        private readonly List<ImportRegion> _importRegions = [];

        public List<ImportRegion> ImportRegions => _importRegions;

        private readonly LandformPerimeterExtractionService _perimeterExtractionService;

        public ImportPanelViewModel(MainWindowViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _editor = mainViewModel.Editor;

            assetsDirectory = AssetManager.RootAssetDirectory;

            imageAnalysisDirectory = Path.Combine(assetsDirectory, "ImageAnalysis");
            _perimeterExtractionService = _mainViewModel.PerimeterExtractionService;
        }

        private LandformAnalysisResult? _analysisResult;

        public ICommand SelectImportFileCommand => new RelayCommand(() =>
        {
            OpenFileDialog dialog = new()
            {
                Title = "Select Image File",
                Filter = UserInterfaceUtilities.GetCommonImageFilter(),
                Multiselect = false,
                InitialDirectory = Settings.Default.LastFileDirectory
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                try
                {
                    ImageBitmap = SKBitmap.Decode(dialog.FileName);
                    ImageFileName = dialog.FileName;
                }
                catch (Exception ex)
                {
                    ImageFileName = string.Empty;
                    ImageBitmap?.Dispose();
                    ImageBitmap = null;

                    RealmStudioXLogger.Exception("Could not load image file", ex);
                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error Opening Import Image", "An error occured opening the image.");
                    dlg.ShowDialog();
                }
            }
        });

        public ICommand AnalyzeImportFileCommand => new RelayCommand(() =>
        {
            if (_imageBitmap != null)
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    // analyze the image and display the results
                    _editor.State.StatusMessage = $"Analyzing {Path.GetFileName(_imageFileName)}.";

                    // load image analysis pipelines
                    var files = Directory.EnumerateFiles(imageAnalysisDirectory, "*.xml", SearchOption.AllDirectories).ToList();

                    if (files.Count > 0)
                    {
                        foreach (var file in files)
                        {
                            var extension = Path.GetExtension(file).ToLowerInvariant();
                            if (extension == ".xml")
                            {
                                var definition = _perimeterExtractionService.PipelineLoader.LoadDefinition(file);
                                var pipeline = new PerimeterPipeline(definition, _perimeterExtractionService.StageRegistry);
                                _perimeterExtractionService.PipelineManager.Add(pipeline);
                            }
                        }


                    }
                    else
                    {
                        return;
                    }






                        _analysisResult = ImageLandformAnalyzer.Analyze(_imageBitmap);

                    foreach (ImportRegion candidate in _analysisResult.ImportRegions)
                    {
                        // Add to import-region collection.

                        ImportRegions.Add(ImportRegion.Clone(candidate));
                    }

                    _editor.State.StatusMessage = $"Found {ImportRegions.Count()} candidate regions.";

                    // add the ImportRegions to the work layer for rendering and accepting/rejecting by the user
                    if (_editor.Scene != null && _editor.Scene.Map != null)
                    {
                        MapLayer workLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene.Map, MapBuilder.WORKLAYER);

                        foreach (ImportRegion ir in ImportRegions)
                        {
                            workLayer.Add(ir);
                        }
                    }
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("Could not analyze import image", ex);
                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error Analyzing Import Image", ex.Message);
                    dlg.ShowDialog();
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }
        });

        public ICommand ExtractLandformsCommand => new RelayCommand(() =>
        {
            // generate MobileSAM prompts from accepted import regions
            // and use MobileSAM with the prompts to extract landform
            // perimeters, then generate Landform objects and add them
            // to the Landform layer

            if (_editor.Scene == null || _editor.Scene.Map == null || ImageBitmap == null || ImageBitmap.IsEmpty)
            {
                return;
            }

            MapLayer workLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene.Map, MapBuilder.WORKLAYER);

            List<SKPath> perimeters = ImportRegionAnalyzer.ExtractLandforms(ImportRegions, ImageBitmap);

            foreach (SKPath landformPerimeter in perimeters)
            {
                // create a new Landform object and add it to the Landform layer
                // if a theme has been selected, use the theme to set the landform's properties
                // otherwise, use default properties

                ImportLandform il = new(landformPerimeter);

                workLayer.Add(il);
            }
        });


        public ICommand SelectRegionCommand => new RelayCommand(() =>
        {
            _mainViewModel.SelectionService.ClearSelection();
            _editor.SetDrawingMode(MapDrawingMode.SelectImportRegions);
            _editor.ActivateTool(EditorToolType.SelectionTool);
        });

        public ICommand AcceptRegionCommand => new RelayCommand(() =>
        {
            foreach (ImportRegion ir in ImportRegions)
            {
                if (ir.IsSelected)
                {
                    ir.State = ImportRegionState.Accepted;
                    ir.IsSelected = false;
                }
            }
        });

        public ICommand RejectRegionCommand => new RelayCommand(() =>
        {
            foreach (ImportRegion ir in ImportRegions)
            {
                if (ir.IsSelected)
                {
                    ir.State = ImportRegionState.Rejected;
                    ir.IsSelected = false;
                }
            }
        });

        public ICommand TraceRegionCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.TraceImportRegion);
            _editor.ActivateTool(EditorToolType.ImportRegionTool);
        });

        public ICommand ResetRegionsCommand => new RelayCommand(() =>
        {
            if (_analysisResult != null)
            {
                ImportRegions.Clear();

                foreach (ImportRegion candidate in _analysisResult.ImportRegions)
                {
                    // rebuild ImportRegions
                    ImportRegions.Add(ImportRegion.Clone(candidate));
                }

                if (_editor.Scene != null && _editor.Scene.Map != null)
                {
                    MapLayer workLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene.Map, MapBuilder.WORKLAYER);
                    workLayer.Clear();

                    foreach (ImportRegion ir in ImportRegions)
                    {
                        workLayer.Add(ir);
                    }
                }
            }

        });

        public ICommand RemoveSelectedRegionCommand => new RelayCommand(() =>
        {
            if (_editor.Scene != null && _editor.Scene.Map != null)
            {
                int selectedIndex = -1;
                for (int i = 0; i < ImportRegions.Count; i++)
                {
                    if (ImportRegions[i] is ImportRegion ir && ir.IsSelected)
                    {
                        selectedIndex = i;
                        break;
                    }
                }

                if (selectedIndex > -1)
                {
                    ImportRegions.RemoveAt(selectedIndex);
                }

                if (_editor.Scene != null && _editor.Scene.Map != null)
                {
                    MapLayer workLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene.Map, MapBuilder.WORKLAYER);
                    workLayer.Clear();

                    foreach (ImportRegion ir in ImportRegions)
                    {
                        workLayer.Add(ir);
                    }
                }
            }
        });

        public ICommand ClearRegionsCommand => new RelayCommand(() =>
        {
            ImportRegions.Clear();

            if (_editor.Scene != null && _editor.Scene.Map != null)
            {
                MapLayer workLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene.Map, MapBuilder.WORKLAYER);
                workLayer.Clear();
            }
        });
    }
}

