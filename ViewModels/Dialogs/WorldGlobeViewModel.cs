using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Celestial;
using RealmStudioX.WPF.Views.Dialogs;

namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    public class WorldGlobeViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainViewModel;
        private readonly WorldGlobeViewer _globeViewer;
        private readonly PlanetaryRingSettings _ringSettings = new();

        public PlanetaryRingViewModel Rings { get; }

        private readonly CelestialBodyAnimation _ringRotationAnimation = new()
        {
            AnimationType = CelestialBodyAnimationType.BasicRotation,
            Enabled = true,
            Settings = new BasicRotationSettings()
        };

        private readonly CelestialBodyAnimation _ringTurbulenceAnimation = new()
        {
            AnimationType = CelestialBodyAnimationType.RingTurbulenceActivity,
            Enabled = false,
            Settings = new RingTurbulenceSettings()
        };

        public WorldGlobeViewModel(MainWindowViewModel mainViewModel, WorldGlobeViewer globeViewer)
        {
            _mainViewModel = mainViewModel;
            _globeViewer = globeViewer;

            Rings = new PlanetaryRingViewModel(
                _ringSettings,
                _ringRotationAnimation,
                _ringTurbulenceAnimation,
                UpdateRingSettings);

            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(CurrentTiltAngle))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"CurrentTiltAngle PropertyChanged: {CurrentTiltAngle:F1}");
                }
            };
        }

        private void UpdateRingSettings()
        {
            _globeViewer.UpdateWorldGlobeRings(_ringSettings, _ringRotationAnimation, _ringTurbulenceAnimation);
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

            System.Diagnostics.Debug.WriteLine($"CurrentTiltAngle UpdateCurrentTiltAngle: {CurrentTiltAngle:F1}");
        }

        private WorldRotationDirection _rotationDirection = WorldRotationDirection.Prograde;

        public WorldRotationDirection RotationDirection
        {
            get => _rotationDirection;
            set => SetProperty(ref _rotationDirection, value);
        }
    }
}
