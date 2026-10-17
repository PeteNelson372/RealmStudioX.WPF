using RealmStudioImageAnalysisLib;
using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.Editor.Tools;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.Properties;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Dialogs;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Diagnostics;
using System.Windows.Input;
using Cursors = System.Windows.Input.Cursors;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class ImportPanelViewModel : ViewModelBase
    {
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

        public List<PerimeterPipeline> _extractionPipelines = [];

        public List<PerimeterPipeline> ExtractionPipelines
        {
            get { return _extractionPipelines; }
            set { SetProperty(ref _extractionPipelines, value); }
        }

        public List<PerimeterPipeline> _refinementPipelines = [];

        public List<PerimeterPipeline> RefinementPipelines
        {
            get { return _refinementPipelines; }
            set { SetProperty(ref _refinementPipelines, value); }
        }

        public PerimeterPipeline? SelectedExtractionPipeline { get; set; }

        public PerimeterPipeline? SelectedRefinementPipeline { get; set; }

        public ImportPanelViewModel(MainWindowViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _editor = mainViewModel.Editor;
        }

        public ICommand SelectImportFileCommand => new RelayCommand(() =>
        {
            ExtractionPipelines = [.. _mainViewModel.PerimeterExtractionService.PipelineManager.GetExtractionPipelines()];
            RefinementPipelines = [.. _mainViewModel.PerimeterExtractionService.PipelineManager.GetRefinementPipelines()];

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
                    if (_editor.Scene != null && _editor.Scene.Map != null)
                    {
                        MapLayer workLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene.Map, MapBuilder.WORKLAYER);
                        workLayer.Clear();

                        ClearRegions();
                    }

                    SKBitmap importBitmap = SKBitmap.Decode(dialog.FileName);

                    if (importBitmap.Width != _editor.Scene?.Map?.MapWidth || importBitmap.Height != _editor.Scene?.Map?.MapHeight)
                    {
                        importBitmap = importBitmap.Resize(new SKImageInfo(_editor.Scene!.Map!.MapWidth, _editor.Scene.Map.MapHeight), SKSamplingOptions.Default);
                    }

                    ImageBitmap = importBitmap.Copy();

                    importBitmap.Dispose();

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

        public ICommand RunExtractionPipelineCommand => new RelayCommand(() =>
        {
            if (SelectedExtractionPipeline == null)
            {
                MessageDialogFactory.ErrorDialog("No Extraction Pipeline Selected", "Please select an extraction pipeline before running the analysis.").ShowDialog();
                return;
            }

            if (_imageBitmap != null)
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    // Analyze the image and display the results.
                    _editor.State.StatusMessage = $"Analyzing {Path.GetFileName(_imageFileName)}.";

                    // Get the extraction pipeline.
                    PerimeterPipeline pipeline = _mainViewModel.PerimeterExtractionService.PipelineManager.Get(SelectedExtractionPipeline.Name);

                    var context = new PerimeterAlgorithmContext
                    {
                        CancellationToken = CancellationToken.None,
                        OutputDirectory = LandformPerimeterExtractionService.ImageAnalysisDebugDirectory,
                    };

                    // Run the pipeline.
                    PerimeterExtractionResult analysisResult = _mainViewModel.PerimeterExtractionService.RunPipeline(pipeline, _imageBitmap, context);

                    if (!analysisResult.Succeeded)
                    {
                        RealmStudioXLogger.Error(analysisResult.FailureReason ?? $"Perimeter pipeline '{analysisResult.SelectedPipeline}' failed.");

                        MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error Analyzing Import Image",
                            analysisResult.FailureReason ?? $"Perimeter pipeline '{analysisResult.SelectedPipeline}' failed.");

                        dlg.ShowDialog();

                        return;
                    }

                    // write attempts to debug output
                    foreach (PerimeterPipelineAttempt attempt in analysisResult.Attempts)
                    {
                        Debug.WriteLine(
                            $"Pipeline '{attempt.PipelineName}': " +
                            $"score={attempt.Evaluation.Score:F1}, " +
                            $"accepted={attempt.Evaluation.Accepted}, " +
                            $"reason={attempt.Evaluation.Reason}");
                    }

                    // Add candidate regions.
                    foreach (ImportRegion candidate in analysisResult.ImportRegions)
                    {
                        ImportRegions.Add(ImportRegion.Clone(candidate));
                    }

                    _editor.State.StatusMessage = $"Found {ImportRegions.Count} candidate regions.";

                    // Add the ImportRegions to the work layer for
                    // rendering and accepting/rejecting by the user.
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
                    Mouse.OverrideCursor = null;

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

        public ICommand RunRefinementPipelineCommand => new RelayCommand(() =>
        {
            if (SelectedRefinementPipeline == null)
            {
                MessageDialogFactory.ErrorDialog("No Refinement Pipeline Selected", "Please select a refinement pipeline before running the analysis.").ShowDialog();
                return;
            }

            if (_imageBitmap != null)
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    if (_editor.Scene == null ||
                        _editor.Scene.Map == null ||
                        _imageBitmap == null ||
                        _imageBitmap.IsEmpty)
                    {
                        return;
                    }

                    _editor.State.StatusMessage = $"Refining {Path.GetFileName(_imageFileName)} perimeters.";

                    // Get the extraction pipeline.
                    PerimeterPipeline pipeline = _mainViewModel.PerimeterExtractionService.PipelineManager.Get(SelectedRefinementPipeline.Name);

                    MapLayer workLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene.Map, MapBuilder.WORKLAYER);

                    var context = new PerimeterAlgorithmContext
                    {
                        CancellationToken = CancellationToken.None,
                        OutputDirectory = LandformPerimeterExtractionService.ImageAnalysisDebugDirectory,
                    };

                    List<ImportRegion> acceptedRegions = [.. ImportRegions.Where(ir => ir.State == ImportRegionState.Accepted)];
                    PerimeterPipelineResult result = PerimeterPipelineRunner.Run(pipeline, _imageBitmap, acceptedRegions, context);

                    if (!result.Succeeded)
                    {
                        return;
                    }

                    ImportRegions.Clear();
                    workLayer.Clear();

                    foreach (SKPath landformPerimeter in result.RefinedPerimeters)
                    {
                        ImportRegion ir = new(landformPerimeter)
                        {
                            State = ImportRegionState.Proposed
                        };

                        ImportRegions.Add(ir);
                        workLayer.Add(ir);
                    }
                }
                catch (Exception ex)
                {
                    Mouse.OverrideCursor = null;

                    RealmStudioXLogger.Exception("Error running refinement pipeline", ex);

                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error Running Refinement Pipeline", ex.Message);

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
            if (_editor.Scene == null || _editor.Scene.Map == null)
            {
                return;
            }

            if (!ImportRegions.Any(ir => ir.State == ImportRegionState.Committed))
            {
                MessageDialog dlg = MessageDialogFactory.ErrorDialog("No Committed Regions", "There are no committed regions from which to create landforms.");
                dlg.ShowDialog();
                return;
            }

            MessageDialog confirmationDialog = MessageDialogFactory.ConfirmationDialog(
                "Create Landforms",
                "Landforms will be created from all committed regions. Existing landforms may be merged or modified. Uncommitted regions will be discarded. Proceed?");

            confirmationDialog.ShowDialog();

            switch (((MessageDialogViewModel)confirmationDialog.DataContext).Result)
            {
                case MessageDialogResult.Yes:
                    BuildLandformsFromCommittedRegions();
                    break;
                case MessageDialogResult.No:
                    break;
            }
        });

        private void BuildLandformsFromCommittedRegions()
        {
            if (_editor.Scene == null || _editor.Scene.Map == null)
            {
                return;
            }

            // clear the work layer of all ImportRegions
            MapLayer workLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene.Map, MapBuilder.WORKLAYER);
            workLayer.Clear();

            MapLayer landformLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene.Map, MapBuilder.LANDFORMLAYER);
            MapLayer landDrawingLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.LANDDRAWINGLAYER);
            Cmd_ModifyLandforms? modifyLandformsCommand = new(landformLayer);

            ILandformSettings landformSettings = _mainViewModel.LandformViewModel;

            string hatchTextureId = _mainViewModel.AssetManager.GetByName(AssetType.HatchTexture, "Random Hatch")[0].Id;
            string dashTextureId = _mainViewModel.AssetManager.GetByName(AssetType.HatchTexture, "Watercolor Dashes")[0].Id;

            LandformShadingSettings shading = new()
            {
                UseTextureBackground = landformSettings.TextureFill,
                LandformBackgroundColor = landformSettings.LandformBackgroundColor.ToSKColor(),
                LandformOutlineColor = landformSettings.LandformOutlineColor.ToSKColor(),
                LandformTextureId = landformSettings.LandformTextureId,
                LandformTextureScale = 1.0f,
                LandformTextureMirror = false,
                LandformOutlineWidth = landformSettings.LandformOutlineWidth,
                LandShadingDepth = landformSettings.LandformShadingDepth,
            };

            CoastlineSettings coastlineSettings = new()
            {
                CoastlineStyle = landformSettings.SelectedCoastlineStyle,
                EffectDistance = landformSettings.CoastlineEffectDistance,
                CoastlineColor = landformSettings.CoastlineColor.ToSKColor(),
                HatchTextureId = hatchTextureId,
                DashTextureId = dashTextureId,
            };

            foreach (ImportRegion ir in ImportRegions)
            {
                if (ir.State == ImportRegionState.Committed && modifyLandformsCommand != null)
                {
                    SKPath perimeterPath = new(ir.HitPath)
                    {
                        FillType = SKPathFillType.EvenOdd
                    };

                    Landform lf = new()
                    {
                        BrushRadius = landformSettings.LandformBrushSize / 2,
                        Shading = shading.Clone(),
                        Coastline = coastlineSettings.Clone(),
                        RenderMode = LandformRenderMode.Final,
                    };

                    lf.ResolveAssets(_mainViewModel.AssetManager);

                    var simplifiedPath = Utilities.SimplifyPath(perimeterPath);

                    lf.ReplaceGeometry(simplifiedPath);

                    lf.InvalidateRenderCache();

                    lf.LocalBounds = new SKRect(0, 0, lf.HitPath.Bounds.Width, lf.HitPath.Bounds.Height);

                    _editor.Scene!.MarkLandClipPathModified();

                    LandformPanelViewModel.MergeOverlappingLandforms(lf, modifyLandformsCommand, landformLayer);

                    modifyLandformsCommand.RegisterNewLandform(lf);
                    modifyLandformsCommand?.CaptureBefore(lf);

                    landformLayer.InvalidateTiles(lf.Bounds);
                    landDrawingLayer.InvalidateTiles(lf.Bounds);
                }
            }

            if (_editor.CommandService != null && modifyLandformsCommand != null)
            {
                _editor.CommandService.ActiveCommands.Execute(modifyLandformsCommand);
                _editor.SetDrawingMode(MapDrawingMode.None);
                _mainViewModel.ShowImportPanel = false;

                modifyLandformsCommand.Dispose();
            }
        }

        public ICommand AnalyzeAreaCommand => new RelayCommand(() =>
        {
            if (_imageBitmap == null ||
                _editor.ActiveEditorTool is not SelectionTool selectionTool ||
                selectionTool.SelectedArea.IsEmpty ||
                SelectedExtractionPipeline == null)
            {
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _editor.State.StatusMessage = $"Analyzing selected area.";

                SKRect selectedArea = selectionTool.SelectedArea;

                // Convert the selection to an integer pixel rectangle,
                // clamped to the source bitmap.
                int left = Math.Clamp(
                    (int)MathF.Floor(selectedArea.Left),
                    0,
                    _imageBitmap.Width);

                int top = Math.Clamp(
                    (int)MathF.Floor(selectedArea.Top),
                    0,
                    _imageBitmap.Height);

                int right = Math.Clamp(
                    (int)MathF.Ceiling(selectedArea.Right),
                    0,
                    _imageBitmap.Width);

                int bottom = Math.Clamp(
                    (int)MathF.Ceiling(selectedArea.Bottom),
                    0,
                    _imageBitmap.Height);

                if (right <= left || bottom <= top)
                    return;

                int width = right - left;
                int height = bottom - top;

                // Create a cropped copy of the original image.
                using SKBitmap croppedBitmap = new(
                    new SKImageInfo(
                        width,
                        height,
                        _imageBitmap.ColorType,
                        _imageBitmap.AlphaType));

                using (SKCanvas canvas = new(croppedBitmap))
                {
                    canvas.Clear(SKColors.Transparent);

                    canvas.DrawBitmap(
                        _imageBitmap,
                        new SKRect(left, top, right, bottom),
                        new SKRect(0, 0, width, height),
                        SKSamplingOptions.Default);
                }

                // Run the selected pipeline against croppedBitmap.
                //
                // Retrieve its "ImportRegions" output, translate each
                // candidate by (left, top), and add it to the Work layer.

                PerimeterPipeline pipeline = _mainViewModel.PerimeterExtractionService.PipelineManager.Get(SelectedExtractionPipeline.Name);

                var context = new PerimeterAlgorithmContext
                {
                    CancellationToken = CancellationToken.None,
                    OutputDirectory = LandformPerimeterExtractionService.ImageAnalysisDebugDirectory,
                };

                // Run the pipeline.
                PerimeterExtractionResult analysisResult = _mainViewModel.PerimeterExtractionService.RunPipeline(pipeline, croppedBitmap, context);

                if (!analysisResult.Succeeded)
                {
                    RealmStudioXLogger.Error(analysisResult.FailureReason ?? $"Perimeter pipeline '{analysisResult.SelectedPipeline}' failed.");

                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error Analyzing Import Image",
                        analysisResult.FailureReason ?? $"Perimeter pipeline '{analysisResult.SelectedPipeline}' failed.");

                    dlg.ShowDialog();

                    return;
                }

                // write attempts to debug output
                foreach (PerimeterPipelineAttempt attempt in analysisResult.Attempts)
                {
                    Debug.WriteLine(
                        $"Pipeline '{attempt.PipelineName}': " +
                        $"score={attempt.Evaluation.Score:F1}, " +
                        $"accepted={attempt.Evaluation.Accepted}, " +
                        $"reason={attempt.Evaluation.Reason}");
                }

                // Add candidate regions.
                foreach (ImportRegion candidate in analysisResult.ImportRegions)
                {
                    SKPath candidateGeometry = candidate.HitPath;
                    candidateGeometry.Offset(left, top);
                    ImportRegions.Add(ImportRegion.Clone(candidate));
                }

                _editor.State.StatusMessage = $"Found {ImportRegions.Count} candidate regions.";

                // Add the ImportRegions to the work layer for
                // rendering and accepting/rejecting by the user.
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
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                RealmStudioXLogger.Exception("Could not analyze selected area", ex);
                MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error Analyzing Selected Area", ex.Message);
                dlg.ShowDialog();
            }
            finally
            {
                Mouse.OverrideCursor = null;
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
                if (ir.IsSelected && ir.State != ImportRegionState.Committed)
                {
                    ir.State = ImportRegionState.Accepted;
                    ir.IsSelected = false;
                }
            }
        });

        public ICommand CommitRegionCommand => new RelayCommand(() =>
        {
            foreach (ImportRegion ir in ImportRegions)
            {
                if (ir.IsSelected && ir.State == ImportRegionState.Accepted)
                {
                    ir.State = ImportRegionState.Committed;
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
            // reset ImportRegions; all candidates are set to Proposed state and added to the work layer for rendering
            MessageDialog confirmationDialog = MessageDialogFactory.ConfirmationDialog(
                "Reset All Candidate Regions",
                "Are you sure you want to reset all candidate regions?");

            confirmationDialog.ShowDialog();

            switch (((MessageDialogViewModel)confirmationDialog.DataContext).Result)
            {
                case MessageDialogResult.Yes:
                    ResetRegions();
                    break;
                case MessageDialogResult.No:
                    break;
            }

        });

        private void ResetRegions()
        {
            foreach (ImportRegion candidate in ImportRegions)
            {
                candidate.State = ImportRegionState.Proposed;
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
            MessageDialog confirmationDialog = MessageDialogFactory.ConfirmationDialog(
                "Clear All Candidate Regions",
                "Are you sure you want to clear all candidate regions?");

            confirmationDialog.ShowDialog();

            switch (((MessageDialogViewModel)confirmationDialog.DataContext).Result)
            {
                case MessageDialogResult.Yes:
                    ClearRegions();
                    break;
                case MessageDialogResult.No:
                    break;
            }

        });

        private void ClearRegions()
        {
            ImportRegions.Clear();
            if (_editor.Scene != null && _editor.Scene.Map != null)
            {
                MapLayer workLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene.Map, MapBuilder.WORKLAYER);
                workLayer.Clear();
            }
        }
    }
}

