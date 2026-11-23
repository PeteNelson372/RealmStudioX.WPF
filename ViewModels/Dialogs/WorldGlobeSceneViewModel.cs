using Emgu.CV;
using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX._3D.Views.Controls;
using RealmStudioX.Core.Celestial;
using RealmStudioX.Infrastructure;
using RealmStudioX.Video.Encoding;
using RealmStudioX.Video.Settings;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Dialogs;
using SharpDX;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    public class WorldGlobeSceneViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainViewModel;
        public MainWindowViewModel MainViewModel => _mainViewModel;

        private readonly WorldGlobeViewer _globeViewer;
        public WorldGlobeViewer GlobeViewer => _globeViewer;

        private readonly ModelViewerControl _modelViewer;
        public ModelViewerControl ModelViewer => _modelViewer;

        private readonly WorldGlobeViewModel _worldGlobeViewModel;
        public WorldGlobeViewModel WorldGlobeModel => _worldGlobeViewModel;

        private readonly AtmosphereViewModel _atmosphereViewModel;
        public AtmosphereViewModel AtmosphereModel => _atmosphereViewModel;

        private readonly LocalStarViewModel _localStarViewModel;
        public LocalStarViewModel StarViewModel => _localStarViewModel;

        private readonly CreatePlanetViewModel _createPlanetViewModel;
        public CreatePlanetViewModel CreatePlanetModel => _createPlanetViewModel;

        private BlackHoleViewModel _localStarBlackHoleModel;
        public BlackHoleViewModel LocalStarBlackHoleModel => _localStarBlackHoleModel;

        private NebulaViewModel _localStarNebulaModel;
        public NebulaViewModel LocalStarNebulaModel => _localStarNebulaModel;

        private CoronaViewModel _localStarCoronaModel;
        public CoronaViewModel LocalStarCoronaModel => _localStarCoronaModel;

        private SunViewModel _localStarSunModel;
        public SunViewModel LocalStarSunModel => _localStarSunModel;

        private GasGiantViewModel _localStarGasGiantModel;
        public GasGiantViewModel LocalStarGasGiantModel => _localStarGasGiantModel;

        private TerrestrialPlanetViewModel _localStarTerrestrialPlanetModel;
        public TerrestrialPlanetViewModel LocalStarTerrestrialPlanetModel => _localStarTerrestrialPlanetModel;

        public EffectsViewModel EffectsModel { get; }

        public CelestialBodyPlacementService BodyPlacementService { get; } = new();

        public CelestialLightingViewModel LightingModel { get; }

        public ObservableCollection<VideoFrameThumbnail> RecordingThumbnails { get; } = [];


        public WorldGlobeSceneViewModel(MainWindowViewModel mainViewModel, WorldGlobeViewer globeViewer)
        {
            _mainViewModel = mainViewModel;
            _globeViewer = globeViewer;
            _modelViewer = globeViewer.ModelViewer;

            _worldGlobeViewModel = new(_mainViewModel, _globeViewer);
            _atmosphereViewModel = new(_mainViewModel, _globeViewer);

            _localStarSunModel = new SunViewModel(() =>
            {
                if (StarViewModel.SelectedLocalStarType == CelestialBodyType.Sun)
                {
                    StarViewModel.UpdateCurrentLocalStar();
                }
            });

            _localStarBlackHoleModel = new BlackHoleViewModel(() =>
            {
                if (StarViewModel.SelectedLocalStarType == CelestialBodyType.BlackHole)
                {
                    StarViewModel.UpdateCurrentLocalStar();
                }
            });

            _localStarNebulaModel = new NebulaViewModel(() =>
            {
                if (StarViewModel.SelectedLocalStarType == CelestialBodyType.Nebula)
                {
                    StarViewModel.UpdateCurrentLocalStar();
                }
            });


            _localStarCoronaModel = new CoronaViewModel(() =>
            {
                if (StarViewModel.SelectedLocalStarType == CelestialBodyType.Corona)
                {
                    StarViewModel.UpdateCurrentLocalStar();
                }
            });

            _localStarGasGiantModel = new GasGiantViewModel(() =>
            {
                if (StarViewModel.SelectedLocalStarType == CelestialBodyType.GasGiant)
                {
                    StarViewModel.UpdateCurrentLocalStar();
                }
            });

            _localStarTerrestrialPlanetModel = new TerrestrialPlanetViewModel(() =>
            {
                if (StarViewModel.SelectedLocalStarType == CelestialBodyType.TerrestrialPlanet)
                {
                    StarViewModel.UpdateCurrentLocalStar();
                }
            });

            LightingModel = new CelestialLightingViewModel(_globeViewer.UpdateLightingSettings);

            EffectsModel = new EffectsViewModel(_modelViewer);

            // create the view models for sun types before the local star view model,
            // then pass the sun type view models into the local star view model

            _localStarViewModel = new(_globeViewer, _localStarBlackHoleModel,
                _localStarNebulaModel, _localStarCoronaModel,
                _localStarSunModel, _localStarGasGiantModel, _localStarTerrestrialPlanetModel);

            _createPlanetViewModel = new(_globeViewer.AddPlanet, _mainViewModel.MapObjectDescriptionService, SelectProjectMap);
        }


        private PlanetViewModel? _planetModel;
        public PlanetViewModel? PlanetModel
        {
            get => _planetModel;
            private set => SetProperty(ref _planetModel, value);
        }

        private CreateMoonViewModel? _createMoonModel;
        public CreateMoonViewModel? CreateMoonModel
        {
            get => _createMoonModel;
            private set => SetProperty(ref _createMoonModel, value);
        }

        private MoonViewModel? _moonModel;
        public MoonViewModel? MoonModel
        {
            get => _moonModel;
            private set => SetProperty(ref _moonModel, value);
        }

        public ICommand SaveWorldGlobeSceneCommand => new RelayCommand(SaveScene);

        private void SaveScene()
        {
            // Capture the current scene and save it.
            WorldGlobeSceneDocument? document = CaptureSceneDocument();

            if (document != null)
            {
                SaveFileDialog dialog = new()
                {
                    InitialDirectory = ApplicationPreferences.Current.OutputDirectory,
                    RestoreDirectory = true,
                    DefaultExt = RealmStudioFileFormat.RealmStudioWorldGlobeSceneExtension,
                    AddExtension = true,
                    CheckFileExists = false,
                    OverwritePrompt = true,
                    Filter = "World Globe Scene (*.rswgsx)|*.rswgsx",
                    Title = "Save World Globe Scene",
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        string fileName = dialog.FileName;
                        MapFileMethods.SaveWorldGlobeSceneDocument(document, fileName);

                        MessageDialog dlg = MessageDialogFactory.InformationDialog("World Globe Scene Saved",
                            $"The world globe scene was saved to {Path.GetFileName(fileName)}");

                        dlg.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        RealmStudioXLogger.Exception("Failure saving World Globe Scene.", ex);
                        MessageDialog dlg = MessageDialogFactory.ErrorDialog("World Globe Scene Save Failure",
                            "Failure saving world globe scene:\n" + ex.Message);

                        dlg.ShowDialog();
                    }
                }
            }
        }

        private WorldGlobeSceneDocument? CaptureSceneDocument()
        {
            try
            {
                WorldGlobeSceneDocument document = new()
                {
                    CelestialSystem = CelestialSystemDocumentMapper.ToDocument(
                        GlobeViewer.CelestialSystem
                            ?? throw new InvalidOperationException(
                                "The celestial scene has not been initialized.")),

                    WorldGlobe = GlobeViewer.CaptureWorldGlobeSettings(),

                    Camera = ModelViewer.CapturePerspectiveCameraSettings()
                        ?? throw new InvalidOperationException(
                            "The World Globe Viewer does not have a perspective camera."),

                    Background = new SceneBackgroundSettings
                    {
                        EnableStarfield = EnableStarfield,
                        TexturePath = string.IsNullOrWhiteSpace(starfieldFileName)
                            ? null
                            : starfieldFileName
                    },

                    Atmosphere = new AtmosphereSceneSettings
                    {
                        EnableAtmosphere = AtmosphereModel.EnableAtmosphere,
                        EnableClouds = AtmosphereModel.EnableClouds,

                        AtmosphereColor = AtmosphereModel.AtmosphereColor,
                        AtmosphereDensity = AtmosphereModel.AtmosphereDensity,
                        AtmosphereAltitude = AtmosphereModel.AtmosphereAltitude,

                        UseDefaultCloudTexture = AtmosphereModel.UseDefaultCloudTexture,
                        UseCustomCloudTexture = AtmosphereModel.UseCustomCloudTexture,
                        CustomCloudTexturePath = AtmosphereModel.CustomCloudTexturePath,

                        CloudColor = AtmosphereModel.CloudColor,
                        CloudOpacity = AtmosphereModel.CloudOpacity,
                        CloudRotationRate = AtmosphereModel.CloudRotationRate
                    },

                    Lighting = new CelestialLightingSettings
                    {
                        EnableAmbientLight = LightingModel.EnableAmbientLight,
                        AmbientLightColor = LightingModel.AmbientLightColor,
                        AmbientLightIntensity = LightingModel.AmbientLightIntensity
                    },

                    Effects = EffectsModel.CaptureSettings(),
                };

                return document;
            }
            catch (Exception ex)
            {
                RealmStudioXLogger.Exception("Failure capturing World Globe Scene.", ex);

                MessageDialog dlg = MessageDialogFactory.ErrorDialog(
                    "World Globe Scene Capture Failure",
                    "Failure capturing world globe scene:\n" + ex.Message);

                dlg.ShowDialog();

                return null;
            }
        }

        public ICommand LoadWorldGlobeSceneCommand => new RelayCommand(LoadScene);

        private void LoadScene()
        {
            OpenFileDialog dialog = new()
            {
                Title = "Load World Globe Scene File",
                Filter = "World Globe Scene (*.rswgsx)|*.rswgsx",
                Multiselect = false,
                DefaultExt = RealmStudioFileFormat.RealmStudioWorldGlobeSceneExtension,
                InitialDirectory = ApplicationPreferences.Current.OutputDirectory,
                CheckFileExists = true
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                try
                {
                    string sceneFileName = dialog.FileName;

                    WorldGlobeSceneDocument loadedSceneDoc = MapFileMethods.LoadWorldGlobeSceneDocument(sceneFileName);

                    // construct the world globe scene from the loaded WorldGlobeSceneDocument

                    RestoreSceneSettings(loadedSceneDoc);
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("Failure loading World Globe Scene.", ex);
                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("World Globe Scene Load Failure",
                        "Failure loading world globe scene:\n" + ex.Message);

                    dlg.ShowDialog();
                }

            }
        }

        public void RestoreSceneSettings(WorldGlobeSceneDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);

            // Restore the World Globe and its existing runtime hierarchy.
            // The document mapper must already have reconstructed CelestialSystem.
            CelestialSystem celestialSystem =
                CelestialSystemDocumentMapper.FromDocument(
                    document.CelestialSystem);

            GlobeViewer.RestoreWorldGlobe(
                celestialSystem,
                document.WorldGlobe);

            // Restore the independent scene settings.
            RestoreLighting(document.Lighting);
            RestoreBackground(document.Background);

            ModelViewer.ApplyPerspectiveCameraSettings(
                document.Camera);

            // Restore post-processing effects
            EffectsModel.RestoreSettings(document.Effects);
        }

        private void RestoreLighting(CelestialLightingSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            LightingModel.EnableAmbientLight =
                settings.EnableAmbientLight;

            LightingModel.AmbientLightColor =
                settings.AmbientLightColor;

            LightingModel.AmbientLightIntensity =
                settings.AmbientLightIntensity;
        }

        private void RestoreBackground(SceneBackgroundSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            EnableStarfield = settings.EnableStarfield;
            starfieldFileName = settings.TexturePath ?? string.Empty;

            if (EnableStarfield &&
                !string.IsNullOrWhiteSpace(starfieldFileName) &&
                File.Exists(starfieldFileName))
            {
                SetBackground(starfieldFileName);
            }
            else
            {
                ModelViewer.SetBackgroundColor(
                    WorldGlobeViewer.DefaultSpaceColor);
            }
        }

        public ICommand CaptureImageCommand => new RelayCommand(async () =>
        {
            SaveFileDialog dialog = new()
            {
                DefaultExt = ".png",
                AddExtension = true,
                CheckFileExists = false,
                OverwritePrompt = true,
                Filter = UserInterfaceUtilities.GetCommonImageFilter(),
                Title = "Save World Globe Snapshot",
                FilterIndex = 3
            };

            if (dialog.ShowDialog() != true)
                return;

            RenderTargetBitmap frameBitmap = _modelViewer.CaptureFrame();

            BitmapEncoder encoder = Path.GetExtension(dialog.FileName)
                .ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" or ".jfif" or ".jpe" =>
                    new JpegBitmapEncoder { QualityLevel = 100 },

                ".bmp" or ".dib" or ".rle" =>
                    new BmpBitmapEncoder(),

                ".gif" =>
                    new GifBitmapEncoder(),

                _ =>
                    new PngBitmapEncoder()
            };

            encoder.Frames.Add(BitmapFrame.Create(frameBitmap));

            using FileStream fs = new(
                dialog.FileName,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

            encoder.Save(fs);
        });

        /// <summary>
        /// Opens the project-map picker without changing the active editor map.
        /// Returns null when the user cancels the selection.
        /// </summary>
        private string? SelectProjectMap(string? currentMapId)
        {
            RealmStudioProject? project =
                _mainViewModel.ProjectManager.CurrentProject;

            if (project is null)
                return null;

            MapSelectionDialog dialog = new(
                project,
                _mainViewModel.CommandService,
                currentMapId,
                _globeViewer);

            return dialog.ShowDialog() == true
                ? dialog.SelectedMapId
                : null;
        }

        public void SelectMoonParent(CelestialBodyNode parent)
        {
            ArgumentNullException.ThrowIfNull(parent);
            PlanetModel = null;
            MoonModel = null;
            CreateMoonModel = new CreateMoonViewModel(
                parent,
                _globeViewer.AddMoon,
                _mainViewModel.MapObjectDescriptionService,
                SelectProjectMap);
        }

        public void SelectMoon(CelestialBodyNode moon)
        {
            ArgumentNullException.ThrowIfNull(moon);
            PlanetModel = null;
            CreateMoonModel = null;
            MoonModel = new MoonViewModel(
                moon,
                _globeViewer.UpdateMoon,
                _globeViewer.RemoveMoon,
                _mainViewModel.MapObjectDescriptionService,
                SelectProjectMap);
        }

        public void SelectPlanet(CelestialBodyNode planet)
        {
            MoonModel = null;
            CreateMoonModel = null;
            PlanetModel = new PlanetViewModel(
                planet,
                _globeViewer.UpdatePlanet,
                _globeViewer.RemovePlanet,
                _mainViewModel.MapObjectDescriptionService,
                SelectProjectMap);
        }

        public void ClearSelectedPlanet()
        {
            PlanetModel = null;
            MoonModel = null;
            CreateMoonModel = null;
        }

        private string starfieldFileName = string.Empty;

        private bool _enableStarfield = true;
        public bool EnableStarfield
        {
            get => _enableStarfield;
            set => _enableStarfield = value;
        }


        private bool _enableAnimation;

        public bool EnableAnimation
        {
            get => _enableAnimation;
            set
            {
                if (!SetProperty(ref _enableAnimation, value))
                {
                    return;
                }

                GlobeViewer?.SetAnimationEnabled(value);
            }
        }


        /// <summary>
        /// Video Recording
        /// </summary>

        private CancellationTokenSource? _recordingCancellation;
        private bool _isRecording;
        private int _recordingDurationSeconds = 60;
        private int _recordedFrames;

        private const int RecordingWidth = 960;
        private const int RecordingHeight = 540;

        public bool IsRecording
        {
            get => _isRecording;
            private set
            {
                if (SetProperty(ref _isRecording, value))
                {
                    OnPropertyChanged(nameof(RecordVideoButtonText));
                    OnPropertyChanged(nameof(CanEditScene));
                    OnPropertyChanged(nameof(TaskbarProgressState));
                    UpdateMainWindowTaskbarProgress();
                }
            }
        }

        public string RecordVideoButtonText =>
            IsRecording ? "Stop Recording" : "Record Video";

        // The scene and recording settings are locked while frames are captured.
        public bool CanEditScene => !IsRecording;

        public TaskbarItemProgressState TaskbarProgressState =>
            IsRecording
                ? TaskbarItemProgressState.Normal
                : TaskbarItemProgressState.None;

        private int _totalRecordingFrames;
        public int TotalRecordingFrames
        {
            get => _totalRecordingFrames;
            private set
            {
                if (SetProperty(ref _totalRecordingFrames, value))
                {
                    OnPropertyChanged(nameof(RecordingProgress));
                    OnPropertyChanged(nameof(RecordingFrameStatus));
                    OnPropertyChanged(nameof(RecordedFramesDisplay));
                    UpdateMainWindowTaskbarProgress();
                }
            }
        }

        public double RecordingProgress => TotalRecordingFrames > 0
            ? Math.Clamp((double)RecordedFrames / TotalRecordingFrames, 0.0, 1.0)
            : 0.0;

        public string RecordingFrameStatus =>
            $"Frame {RecordedFrames:N0} / {TotalRecordingFrames:N0}";

        private string _elapsedRecordingTime = "00:00:00";
        public string ElapsedRecordingTime
        {
            get => _elapsedRecordingTime;
            private set => SetProperty(ref _elapsedRecordingTime, value);
        }

        private const int RollingFrameSampleCount = 60;
        private readonly Queue<double> _recentFrameTimesMs = new();

        private string _frameTimeDisplay = "--";
        public string FrameTimeDisplay
        {
            get => _frameTimeDisplay;
            private set => SetProperty(ref _frameTimeDisplay, value);
        }

        private string _captureRateDisplay = "--";
        public string CaptureRateDisplay
        {
            get => _captureRateDisplay;
            private set => SetProperty(ref _captureRateDisplay, value);
        }

        private string _remainingTimeDisplay = "--";
        public string RemainingTimeDisplay
        {
            get => _remainingTimeDisplay;
            private set => SetProperty(ref _remainingTimeDisplay, value);
        }

        private static string FormatElapsedTime(TimeSpan elapsed) =>
            $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";

        private void ResetRecordingStatistics()
        {
            _recentFrameTimesMs.Clear();
            FrameTimeDisplay = "--";
            CaptureRateDisplay = "--";
            RemainingTimeDisplay = "--";
        }

        private void UpdateRecordingStatistics(
            double frameTimeMs,
            Stopwatch recordingStopwatch)
        {
            _recentFrameTimesMs.Enqueue(frameTimeMs);

            while (_recentFrameTimesMs.Count > RollingFrameSampleCount)
                _recentFrameTimesMs.Dequeue();

            double averageFrameTimeMs = _recentFrameTimesMs.Average();

            double captureRate = averageFrameTimeMs > 0
                ? 1000.0 / averageFrameTimeMs
                : 0.0;

            int remainingFrames = Math.Max(
                0,
                TotalRecordingFrames - RecordedFrames);

            double remainingSeconds =
                remainingFrames * averageFrameTimeMs / 1000.0;

            ElapsedRecordingTime = FormatElapsedTime(recordingStopwatch.Elapsed);
            FrameTimeDisplay = $"{averageFrameTimeMs:F0} ms";
            CaptureRateDisplay = $"{captureRate:F1} fps";
            RemainingTimeDisplay =
                FormatElapsedTime(TimeSpan.FromSeconds(remainingSeconds));
        }

        private void UpdateMainWindowTaskbarProgress()
        {
            if (Application.Current?.MainWindow is MainWindow mainWindow)
            {
                mainWindow.UpdateTaskbarRecordingProgress(
                    IsRecording,
                    RecordingProgress);
            }
        }

        public int RecordingDurationSeconds
        {
            get => _recordingDurationSeconds;
            set
            {
                SetProperty(ref _recordingDurationSeconds, value);
                OnPropertyChanged(nameof(RecordingDurationDisplay));
            }
        }

        public int RecordedFrames
        {
            get => _recordedFrames;
            private set
            {
                if (SetProperty(ref _recordedFrames, value))
                {
                    OnPropertyChanged(nameof(RecordingProgress));
                    OnPropertyChanged(nameof(RecordingFrameStatus));
                    OnPropertyChanged(nameof(RecordedFramesDisplay));
                    UpdateMainWindowTaskbarProgress();
                }
            }
        }

        public string RecordedFramesDisplay => $"{RecordedFrames:N0}/{TotalRecordingFrames:N0}";

        public string RecordingDurationDisplay => $"{RecordingDurationSeconds:N0} seconds";

        private double _animationTime;
        public double AnimationTime
        {
            get => _animationTime;
            private set => SetProperty(ref _animationTime, value);
        }

        public ICommand RecordVideoCommand => new RelayCommand(async () =>
        {
            if (_recordingCancellation is not null)
            {
                _recordingCancellation.Cancel();
                return;
            }

            await StartVideoRecordingAsync();
        });


        private async Task StartVideoRecordingAsync()
        {
            if (RecordingDurationSeconds <= 0)
            {
                MessageDialog messageDialog = MessageDialogFactory.WarningDialog(
                    "Video Recording",
                    "Recording duration must be greater than zero.");

                messageDialog.ShowDialog();
                return;
            }

            int fps = SelectedFrameRate;
            if (fps <= 0)
                return;

            string temporaryFile = Path.Combine(
                Path.GetTempPath(),
                $"RealmStudioX_{Guid.NewGuid():N}.avi");

            CancellationTokenSource cancellation = new();
            _recordingCancellation = cancellation;

            IsRecording = true;
            RecordedFrames = 0;
            TotalRecordingFrames = 0;
            ElapsedRecordingTime = "00:00:00";
            ResetRecordingStatistics();

            RecordingThumbnails.Clear();

            AviVideoWriter? writer = null;
            Exception? recordingError = null;
            bool wasCancelled = false;

            Stopwatch recordingStopwatch = Stopwatch.StartNew();

            try
            {
                // Establish the initial scene state and capture frame zero immediately.
                AnimationTime = 0.0;

                Stopwatch initialCaptureTimer = Stopwatch.StartNew();
                _globeViewer.SetAnimationTime(0.0);
                RenderTargetBitmap firstFrame = _modelViewer.CaptureFrame();
                initialCaptureTimer.Stop();

                if (firstFrame.PixelWidth != RecordingWidth ||
                    firstFrame.PixelHeight != RecordingHeight)
                {
                    throw new InvalidOperationException(
                        $"The World Globe viewport must capture at {RecordingWidth}x{RecordingHeight}. " +
                        $"The current capture size is {firstFrame.PixelWidth}x{firstFrame.PixelHeight}.");
                }

                int width = RecordingWidth;
                int height = RecordingHeight;

                VideoRecordingSettings settings = new()
                {
                    Width = width,
                    Height = height,
                    FramesPerSecond = fps,
                    Quality = 100,

                };

                writer = new AviVideoWriter(temporaryFile, settings);

                int totalFrames = checked(RecordingDurationSeconds * fps);
                TotalRecordingFrames = totalFrames;

                Stopwatch stageTimer = Stopwatch.StartNew();

                for (int i = 0; i < totalFrames; i++)
                {
                    Stopwatch frameStopwatch = Stopwatch.StartNew();

                    cancellation.Token.ThrowIfCancellationRequested();

                    RenderTargetBitmap frame;

                    if (i == 0)
                    {
                        frame = firstFrame;
                    }
                    else
                    {
                        // Yield between frames so the UI can process input, as the
                        // original Realm Studio recorder did. Do not yield between
                        // advancing the scene and capturing its frame.
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        cancellation.Token.ThrowIfCancellationRequested();

                        AnimationTime = (double)i / fps;

                        _globeViewer.SetAnimationTime(AnimationTime);
                        frame = _modelViewer.CaptureFrame();
                    }

                    // Convert Pbgra32 to the BGR32 format expected by AviVideoWriter.
                    FormatConvertedBitmap convertedFrame = new(
                        frame,
                        PixelFormats.Bgr32,
                        null,
                        0);

                    convertedFrame.Freeze();

                    int stride = width * 4;
                    byte[] pixels = new byte[stride * height];

                    convertedFrame.CopyPixels(pixels, stride, 0);
                    // Encode and write away from the UI thread.
                    AviVideoWriter activeWriter = writer;

                    await Task.Run(() => activeWriter.WriteFrame(pixels));

                    // Update wall-clock elapsed time and add one thumbnail
                    if (i % fps == 0)
                    {
                        RecordingThumbnails.Add(new VideoFrameThumbnail
                        {
                            Image = CreateRecordingThumbnail(frame),
                            TimeLabel = TimeSpan
                                .FromSeconds(AnimationTime)
                                .ToString(@"mm\:ss")
                        });
                    }

                    RecordedFrames = activeWriter.FramesWritten;

                    frameStopwatch.Stop();

                    double frameTimeMs = frameStopwatch.Elapsed.TotalMilliseconds;

                    // Frame zero's scene capture happened before the writing loop,
                    // so include that time in its first-frame measurement.
                    if (i == 0)
                    {
                        frameTimeMs += initialCaptureTimer.Elapsed.TotalMilliseconds;
                    }

                    UpdateRecordingStatistics(
                        frameTimeMs,
                        recordingStopwatch);
                }
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
            }
            catch (Exception ex)
            {
                recordingError = ex;
            }
            finally
            {
                recordingStopwatch.Stop();
                ElapsedRecordingTime = FormatElapsedTime(recordingStopwatch.Elapsed);

                try
                {
                    writer?.Complete();
                }
                catch (Exception ex)
                {
                    recordingError ??= ex;
                }

                _recordingCancellation = null;
                IsRecording = false;
                cancellation.Dispose();
            }

            if (recordingError is not null)
            {
                RecordingThumbnails.Clear();
                TryDeleteTemporaryRecording(temporaryFile);

                MessageDialog messageDialog = MessageDialogFactory.ErrorDialog(
                    "Video Recording",
                    $"The video recording could not be completed.\n{recordingError.Message}");

                messageDialog.ShowDialog();
                return;
            }

            if (RecordedFrames == 0)
            {
                RecordingThumbnails.Clear();
                TryDeleteTemporaryRecording(temporaryFile);
                return;
            }

            // An early stop requires the user's decision about the partial video.
            if (wasCancelled)
            {
                MessageDialog messageDialog = MessageDialogFactory.ConfirmationDialog(
                    "Video Recording Stopped",
                    $"Recording was stopped before completion.\n" +
                    $"{RecordedFrames} frames have been captured.\n" +
                    "Would you like to keep this video?");

                messageDialog.ShowDialog();

                switch (((MessageDialogViewModel)messageDialog.DataContext).Result)
                {
                    case MessageDialogResult.Yes:
                        break;
                    case MessageDialogResult.No:
                        RecordingThumbnails.Clear();
                        TryDeleteTemporaryRecording(temporaryFile);
                        return;
                }
            }

            // Recording is complete, or the user elected to keep the partial video.
            SaveFileDialog dialog = new()
            {
                InitialDirectory = ApplicationPreferences.Current.OutputDirectory,
                RestoreDirectory = true,
                DefaultExt = ".avi",
                AddExtension = true,
                CheckFileExists = false,
                OverwritePrompt = true,
                Filter = "AVI Video (*.avi)|*.avi",
                Title = "Save World Globe Video",
                FileName = "WorldGlobe.avi"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    File.Move(temporaryFile, dialog.FileName, true);
                }
                catch (Exception ex)
                {
                    RecordingThumbnails.Clear();
                    MessageDialog messageDialog = MessageDialogFactory.ErrorDialog(
                        "Video Recording",
                        $"The video could not be saved.\n{ex.Message}\n" +
                        $"The temporary recording remains at:\n{temporaryFile}");

                    messageDialog.ShowDialog();
                }
            }
            else
            {
                RecordingThumbnails.Clear();
                TryDeleteTemporaryRecording(temporaryFile);
            }
        }

        private static void TryDeleteTemporaryRecording(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Cleanup failure should not interrupt the UI.
            }
        }


        public int[] SupportedFrameRates => WorldGlobeViewer.SupportedFrameRates;

        private int _selectedFrameRate = 30;
        public int SelectedFrameRate
        {
            get => _selectedFrameRate;
            set => SetProperty(ref _selectedFrameRate, value);
        }

        private static RenderTargetBitmap CreateRecordingThumbnail(RenderTargetBitmap source)
        {
            const int thumbnailWidth = 120;
            const int thumbnailHeight = 70;

            double scale = Math.Min(
                (double)thumbnailWidth / source.PixelWidth,
                (double)thumbnailHeight / source.PixelHeight);

            double imageWidth = source.PixelWidth * scale;
            double imageHeight = source.PixelHeight * scale;

            double x = (thumbnailWidth - imageWidth) / 2.0;
            double y = (thumbnailHeight - imageHeight) / 2.0;

            DrawingVisual visual = new();

            using (DrawingContext context = visual.RenderOpen())
            {
                context.DrawRectangle(
                    Brushes.Transparent,
                    null,
                    new Rect(0, 0, thumbnailWidth, thumbnailHeight));

                context.DrawImage(
                    source,
                    new Rect(x, y, imageWidth, imageHeight));
            }

            RenderTargetBitmap thumbnail = new(
                thumbnailWidth,
                thumbnailHeight,
                96,
                96,
                PixelFormats.Pbgra32);

            thumbnail.Render(visual);
            thumbnail.Freeze();

            return thumbnail;
        }

        //==========================

        public ICommand ToggleStarfield => new RelayCommand(() =>
        {
            if (EnableStarfield && !string.IsNullOrEmpty(starfieldFileName))
            {
                SetBackground(starfieldFileName);
            }
            else
            {
                ModelViewer.SetBackgroundColor(WorldGlobeViewer.DefaultSpaceColor);
            }
        });

        public ICommand SelectBackgroundFileCommand => new RelayCommand(() =>
        {
            string starfieldDirectory = Path.Combine(ApplicationPreferences.Current.RootAssetDirectory, "Textures", "Starfields");

            OpenFileDialog dialog = new()
            {
                Title = "Select Star Field Image File",
                Filter = UserInterfaceUtilities.GetCommonImageFilter(),
                Multiselect = false,
                InitialDirectory = starfieldDirectory,
                CheckFileExists = true
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                starfieldFileName = dialog.FileName;

                if (EnableStarfield)
                {
                    SetBackground(starfieldFileName);
                }
            }
        });

        public void SetBackground(string backgroundFile)
        {
            if (string.IsNullOrEmpty(backgroundFile) || !File.Exists(backgroundFile))
            {
                throw new ArgumentException("Background file is empty or does not exist", nameof(backgroundFile));
            }

            BitmapImage bitmap = new();

            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(backgroundFile, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            ModelViewer.SetBackground(bitmap);
        }

        public ICommand SelectTextureFileCommand => new RelayCommand(() =>
        {
            string planetTextureDirectory = Path.Combine(
                ApplicationPreferences.Current.RootAssetDirectory,
                "Textures",
                "Planet");

            OpenFileDialog dialog = new()
            {
                Title = "Select Planet Texture Image File",
                Filter = UserInterfaceUtilities.GetCommonImageFilter(),
                Multiselect = false,
                InitialDirectory = planetTextureDirectory,
                CheckFileExists = true
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                _globeViewer.SetWorldGlobeBitmapTexture(dialog.FileName);
            }
        });

        public ICommand SelectWorldGlobeMapTextureCommand => new RelayCommand(() =>
        {
            string? currentMapId = _globeViewer.WorldGlobeMapId;

            string? mapId = SelectProjectMap(currentMapId);

            if (string.IsNullOrWhiteSpace(mapId))
                return;

            _globeViewer.SetWorldGlobeMapTexture(mapId);
        });

        public ICommand ClearTextureCommand => new RelayCommand(() =>
        {
            // reset the world globe to the default texture
            // created from the selected map

            _globeViewer.CreateGlobe();
        });

        public ICommand ResetCameraCommand => new RelayCommand(() =>
        {
            ResetModelViewerDefaults();
            _modelViewer.ResetCamera();

            _globeViewer.ResetGlobeView();
        });

        private void ResetModelViewerDefaults()
        {
            ShowViewCube = false;
            ShowCoordinateSystem = false;
        }

        private bool _showViewCube = false;

        public bool ShowViewCube
        {
            get => _showViewCube;
            set
            {
                if (_showViewCube != value)
                {
                    _showViewCube = value;
                    _modelViewer.ShowViewCube(_showViewCube);
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ShowViewCubeCommand => new RelayCommand(() =>
        {
            ShowViewCube = !ShowViewCube;
        });

        private bool _showCoordinateSystem = false;

        public bool ShowCoordinateSystem
        {
            get => _showCoordinateSystem;
            set
            {
                if (_showCoordinateSystem != value)
                {
                    _showCoordinateSystem = value;
                    _modelViewer.ShowCoordinateSystem(_showCoordinateSystem);
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ShowCoordinateSystemCommand => new RelayCommand(() =>
        {
            ShowCoordinateSystem = !ShowCoordinateSystem;
        });

        public ICommand ChangeAxisCommand => new RelayCommand(() =>
        {
            if (_modelViewer.CurrentUpDirection == ModelUpDirection.XUp)
            {
                _modelViewer.SetUpDirection(ModelUpDirection.YUp);
            }
            else if (_modelViewer.CurrentUpDirection == ModelUpDirection.YUp)
            {
                _modelViewer.SetUpDirection(ModelUpDirection.ZUp);
            }
            else if (_modelViewer.CurrentUpDirection == ModelUpDirection.ZUp)
            {
                _modelViewer.SetUpDirection(ModelUpDirection.XUp);
            }

        });

        public ICommand SetXUpCommand => new RelayCommand(() =>
        {
            _modelViewer.SetUpDirection(ModelUpDirection.XUp);
        });

        public ICommand SetYUpCommand => new RelayCommand(() =>
        {
            _modelViewer.SetUpDirection(ModelUpDirection.YUp);
        });

        public ICommand SetZUpCommand => new RelayCommand(() =>
        {
            _modelViewer.SetUpDirection(ModelUpDirection.ZUp);
        });

        public ICommand ViewFrontCommand => new RelayCommand(() =>
        {
            _modelViewer.SetCameraView(ModelViewDirection.Front);
        });

        public ICommand ViewBackCommand => new RelayCommand(() =>
        {
            _modelViewer.SetCameraView(ModelViewDirection.Back);
        });

        public ICommand ViewLeftCommand => new RelayCommand(() =>
        {
            _modelViewer.SetCameraView(ModelViewDirection.Left);
        });

        public ICommand ViewRightCommand => new RelayCommand(() =>
        {
            _modelViewer.SetCameraView(ModelViewDirection.Right);
        });

        public ICommand ViewTopCommand => new RelayCommand(() =>
        {
            _modelViewer.SetCameraView(ModelViewDirection.Top);
        });

        public ICommand ViewBottomCommand => new RelayCommand(() =>
        {
            _modelViewer.SetCameraView(ModelViewDirection.Bottom);
        });
    }

    public sealed class VideoFrameThumbnail
    {
        public BitmapSource Image { get; init; } = null!;
        public string TimeLabel { get; init; } = string.Empty;
    }

}