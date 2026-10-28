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
using System.Windows.Media.Imaging;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

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

        private BlackHoleViewModel _blackHoleModel;
        public BlackHoleViewModel BlackHoleModel => _blackHoleModel;

        public WorldGlobeSceneViewModel(MainWindowViewModel mainViewModel, WorldGlobeViewer globeViewer)
        {
            _mainViewModel = mainViewModel;
            _globeViewer = globeViewer;
            _modelViewer = globeViewer.ModelViewer;

            _worldGlobeViewModel = new (_mainViewModel, _globeViewer);
            _atmosphereViewModel = new(_mainViewModel, _globeViewer);

            _blackHoleModel = new(this);

            // create the view models for sun types before the local star view model,
            // then pass the sun type view models into the local star view model

            _localStarViewModel = new(_globeViewer, _blackHoleModel);
        }

        private string starfieldFileName = string.Empty;
        private string planetTextureFileName = string.Empty;

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