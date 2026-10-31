using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.Views.Dialogs;
using RealmStudioX.WPF.WorldGlobe;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    public class LocalStarViewModel(WorldGlobeViewer globeViewer,
        BlackHoleViewModel blackHoleModel,
        NebulaViewModel nebulaModel,
        CoronaViewModel coronaModel,
        SunViewModel sunModel,
        GasGiantViewModel gasGiantModel) : ViewModelBase
    {
        private WorldGlobeViewer _globeViewer = globeViewer;

        private BlackHoleViewModel _blackHoleModel = blackHoleModel;

        private NebulaViewModel _nebulaModel = nebulaModel;

        private CoronaViewModel _coronaModel = coronaModel;

        private SunViewModel _sunModel = sunModel;

        private GasGiantViewModel _gasGiantModel = gasGiantModel;

        public IReadOnlyList<LocalStarType> LocalStarTypes { get; } = Enum.GetValues<LocalStarType>();

        private LocalStar? _currentLocalStar;

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
                        _globeViewer.ShowLocalStar(_currentLocalStar);
                    }
                    else
                    {
                        _globeViewer.RemoveLocalStar();
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

        private WorldRotationDirection _localStarRotationDirection = WorldRotationDirection.Prograde;

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
                _globeViewer.ShowLocalStar(_currentLocalStar);
            }
        });

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

        private const double GlobeRadius = 1.0;
        private const double MinimumStarGap = 0.15;

        private LocalStar CreateLocalStarFromProperties()
        {
            LocalStar star = new();
            CopyPropertiesToLocalStar(star);
            return star;
        }

        public void UpdateCurrentLocalStar()
        {
            if (_currentLocalStar is null)
                return;

            LocalStarType previousType = _currentLocalStar.StarImageType;

            CopyPropertiesToLocalStar(_currentLocalStar);

            // Changing the type may require building a different set
            // of scene objects. Otherwise, update the existing objects.
            if (previousType != _currentLocalStar.StarImageType)
            {
                _globeViewer.ShowLocalStar(_currentLocalStar);
            }
            else
            {
                _globeViewer.UpdateLocalStar(_currentLocalStar);
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

            if (star.StarImageType == LocalStarType.BlackHole)
            {
                BlackHoleSettings detailSettings = new()
                {
                    EnableJets = _blackHoleModel.EnableJets,
                    JetLength = _blackHoleModel.JetLength,
                    EnableJetParticles = _blackHoleModel.EnableJetParticles,

                    JetParticleSpeed = _blackHoleModel.JetParticleSpeed,
                    JetParticleLifetime = _blackHoleModel.JetParticleLifetime,
                    JetParticleEmissionRate = _blackHoleModel.JetParticleEmissionRate,
                    JetPulseRate = _blackHoleModel.JetPulseRate,
                    JetParticleDispersion = _blackHoleModel.JetParticleDispersion,

                    EnableDiskParticles = _blackHoleModel.EnableDiskParticles,
                    DiskParticleOutflowSpeed = _blackHoleModel.DiskParticleOutflowSpeed,
                    DiskParticleLifetime = _blackHoleModel.DiskParticleLifetime,
                    DiskParticleEmissionRate = _blackHoleModel.DiskParticleEmissionRate
                };

                star.LocalStarDetailSettings = detailSettings;
            }
            else if (star.StarImageType == LocalStarType.Nebula)
            {
                star.LocalStarDetailSettings = new NebulaSettings
                {
                    LayerCount = _nebulaModel.LayerCount,
                    Width = _nebulaModel.Width,
                    Height = _nebulaModel.Height,
                    Depth = _nebulaModel.Depth,
                    Opacity = _nebulaModel.Opacity,
                    Brightness = _nebulaModel.Brightness,
                    DetailIntensity = _nebulaModel.DetailIntensity,
                    RotationSpeed = _nebulaModel.RotationSpeed,
                    DriftSpeed = _nebulaModel.DriftSpeed
                };

            }
            else if (star.StarImageType == LocalStarType.Corona)
            {
                star.LocalStarDetailSettings = new CoronaSettings
                {
                    ActivityIntensity = _coronaModel.ActivityIntensity,
                    EnableFlares = _coronaModel.EnableFlares,
                    EnableProminences = _coronaModel.EnableProminences,
                    ActivityGeometryScale = _coronaModel.ActivityGeometryScale,
                    ActivityThicknessScale = _coronaModel.ActivityThicknessScale,
                };
            }
            else if (star.StarImageType == LocalStarType.Sun)
            {
                star.LocalStarDetailSettings = new SunSettings
                {
                    EnableCorona = _sunModel.EnableCorona,
                    CoronaOpacity = _sunModel.CoronaOpacity,
                    CoronaRotationSpeed = _sunModel.CoronaRotationSpeed,

                    EnableProminences = _sunModel.EnableProminences,
                    EnableFlares = _sunModel.EnableFlares,
                    ActivityIntensity = _sunModel.ActivityIntensity,
                    ActivityFrequency = _sunModel.ActivityFrequency,
                    ActivityGeometryScale = _sunModel.ActivityGeometryScale,
                    ActivityThicknessScale = _sunModel.ActivityThicknessScale,
                };
            }
            else if (star.StarImageType == LocalStarType.GasGiant)
            {
                star.LocalStarDetailSettings = new GasGiantSettings
                {
                    TexturePath = _gasGiantModel.TexturePath,
                    EnableAtmosphericTurbulence = _gasGiantModel.EnableAtmosphericTurbulence,
                    TurbulenceIntensity = _gasGiantModel.TurbulenceIntensity,
                    RingSettings = _gasGiantModel.Rings.Settings.Clone()
                };
            }

        }
    }
}
