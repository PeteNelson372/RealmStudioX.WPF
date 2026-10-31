
using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Infrastructure;

namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    public class NebulaViewModel(WorldGlobeSceneViewModel globeSceneViewModel) : ViewModelBase
    {
        private readonly WorldGlobeSceneViewModel _globeSceneViewModel = globeSceneViewModel
            ?? throw new ArgumentNullException(nameof(globeSceneViewModel));

        // =============================================================
        // Layer count
        // =============================================================

        private const int MinimumLayerCount = 1;
        private const int MaximumLayerCount = 7;
        private const int DefaultLayerCount = 5;

        private int _layerCount = DefaultLayerCount;

        public int LayerCount
        {
            get => _layerCount;
            set
            {
                value = Math.Clamp(value, MinimumLayerCount, MaximumLayerCount);

                if (SetProperty(ref _layerCount, value))
                {
                    UpdateActiveNebula();
                }
            }
        }

        // =============================================================
        // Width
        // =============================================================

        private const double MinimumWidth = 1.0;
        private const double MaximumWidth = 60.0;
        private const double DefaultWidth = 18.0;

        private double _width = DefaultWidth;

        public double Width
        {
            get => _width;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, MinimumWidth, MaximumWidth) : DefaultWidth;

                if (SetProperty(ref _width, value))
                {
                    UpdateActiveNebula();
                }
            }
        }

        // =============================================================
        // Height
        // =============================================================

        private const double MinimumHeight = 1.0;
        private const double MaximumHeight = 60.0;
        private const double DefaultHeight = 12.0;

        private double _height = DefaultHeight;

        public double Height
        {
            get => _height;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, MinimumHeight, MaximumHeight) : DefaultHeight;

                if (SetProperty(ref _height, value))
                {
                    UpdateActiveNebula();
                }
            }
        }

        // =============================================================
        // Depth
        // =============================================================

        private const double MinimumDepth = 0.0;
        private const double MaximumDepth = 20.0;
        private const double DefaultDepth = 5.0;

        private double _depth = DefaultDepth;

        public double Depth
        {
            get => _depth;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, MinimumDepth, MaximumDepth) : DefaultDepth;

                if (SetProperty(ref _depth, value))
                {
                    UpdateActiveNebula();
                }
            }
        }

        // =============================================================
        // Opacity
        // =============================================================

        private const double MinimumOpacity = 0.0;
        private const double MaximumOpacity = 1.0;
        private const double DefaultOpacity = 0.9;

        private double _opacity = DefaultOpacity;

        public double Opacity
        {
            get => _opacity;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, MinimumOpacity, MaximumOpacity) : DefaultOpacity;

                if (SetProperty(ref _opacity, value))
                {
                    UpdateActiveNebula();
                }
            }
        }

        // =============================================================
        // Brightness
        // =============================================================

        private const double MinimumBrightness = 0.0;
        private const double MaximumBrightness = 3.0;
        private const double DefaultBrightness = 1.0;

        private double _brightness = DefaultBrightness;

        public double Brightness
        {
            get => _brightness;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, MinimumBrightness, MaximumBrightness) : DefaultBrightness;

                if (SetProperty(ref _brightness, value))
                {
                    UpdateActiveNebula();
                }
            }
        }

        // =============================================================
        // Detail intensity
        // =============================================================

        private const double MinimumDetailIntensity = 0.0;
        private const double MaximumDetailIntensity = 2.0;
        private const double DefaultDetailIntensity = 1.0;

        private double _detailIntensity = DefaultDetailIntensity;

        public double DetailIntensity
        {
            get => _detailIntensity;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, MinimumDetailIntensity, MaximumDetailIntensity) : DefaultDetailIntensity;

                if (SetProperty(ref _detailIntensity, value))
                {
                    UpdateActiveNebula();
                }
            }
        }

        // =============================================================
        // Drift speed
        // =============================================================

        private const double MinimumDriftSpeed = 0.0;
        private const double MaximumDriftSpeed = 0.5;
        private const double DefaultDriftSpeed = 0.25;

        private double _driftSpeed = DefaultDriftSpeed;

        public double DriftSpeed
        {
            get => _driftSpeed;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, MinimumDriftSpeed, MaximumDriftSpeed) : DefaultDriftSpeed;

                if (SetProperty(ref _driftSpeed, value))
                {
                    UpdateActiveNebula();
                }
            }
        }

        // =============================================================
        // Rotation speed
        // =============================================================

        private const double MinimumRotationSpeed = -2.0;
        private const double MaximumRotationSpeed = 2.0;
        private const double DefaultRotationSpeed = 1.2;

        private double _rotationSpeed = DefaultRotationSpeed;

        public double RotationSpeed
        {
            get => _rotationSpeed;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, MinimumRotationSpeed, MaximumRotationSpeed) : DefaultRotationSpeed;

                if (SetProperty(ref _rotationSpeed, value))
                {
                    UpdateActiveNebula();
                }
            }
        }

        // =============================================================
        // Update the active scene object
        // =============================================================

        private void UpdateActiveNebula()
        {
            if (_globeSceneViewModel.StarViewModel.SelectedLocalStarType == LocalStarType.Nebula)
            {
                _globeSceneViewModel.StarViewModel.UpdateCurrentLocalStar();
            }
        }
    }
}
