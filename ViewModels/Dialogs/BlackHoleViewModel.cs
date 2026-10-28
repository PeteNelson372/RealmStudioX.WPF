using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Infrastructure;

namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    public class BlackHoleViewModel(WorldGlobeSceneViewModel globeSceneViewModel) : ViewModelBase
    {
        private readonly WorldGlobeSceneViewModel _globeSceneViewModel = globeSceneViewModel
            ?? throw new ArgumentNullException(nameof(globeSceneViewModel));


        private bool _enableJets = true;

        public bool EnableJets
        {
            get => _enableJets;
            set
            {
                if (SetProperty(ref _enableJets, value))
                {
                    UpdateActiveBlackHole();
                }
            }
        }

        private const double MinimumJetLength = 0.1;
        private const double MaximumJetLength = 30.0;
        private const double DefaultJetLength = 8.4;

        private double _jetLength = DefaultJetLength;

        public double JetLength
        {
            get => _jetLength;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, MinimumJetLength, MaximumJetLength) : DefaultJetLength;

                if (SetProperty(ref _jetLength, value))
                {
                    UpdateActiveBlackHole();
                }
            }
        }

        private const double MinimumPulseRate = 0.0;
        private const double MaximumPulseRate = 2.0;
        private const double DefaultPulseRate = 0.325;

        private double _jetPulseRate = DefaultPulseRate;

        public double JetPulseRate
        {
            get => _jetPulseRate;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, MinimumPulseRate, MaximumPulseRate) : DefaultPulseRate;

                if (SetProperty(ref _jetPulseRate, value))
                {
                    UpdateActiveBlackHole();
                }
            }
        }

        private bool _enableJetParticles = true;

        public bool EnableJetParticles
        {
            get => _enableJetParticles;
            set
            {
                if (SetProperty(ref _enableJetParticles, value))
                {
                    UpdateActiveBlackHole();
                }
            }
        }

        private const double MinimumJetParticleSpeed = 0.0;
        private const double MaximumJetParticleSpeed = 20.0;
        private const double DefaultJetParticleSpeed = 2.625;

        private double _jetParticleSpeed = DefaultJetParticleSpeed;

        public double JetParticleSpeed
        {
            get => _jetParticleSpeed;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, MinimumJetParticleSpeed, MaximumJetParticleSpeed) : DefaultJetParticleSpeed;

                if (SetProperty(ref _jetParticleSpeed, value))
                {
                    UpdateActiveBlackHole();
                }
            }
        }

        private const double MinimumJetParticleEmissionRate = 0.0;
        private const double MaximumJetParticleEmissionRate = 60.0;
        private const double DefaultJetParticleEmissionRate = 22.0;

        private double _jetParticleEmissionRate = DefaultJetParticleEmissionRate;

        public double JetParticleEmissionRate
        {
            get => _jetParticleEmissionRate;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, MinimumJetParticleEmissionRate, MaximumJetParticleEmissionRate) : DefaultJetParticleEmissionRate;

                if (SetProperty(ref _jetParticleEmissionRate, value))
                {
                    UpdateActiveBlackHole();
                }
            }
        }

        private const double MinimumJetParticleLifetime = 0.1;
        private const double MaximumJetParticleLifetime = 20.0;
        private const double DefaultJetParticleLifetime = 3.2;

        private double _jetParticleLifetime = DefaultJetParticleLifetime;

        public double JetParticleLifetime
        {
            get => _jetParticleLifetime;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, MinimumJetParticleLifetime, MaximumJetParticleLifetime) : DefaultJetParticleLifetime;

                if (SetProperty(ref _jetParticleLifetime, value))
                {
                    UpdateActiveBlackHole();
                }
            }
        }

        private const double MinimumJetParticleDispersion = 0.0;
        private const double MaximumJetParticleDispersion = 4.0;
        private const double DefaultJetParticleDispersion = 1.0;

        private double _jetParticleDispersion = DefaultJetParticleDispersion;

        public double JetParticleDispersion
        {
            get => _jetParticleDispersion;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, MinimumJetParticleDispersion, MaximumJetParticleDispersion) : DefaultJetParticleDispersion;

                if (SetProperty(ref _jetParticleDispersion, value))
                {
                    UpdateActiveBlackHole();
                }
            }
        }

        private bool _enableDiskParticles = true;

        public bool EnableDiskParticles
        {
            get => _enableDiskParticles;
            set
            {
                if (SetProperty(ref _enableDiskParticles, value))
                {
                    UpdateActiveBlackHole();
                }
            }
        }

        private const double MinimumDiskParticleEmissionRate = 0.0;
        private const double MaximumDiskParticleEmissionRate = 40.0;
        private const double DefaultDiskParticleEmissionRate = 6.0;

        private double _diskParticleEmissionRate = DefaultDiskParticleEmissionRate;

        public double DiskParticleEmissionRate
        {
            get => _diskParticleEmissionRate;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, MinimumDiskParticleEmissionRate, MaximumDiskParticleEmissionRate) : DefaultDiskParticleEmissionRate;

                if (SetProperty(ref _diskParticleEmissionRate, value))
                {
                    UpdateActiveBlackHole();
                }
            }
        }

        private const double MinimumDiskParticleLifetime = 0.1;
        private const double MaximumDiskParticleLifetime = 20.0;
        private const double DefaultDiskParticleLifetime = 2.5;

        private double _diskParticleLifetime = DefaultDiskParticleLifetime;

        public double DiskParticleLifetime
        {
            get => _diskParticleLifetime;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, MinimumDiskParticleLifetime, MaximumDiskParticleLifetime) : DefaultDiskParticleLifetime;

                if (SetProperty(ref _diskParticleLifetime, value))
                {
                    UpdateActiveBlackHole();
                }
            }
        }

        private const double MinimumDiskParticleOutflowSpeed = 0.0;
        private const double MaximumDiskParticleOutflowSpeed = 5.0;
        private const double DefaultDiskParticleOutflowSpeed = 1.0;

        private double _diskParticleOutflowSpeed = DefaultDiskParticleOutflowSpeed;

        public double DiskParticleOutflowSpeed
        {
            get => _diskParticleOutflowSpeed;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, MinimumDiskParticleOutflowSpeed, MaximumDiskParticleOutflowSpeed) : DefaultDiskParticleOutflowSpeed;

                if (SetProperty(ref _diskParticleOutflowSpeed, value))
                {
                    UpdateActiveBlackHole();
                }
            }
        }

        private void UpdateActiveBlackHole()
        {
            if (_globeSceneViewModel.StarViewModel.SelectedLocalStarType == LocalStarType.BlackHole)
            {
                _globeSceneViewModel.StarViewModel.UpdateCurrentLocalStar();
            }
        }
    }
}
