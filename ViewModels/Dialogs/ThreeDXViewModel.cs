using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX._3D.Views.Controls;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows.Input;
using Cursors = System.Windows.Input.Cursors;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    public class ThreeDXViewModel(ThreeDXModelViewer modelViewer) : ViewModelBase
    {
        private ModelViewer3DXControl _modelViewer = modelViewer.ModelViewer;

        public ICommand OpenModelCommand => new RelayCommand(() =>
        {
            LoadModel();

            SetDefaultLightingValues();

            _modelViewer.SetAmbientLightIntensity(_ambientLightIntensity);
            _modelViewer.SetKeyLightIntensity(_keyLightIntensity);
            _modelViewer.SetFillLightIntensity(_fillLightIntensity);
        });

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

        private bool _isPerspectiveView = true;
        public bool IsPerspectiveView
        {
            get => _isPerspectiveView;
            set
            {
                if (_isPerspectiveView != value)
                {
                    _isPerspectiveView = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand PerspectiveViewCommand => new RelayCommand(() =>
        {
            IsPerspectiveView = true;
            IsOrthographicView = false;

            _modelViewer.SetCameraProjection(CameraProjection.Perspective);
        });

        private bool _isOrthographicView = false;
        public bool IsOrthographicView
        {
            get => _isOrthographicView;
            set
            {
                if (_isOrthographicView != value)
                {
                    _isOrthographicView = value;
                    OnPropertyChanged();
                }
            }
        }
        public ICommand OrthographicViewCommand => new RelayCommand(() =>
        {
            IsOrthographicView = true;
            IsPerspectiveView = false;
            _modelViewer.SetCameraProjection(CameraProjection.Orthographic);
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

        private bool _showViewCube = true;

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

        private bool _showCoordinateSystem = true;

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

        private bool _showGrid = false;

        public bool ShowGrid
        {
            get => _showGrid;
            set
            {
                if (_showGrid != value)
                {
                    _showGrid = value;
                    _modelViewer.ShowGrid(_showGrid);
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ShowGridCommand => new RelayCommand(() =>
        {
            ShowGrid = !ShowGrid;
        });

        private bool _showBoundingBox = false;

        public bool ShowBoundingBox
        {
            get => _showBoundingBox;
            set
            {
                if (_modelViewer.IsModelLoaded)
                {
                    if (_showBoundingBox != value)
                    {
                        _showBoundingBox = value;
                        _modelViewer.ShowBoundingBox(_showBoundingBox);
                        OnPropertyChanged();
                    }
                }
                else
                {
                    _showBoundingBox = false;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ShowBoundingBoxCommand => new RelayCommand(() =>
        {
            ShowBoundingBox = !ShowBoundingBox;
        });

        private bool _showWireframe = false;

        public bool ShowWireFrame
        {
            get => _showWireframe;
            set
            {
                if (_modelViewer.IsModelLoaded)
                {
                    if (_showWireframe != value)
                    {
                        _showWireframe = value;
                        _modelViewer.ShowWireframe(_showWireframe);
                        OnPropertyChanged();
                    }
                }
                else
                {
                    _showWireframe = false;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ShowWireFrameCommand => new RelayCommand(() =>
        {
            ShowWireFrame = !ShowWireFrame;
        });

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

        private void LoadModel()
        {
            try
            {
                OpenFileDialog ofd = new()
                {
                    Title = "Open 3D Model",
                    DefaultExt = "obj",
                    Filter =
                        "3D Model files|*.obj;*.stl;*.3ds;*.lwo;*.off|" +
                        "OBJ files (*.obj)|*.obj|" +
                        "STL files (*.stl)|*.stl|" +
                        "3DS files (*.3ds)|*.3ds|" +
                        "LWO files (*.lwo)|*.lwo|" +
                        "OFF files (*.off)|*.off|" +
                        "All files (*.*)|*.*",
                    CheckFileExists = true,
                    RestoreDirectory = true,
                    Multiselect = false
                };

                if (ofd.ShowDialog() == true)
                {
                    if (ofd.FileName != "")
                    {
                        ResetModelViewerDefaults();

                        Mouse.OverrideCursor = Cursors.Wait;
                        _modelViewer.LoadModel(ofd.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                RealmStudioXLogger.Error(ex.Message);
                MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error Loading Model", ex.Message);
                dlg.ShowDialog();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

        }

        private void ResetModelViewerDefaults()
        {
            ShowViewCube = true;
            ShowCoordinateSystem = true;
            ShowWireFrame = false;
            ShowGrid = false;
            ShowBoundingBox = false;
        }

    }
}
