using RealmStudioShapeRenderingLib;
using RealmStudioX._3D.Views.Controls;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.Views.Dialogs;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    public class HeightMapDXViewModel : ViewModelBase
    {
        private readonly ModelViewer3DXControl _modelViewer;
        private readonly RealmStudioMap _map;

        public HeightMapDXViewModel(HeightMapDXModelViewer modelViewer, RealmStudioMap map)
        {
            _modelViewer = modelViewer.ModelViewer;
            _map = map;

            SetDefaultLightingValues();

            _modelViewer.SetAmbientLightIntensity(_ambientLightIntensity);
            _modelViewer.SetKeyLightIntensity(_keyLightIntensity);
            _modelViewer.SetFillLightIntensity(_fillLightIntensity);
        }

        private void SetDefaultLightingValues()
        {
            _ambientLightIntensity = 0.40f;
            _keyLightIntensity = 0.55f;
            _fillLightIntensity = 0.40f;
        }

        public ICommand FitModelCommand => new RelayCommand(() =>
        {
            if (_modelViewer.ModelBounds != null)
            {
                _modelViewer.FitModel(_modelViewer.ModelBounds);
            }
        });

        public ICommand ResetCameraCommand => new RelayCommand(() =>
        {
            ResetModelViewerDefaults();
            _modelViewer.ResetCamera();
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

        public ICommand Snapshot3DSceneCommand => new RelayCommand(() =>
        {
            BitmapSource? bitmap = _modelViewer.CreateSnapshot(_map.MapWidth, _map.MapHeight);

            if (bitmap != null)
            {
                SaveSnapshot(bitmap);
            }
        });

        private static void SaveSnapshot(BitmapSource bitmap)
        {
            SaveFileDialog dialog = new()
            {
                Title = "Save 3D Heightmap Snapshot",
                Filter =
                    "PNG Image (*.png)|*.png|" +
                    "JPEG Image (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                    "BMP Image (*.bmp)|*.bmp",
                DefaultExt = ".png",
                AddExtension = true
            };

            if (dialog.ShowDialog() != true)
                return;

            BitmapEncoder encoder;

            switch (Path.GetExtension(dialog.FileName).ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg":
                    encoder = new JpegBitmapEncoder
                    {
                        QualityLevel = 95
                    };
                    break;

                case ".bmp":
                    encoder = new BmpBitmapEncoder();
                    break;

                default:
                    encoder = new PngBitmapEncoder();
                    break;
            }

            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using FileStream stream = File.Create(dialog.FileName);

            encoder.Save(stream);
        }

        private float _ambientLightIntensity = 0.4f;
        public float AmbientLightIntensity
        {
            get => _ambientLightIntensity;
            set
            {
                if (_ambientLightIntensity != value)
                {
                    _ambientLightIntensity = value;
                    _modelViewer.SetAmbientLightIntensity(_ambientLightIntensity);
                    OnPropertyChanged();
                }
            }
        }

        private float _keyLightIntensity = 0.55f;
        public float KeyLightIntensity
        {
            get => _keyLightIntensity;
            set
            {
                if (_keyLightIntensity != value)
                {
                    _keyLightIntensity = value;
                    _modelViewer.SetKeyLightIntensity(_keyLightIntensity);
                    OnPropertyChanged();
                }
            }
        }

        private float _fillLightIntensity = 0.4f;
        public float FillLightIntensity
        {
            get => _fillLightIntensity;
            set
            {
                if (_fillLightIntensity != value)
                {
                    _fillLightIntensity = value;
                    _modelViewer.SetFillLightIntensity(_fillLightIntensity);
                    OnPropertyChanged();
                }
            }
        }

        private void ResetModelViewerDefaults()
        {
            ShowViewCube = false;
            ShowCoordinateSystem = false;
        }

    }
}
