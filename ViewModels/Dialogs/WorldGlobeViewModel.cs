using RealmStudioShapeRenderingLib;
using RealmStudioX._3D.Views.Controls;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Dialogs;
using SkiaSharp;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

        private string starfieldFileName = string.Empty;
        private string planetTextureFileName = string.Empty;

        private bool _enableStarfield = true;
        public bool EnableStarfield
        {
            get => _enableStarfield;
            set => _enableStarfield = value;
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

        private Color _atmosphereColor = Colors.Transparent;
        public Color AtmosphereColor
        {
            get => _atmosphereColor;
            set
            {
                if (SetProperty(ref _atmosphereColor, value))
                {
                    _atmosphereColorBrush.Color = value;
                }
            }
        }

        private SolidColorBrush _atmosphereColorBrush = new(Colors.Transparent);
        public Brush AtmosphereColorBrush => _atmosphereColorBrush;

        private Color _cloudColor = Colors.Transparent;
        public Color CloudColor
        {
            get => _cloudColor;
            set
            {
                if (SetProperty(ref _cloudColor, value))
                {
                    _cloudColorBrush.Color = value;
                }
            }
        }

        private SolidColorBrush _cloudColorBrush = new(Colors.Transparent);
        public Brush CloudColorBrush => _cloudColorBrush;

        public ICommand ToggleStarfield => new RelayCommand(() =>
        {
            if (EnableStarfield && !string.IsNullOrEmpty(starfieldFileName))
            {
                SetBackground(starfieldFileName);
            }
            else
            {
                ModelViewer.SetBackground(null);
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

                _globeViewer.InitializeWorldGlobe(texture);
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