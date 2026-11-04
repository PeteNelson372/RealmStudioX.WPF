using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.Views.Celestial;
using RealmStudioX.WPF.Views.Dialogs;
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

        private CelestialBody? _currentLocalStar;

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
                        _globeViewer.ShowLocalStar(_currentLocalStar, CalculateWorldPosition());
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
                _globeViewer.ShowLocalStar(_currentLocalStar, CalculateWorldPosition());
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

        private Point3D CalculateWorldPosition()
        {
            double radius = Math.Clamp(LocalStarSize, 0.01, 1.0);

            double distanceFromCenter =
                GlobeRadius +
                radius +
                MinimumStarGap +
                Math.Clamp(LocalStarDistance, 0.0, 1.0)
                    * LocalStarPositionScale;

            return new Point3D(
                -LocalStarOrbit * LocalStarPositionScale,
                -LocalStarPlane * LocalStarPositionScale,
                -distanceFromCenter);
        }

        private const double GlobeRadius = 1.0;
        private const double MinimumStarGap = 0.15;

        private CelestialBody CreateLocalStarFromProperties()
        {
            CelestialBody star = new();
            CopyPropertiesToCelestialBody(star);
            return star;
        }

        public void UpdateCurrentLocalStar()
        {
            if (_currentLocalStar is null)
                return;

            CelestialBodyRendererType previousType = _currentLocalStar.RendererType;

            CopyPropertiesToCelestialBody(_currentLocalStar);

            Point3D worldPosition = CalculateWorldPosition();

            if (previousType != _currentLocalStar.RendererType)
            {
                _globeViewer.ShowLocalStar(_currentLocalStar, worldPosition);
            }
            else
            {
                _globeViewer.UpdateLocalStar(_currentLocalStar, worldPosition);
            }
        }

        private const double LocalStarPositionScale = 6.0;

        private void CopyPropertiesToCelestialBody(CelestialBody star)
        {
            star.Name = SelectedLocalStarType.ToString();
            star.Description = $"Local star of type {SelectedLocalStarType}.";
            star.Radius = Math.Clamp(LocalStarSize, 0.01, 1.0);
            star.BaseColor = StarlightColor;

            star.Animations.Clear();

            switch (SelectedLocalStarType)
            {
                case LocalStarType.Sun:
                    {
                        star.RendererType = CelestialBodyRendererType.Sun;
                        star.Animations.Clear();

                        star.Animations.Add(new CelestialBodyAnimation
                        {
                            AnimationType = CelestialBodyAnimationType.BasicRotation,
                            Enabled = true,
                            Settings = new BasicRotationSettings
                            {
                                RotationRate = LocalStarRotationRate,
                                RotationDirection = LocalStarRotationDirection
                            }
                        });

                        SunViewModel sunViewModel = _sunModel;

                        star.Animations.Add(new CelestialBodyAnimation
                        {
                            AnimationType = CelestialBodyAnimationType.SolarActivity,
                            Enabled = true,
                            Settings = new SunAnimationSettings
                            {
                                EnableProminences = sunViewModel.EnableProminences,
                                EnableFlares = sunViewModel.EnableFlares,
                                EnableCorona = sunViewModel.EnableCorona,
                                CoronaOpacity = sunViewModel.CoronaOpacity,
                                CoronaRotationSpeed = sunViewModel.CoronaRotationSpeed,
                                ActivityIntensity = sunViewModel.ActivityIntensity,
                                ActivityFrequency = sunViewModel.ActivityFrequency,
                                ActivityGeometryScale = sunViewModel.ActivityGeometryScale,
                                ActivityThicknessScale = sunViewModel.ActivityThicknessScale
                            }
                        });

                        break;
                    }

                case LocalStarType.Corona:
                    star.RendererType = CelestialBodyRendererType.Corona;
                    break;

                case LocalStarType.BlackHole:
                    star.RendererType = CelestialBodyRendererType.BlackHole;
                    break;

                case LocalStarType.Nebula:
                    star.RendererType = CelestialBodyRendererType.Nebula;
                    break;

                case LocalStarType.GasGiant:
                    // Register a dedicated GasGiant renderer type before
                    // enabling this case.
                    throw new NotSupportedException(
                        "The Gas Giant celestial-body renderer has not yet been migrated.");

                default:
                    throw new NotSupportedException(
                        $"Local Star type '{SelectedLocalStarType}' is not supported.");
            }
        }
    }
}
