using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Dialogs;
using RealmStudioX.WPF.WorldGlobe;

namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    public class WorldGlobeViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainViewModel;
        private readonly WorldGlobeViewer _globeViewer;
        private readonly PlanetaryRingSettings _ringSettings = new();

        public PlanetaryRingViewModel Rings { get; }

        public WorldGlobeViewModel(MainWindowViewModel mainViewModel, WorldGlobeViewer globeViewer)
        {
            _mainViewModel = mainViewModel;
            _globeViewer = globeViewer;

            Rings = new PlanetaryRingViewModel(_ringSettings, UpdateRingSettings);
        }

        private void UpdateRingSettings()
        {
            _globeViewer.UpdateWorldGlobeRings(_ringSettings);
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
                _globeViewer.UpdateGlobeTilt(_worldTiltAngle);

                double actualTilt = _globeViewer.GetCurrentAxialTilt();

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
    }
}
