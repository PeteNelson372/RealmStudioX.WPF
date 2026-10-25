using HelixToolkit.Wpf;
using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX._3D.Views.Controls;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.Properties;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Dialogs;
using RealmStudioX.WPF.WorldGlobe;
using SkiaSharp;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    public class WorldGlobeViewModel(MainWindowViewModel mainViewModel, WorldGlobeViewer globeViewer) : ViewModelBase
    {
        private readonly MainWindowViewModel _mainViewModel = mainViewModel;
        public MainWindowViewModel MainViewModel => _mainViewModel;

        private readonly WorldGlobeViewer _globeViewer = globeViewer;
        public WorldGlobeViewer GlobeViewer => _globeViewer;

        private readonly ModelViewerControl _modelViewer = globeViewer.ModelViewer;
        public ModelViewerControl ModelViewer => _modelViewer;

        public IReadOnlyList<LocalStarType> LocalStarTypes { get; } = Enum.GetValues<LocalStarType>();

        private LocalStar? _currentLocalStar;

        private string starfieldFileName = string.Empty;
        private string planetTextureFileName = string.Empty;

        private string? _customCloudTexturePath;

        private bool _enableStarfield = true;
        public bool EnableStarfield
        {
            get => _enableStarfield;
            set => _enableStarfield = value;
        }

        private bool _enableAtmosphere = false;
        public bool EnableAtmosphere
        {
            get => _enableAtmosphere;
            set => _enableAtmosphere = value;
        }

        private bool _enableClouds = true;
        public bool EnableClouds
        {
            get => _enableClouds;
            set
            {
                if (SetProperty(ref _enableClouds, value))
                {
                    GlobeViewer.RefreshCloudLayer();
                }
            }
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

        private double _rotationRate = 1.0;
        public double RotationRate
        {
            get => _rotationRate;
            set => _rotationRate = value;
        }

        private int _selectedFrameRate = 30;
        public int SelectedFrameRate
        {
            get => _selectedFrameRate;
            set => _selectedFrameRate = value;
        }

        private float _worldTiltAngle = 0;
        public float WorldTiltAngle
        {
            get => _worldTiltAngle;
            set
            {
                SetProperty(ref _worldTiltAngle, value);
                GlobeViewer.UpdateGlobeTilt(_worldTiltAngle);

                double actualTilt = GlobeViewer.GetCurrentAxialTilt();

                UpdateCurrentTiltAngle(actualTilt);
            }
        }

        private double _currentTiltAngle;

        public double CurrentTiltAngle
        {
            get => _currentTiltAngle;
            private set => SetProperty(ref _currentTiltAngle, value);
        }

        public void UpdateCurrentTiltAngle(double angle)
        {
            CurrentTiltAngle = Math.Clamp(angle, 0.0, 90.0);
        }

        private WorldRotationDirection _rotationDirection = WorldRotationDirection.Prograde;

        public WorldRotationDirection RotationDirection
        {
            get => _rotationDirection;
            set => _rotationDirection = value;
        }

        public ICommand ToggleAtmosphere => new RelayCommand(() =>
        {
            GlobeViewer.RefreshAtmosphere();
        });

        private Color _atmosphereColor = Colors.Transparent;
        public Color AtmosphereColor
        {
            get => _atmosphereColor;
            set
            {
                if (SetProperty(ref _atmosphereColor, value))
                {
                    _atmosphereColorBrush.Color = value;
                    GlobeViewer.RefreshAtmosphere();
                }
            }
        }

        private readonly SolidColorBrush _atmosphereColorBrush = new(Colors.Transparent);
        public Brush AtmosphereColorBrush => _atmosphereColorBrush;

        private double _atmosphereDensity = 0.01;

        public double AtmosphereDensity
        {
            get => _atmosphereDensity * 100.0;
            set
            {
                double density = Math.Clamp(value, 0.0, 100.0) / 100.0;

                if (SetProperty(ref _atmosphereDensity, density))
                {
                    GlobeViewer.RefreshAtmosphere();
                }
            }
        }

        private double _atmosphereAltitude = 0.001;
        public double AtmosphereAltitude
        {
            get => _atmosphereAltitude * 1000.0;
            set
            {
                double altitude = value / 1000.0;
                if (SetProperty(ref _atmosphereAltitude, altitude))
                {
                    GlobeViewer.RefreshAtmosphere();
                }
            }
        }

        private bool _useDefaultCloudTexture = false;

        public bool UseDefaultCloudTexture
        {
            get => _useDefaultCloudTexture;
            set
            {
                if (SetProperty(ref _useDefaultCloudTexture, value) && value)
                {
                    _globeViewer.LoadDefaultCloudTexture();
                    GlobeViewer.RefreshCloudLayer();
                }
            }
        }

        private bool _useCustomCloudTexture;

        public bool UseCustomCloudTexture
        {
            get => _useCustomCloudTexture;
            set
            {
                if (SetProperty(ref _useCustomCloudTexture, value) && value)
                {
                    LoadCustomCloudTexture();
                    GlobeViewer.RefreshCloudLayer();
                }
            }
        }

        private Color _cloudColor = Colors.Transparent;
        public Color CloudColor
        {
            get => _cloudColor;
            set
            {
                if (SetProperty(ref _cloudColor, value))
                {
                    _cloudColorBrush.Color = value;
                    GlobeViewer.RefreshCloudLayer();
                }
            }
        }

        private readonly SolidColorBrush _cloudColorBrush = new(Colors.Transparent);
        public Brush CloudColorBrush => _cloudColorBrush;

        private double _cloudOpacity = 0.4;
        public double CloudOpacity
        {
            get => _cloudOpacity;
            set
            {
                if (SetProperty(ref _cloudOpacity, value))
                {
                    GlobeViewer.RefreshCloudLayer();
                }
            }
        }

        private double _cloudRotationRate = 1.0;
        public double CloudRotationRate
        {
            get => _cloudRotationRate;
            set
            {
                if (SetProperty(ref _cloudRotationRate, value))
                {
                    GlobeViewer.RefreshCloudLayer();
                }
            }
        }

        #region Local Star Properties

        private bool _enableLocalStar;

        public bool EnableLocalStar
        {
            get => _enableLocalStar;
            set
            {
                if (SetProperty(ref _enableLocalStar, value))
                {
                    if (_enableLocalStar && _currentLocalStar != null)
                    {
                        GlobeViewer.ShowLocalStar(_currentLocalStar);
                    }
                    else
                    {
                        GlobeViewer.RemoveLocalStar();
                    }
                }
            }
        }

        private LocalStarType _selectedLocalStarType = LocalStarType.Sun;

        public LocalStarType SelectedLocalStarType
        {
            get => _selectedLocalStarType;
            set
            {
                if (SetProperty(ref _selectedLocalStarType, value))
                {
                    UpdateCurrentLocalStar();
                }
            }
        }

        private double _localStarSize = 0.09;

        public double LocalStarSize
        {
            get => _localStarSize;
            set
            {
                double size = Math.Clamp(value, 0.01, 1.0);

                if (SetProperty(ref _localStarSize, size))
                {
                    UpdateCurrentLocalStar();
                }
            }
        }

        private double _localStarDistance = 0.08;

        public double LocalStarDistance
        {
            get => _localStarDistance;
            set
            {
                if (SetProperty(ref _localStarDistance, Math.Clamp(value, 0.0, 1.0)))
                {
                    UpdateCurrentLocalStar();
                }
            }
        }

        private double _localStarOrbit = 0.0;

        public double LocalStarOrbit
        {
            get => _localStarOrbit;
            set
            {
                if (SetProperty(ref _localStarOrbit, Math.Clamp(value, 0.0, 1.0)))
                {
                    UpdateCurrentLocalStar();
                }
            }
        }

        private double _localStarPlane = 0.24;

        public double LocalStarPlane
        {
            get => _localStarPlane;
            set
            {
                if (SetProperty(ref _localStarPlane, Math.Clamp(value, 0.0, 1.0)))
                {
                    UpdateCurrentLocalStar();
                }
            }
        }

        private WorldRotationDirection _localStarRotationDirection =
            WorldRotationDirection.Prograde;

        public WorldRotationDirection LocalStarRotationDirection
        {
            get => _localStarRotationDirection;
            set
            {
                if (SetProperty(ref _localStarRotationDirection, value))
                {
                    UpdateCurrentLocalStar();
                }
            }
        }

        private double _localStarRotationRate = 1.0;

        public double LocalStarRotationRate
        {
            get => _localStarRotationRate;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, 0.0, 10.0) : 1.0;

                if (SetProperty(ref _localStarRotationRate, value))
                {
                    UpdateCurrentLocalStar();
                }
            }
        }

        private double _localStarlightIntensity = 0.1;

        public double LocalStarlightIntensity
        {
            get => _localStarlightIntensity;
            set
            {
                if (SetProperty(ref _localStarlightIntensity, Math.Clamp(value, 0.0, 1.0)))
                {
                    UpdateCurrentLocalStar();
                }
            }
        }

        public ICommand CreateLocalStarCommand => new RelayCommand(() =>
        {
            _currentLocalStar = CreateLocalStarFromProperties();

            if (EnableLocalStar)
            {
                GlobeViewer.ShowLocalStar(_currentLocalStar);
            }
        });

        private const double GlobeRadius = 1.0;
        private const double MinimumStarGap = 0.15;

        private LocalStar CreateLocalStarFromProperties()
        {
            LocalStar star = new();
            CopyPropertiesToLocalStar(star);
            return star;
        }

        private void UpdateCurrentLocalStar()
        {
            if (_currentLocalStar is null)
                return;

            LocalStarType previousType = _currentLocalStar.StarImageType;

            CopyPropertiesToLocalStar(_currentLocalStar);

            // Changing the type may require building a different set
            // of scene objects. Otherwise, update the existing objects.
            if (previousType != _currentLocalStar.StarImageType)
            {
                GlobeViewer.ShowLocalStar(_currentLocalStar);
            }
            else
            {
                GlobeViewer.UpdateLocalStar(_currentLocalStar);
            }
        }

        private const double LocalStarPositionScale = 6.0;

        private void CopyPropertiesToLocalStar(LocalStar star)
        {
            star.StarImageType = SelectedLocalStarType;

            star.Radius = Math.Clamp(LocalStarSize, 0.01, 1.0);

            double distanceFromCenter =
                GlobeRadius +
                star.Radius +
                MinimumStarGap +
                Math.Clamp(LocalStarDistance, 0.0, 1.0)
                    * LocalStarPositionScale;

            star.Center = new Point3D(
                LocalStarOrbit * LocalStarPositionScale,
                LocalStarPlane * LocalStarPositionScale,
                distanceFromCenter);

            star.StarColor = StarlightColor;

            star.LightIntensity = Math.Clamp(LocalStarlightIntensity, 0.0, 1.0);

            star.RotationRate = LocalStarRotationRate;
            star.RotationDirection = LocalStarRotationDirection;
            star.JetPulseRate = LocalStarJetPulseRate;
        }


        #region Starlight Color

        private Color _starlightColor = Colors.Yellow;

        public Color StarlightColor
        {
            get => _starlightColor;
            set
            {
                if (SetProperty(ref _starlightColor, value))
                {
                    SolidColorBrush brush = new(value);
                    brush.Freeze();

                    StarlightColorBrush = brush;

                    UpdateCurrentLocalStar();
                }
            }
        }

        private Brush _starlightColorBrush = new SolidColorBrush(Colors.Yellow);

        public Brush StarlightColorBrush
        {
            get => _starlightColorBrush;
            private set => SetProperty(ref _starlightColorBrush, value);
        }

        private double _localStarJetPulseRate = 0.325;

        public double LocalStarJetPulseRate
        {
            get => _localStarJetPulseRate;
            set
            {
                value = double.IsFinite(value)
                    ? Math.Clamp(value, 0.0, 2.0)
                    : 0.325;

                if (SetProperty(ref _localStarJetPulseRate, value))
                {
                    UpdateCurrentLocalStar();
                }
            }
        }

        #endregion

        #endregion

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

        private void LoadCustomCloudTexture()
        {
            if (!string.IsNullOrWhiteSpace(_customCloudTexturePath) && File.Exists(_customCloudTexturePath))
            {
                GlobeViewer.LoadCloudTexture(_customCloudTexturePath);
            }
        }

        public ICommand SelectCloudTextureCommand => new RelayCommand(() =>
        {
            string cloudsDirectory = Path.Combine(ApplicationPreferences.Current.RootAssetDirectory, "Textures", "Clouds");

            OpenFileDialog dialog = new()
            {
                Title = "Select Cloud Layer Image File",
                Filter = UserInterfaceUtilities.GetCommonImageFilter(),
                Multiselect = false,
                InitialDirectory = cloudsDirectory,
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                _customCloudTexturePath = dialog.FileName;
                UseCustomCloudTexture = true;

                string? directory = Path.GetDirectoryName(dialog.FileName);

                if (!string.IsNullOrEmpty(directory))
                {
                    Settings.Default.LastFileDirectory = directory;
                }
            }
            catch (Exception ex)
            {
                RealmStudioXLogger.Exception("Error loading cloud texture", ex);
                MessageDialog dlg = MessageDialogFactory.ErrorDialog("Cloud Texture", $"Unable to load the cloud texture.\n\n{ex.Message}");
                dlg.ShowDialog();
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
            string planetTextureDirectory = Path.Combine(ApplicationPreferences.Current.RootAssetDirectory, "Textures", "Planet");

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
                planetTextureFileName = dialog.FileName;

                SKBitmap texture = SKBitmap.Decode(planetTextureFileName);

                _globeViewer.InitializeWorldGlobeScene(texture);
            }
        });

        public ICommand ClearTextureCommand => new RelayCommand(() =>
        {
            _modelViewer.ClearModel();
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

}