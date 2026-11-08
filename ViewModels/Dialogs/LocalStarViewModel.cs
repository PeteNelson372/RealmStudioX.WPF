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

        public IReadOnlyList<OrbitalBodyType> LocalStarTypes { get; } = Enum.GetValues<OrbitalBodyType>();

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

        private OrbitalBodyType _selectedLocalStarType = OrbitalBodyType.Sun;

        public OrbitalBodyType SelectedLocalStarType
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

            star.AxialTilt = SelectedLocalStarType == OrbitalBodyType.GasGiant ? _gasGiantModel.AxialTilt : 0.0;

            star.TextureSource = new CelestialTextureSource();

            if (SelectedLocalStarType != OrbitalBodyType.GasGiant)
            {
                for (int i = star.Attachments.Count - 1; i >= 0; i--)
                {
                    if (star.Attachments[i].Type ==
                        CelestialBodyAttachmentType.PlanetaryRings)
                    {
                        star.Attachments.RemoveAt(i);
                    }
                }
            }

            switch (SelectedLocalStarType)
            {
                case OrbitalBodyType.Sun:
                    star.RendererType = CelestialBodyRendererType.Sun;

                    CelestialBodyAnimation rotation =
                        GetOrCreateAnimation(
                            star,
                            CelestialBodyAnimationType.BasicRotation);

                    rotation.Enabled = true;
                    rotation.Settings = new BasicRotationSettings
                    {
                        RotationRate = LocalStarRotationRate,
                        RotationDirection = LocalStarRotationDirection
                    };

                    CelestialBodyAnimation solarActivity =
                        GetOrCreateAnimation(
                            star,
                            CelestialBodyAnimationType.SolarActivity);

                    solarActivity.Enabled = true;
                    solarActivity.Settings = new SunAnimationSettings
                    {
                        EnableProminences = _sunModel.EnableProminences,
                        EnableFlares = _sunModel.EnableFlares,
                        ActivityIntensity = _sunModel.ActivityIntensity,
                        ActivityFrequency = _sunModel.ActivityFrequency,
                        ActivityGeometryScale = _sunModel.ActivityGeometryScale,
                        ActivityThicknessScale = _sunModel.ActivityThicknessScale,
                        EnableCorona = _sunModel.EnableCorona,
                        CoronaOpacity = _sunModel.CoronaOpacity,
                        CoronaRotationSpeed = _sunModel.CoronaRotationSpeed
                    };

                    RemoveUnusedAnimations(
                        star,
                        CelestialBodyAnimationType.BasicRotation,
                        CelestialBodyAnimationType.SolarActivity);

                    break;

                case OrbitalBodyType.Corona:
                    star.RendererType = CelestialBodyRendererType.Corona;

                    CelestialBodyAnimation coronaActivity =
                        GetOrCreateAnimation(
                            star,
                            CelestialBodyAnimationType.SolarActivity);

                    coronaActivity.Enabled = true;
                    coronaActivity.Settings = new CoronaAnimationSettings
                    {
                        EnableProminences = _coronaModel.EnableProminences,
                        EnableFlares = _coronaModel.EnableFlares,
                        ActivityIntensity = _coronaModel.ActivityIntensity,
                        ActivityFrequency = _coronaModel.ActivityFrequency,
                        ActivityGeometryScale = _coronaModel.ActivityGeometryScale,
                        ActivityThicknessScale = _coronaModel.ActivityThicknessScale
                    };

                    RemoveUnusedAnimations(
                        star,
                        CelestialBodyAnimationType.SolarActivity);

                    break;

                case OrbitalBodyType.BlackHole:
                    star.RendererType = CelestialBodyRendererType.BlackHole;

                    CelestialBodyAnimation blackHoleRotation =
                        GetOrCreateAnimation(
                            star,
                            CelestialBodyAnimationType.BasicRotation);

                    blackHoleRotation.Enabled = true;
                    blackHoleRotation.Settings = new BasicRotationSettings
                    {
                        RotationRate = LocalStarRotationRate,
                        RotationDirection = LocalStarRotationDirection
                    };

                    CelestialBodyAnimation blackHoleActivity =
                        GetOrCreateAnimation(
                            star,
                            CelestialBodyAnimationType.BlackHoleActivity);

                    blackHoleActivity.Enabled = true;
                    blackHoleActivity.Settings = new BlackHoleAnimationSettings
                    {
                        EnableJets = _blackHoleModel.EnableJets,
                        JetLength = _blackHoleModel.JetLength,
                        JetPulseRate = _blackHoleModel.JetPulseRate,
                        EnableJetParticles = _blackHoleModel.EnableJetParticles,
                        JetParticleSpeed = _blackHoleModel.JetParticleSpeed,
                        JetParticleEmissionRate = _blackHoleModel.JetParticleEmissionRate,
                        JetParticleLifetime = _blackHoleModel.JetParticleLifetime,
                        JetParticleDispersion = _blackHoleModel.JetParticleDispersion,
                        EnableDiskParticles = _blackHoleModel.EnableDiskParticles,
                        DiskParticleEmissionRate = _blackHoleModel.DiskParticleEmissionRate,
                        DiskParticleLifetime = _blackHoleModel.DiskParticleLifetime,
                        DiskParticleOutflowSpeed = _blackHoleModel.DiskParticleOutflowSpeed
                    };

                    RemoveUnusedAnimations(
                        star,
                        CelestialBodyAnimationType.BasicRotation,
                        CelestialBodyAnimationType.BlackHoleActivity);

                    break;

                case OrbitalBodyType.Nebula:
                    star.RendererType = CelestialBodyRendererType.Nebula;

                    star.Animations.Add(new CelestialBodyAnimation
                    {
                        AnimationType = CelestialBodyAnimationType.NebulaActivity,
                        Enabled = true,
                        Settings = new NebulaAnimationSettings
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
                        }
                    });
                    break;


                case OrbitalBodyType.GasGiant:
                    star.RendererType = CelestialBodyRendererType.Planet;

                    star.TextureSource = new CelestialTextureSource
                    {
                        SourceType = CelestialTextureSourceType.BitmapFile,
                        FilePath = _gasGiantModel.TexturePath
                    };

                    CelestialBodyAnimation gasGiantRotation =
                        GetOrCreateAnimation(
                            star,
                            CelestialBodyAnimationType.BasicRotation);

                    gasGiantRotation.Enabled = true;
                    gasGiantRotation.Settings = new BasicRotationSettings
                    {
                        RotationRate = LocalStarRotationRate,
                        RotationDirection = LocalStarRotationDirection
                    };

                    CelestialBodyAnimation atmosphereActivity =
                        GetOrCreateAnimation(
                            star,
                            CelestialBodyAnimationType.AtmosphereTurbulenceActivity);

                    atmosphereActivity.Enabled = true;
                    atmosphereActivity.Settings = new GasGiantAnimationSettings
                    {
                        EnableAtmosphericTurbulence =
                            _gasGiantModel.EnableAtmosphericTurbulence,

                        TurbulenceIntensity =
                            _gasGiantModel.TurbulenceIntensity
                    };

                    RemoveUnusedAnimations(
                        star,
                        CelestialBodyAnimationType.BasicRotation,
                        CelestialBodyAnimationType.AtmosphereTurbulenceActivity);

                    UpdateGasGiantRingAttachment(star);

                    break;


                default:
                    throw new NotSupportedException(
                        $"Local Star type '{SelectedLocalStarType}' is not supported.");
            }
        }


        private void UpdateGasGiantRingAttachment(CelestialBody star)
        {
            PlanetaryRingViewModel rings = _gasGiantModel.Rings;

            CelestialBodyAttachment? attachment =
                star.Attachments.FirstOrDefault(
                    item => item.Type == CelestialBodyAttachmentType.PlanetaryRings);

            if (attachment is null)
            {
                attachment = new CelestialBodyAttachment
                {
                    Name = "Planetary Rings",
                    Description = "A textured ring system surrounding the gas giant.",
                    Type = CelestialBodyAttachmentType.PlanetaryRings
                };

                star.Attachments.Add(attachment);
            }

            attachment.Enabled = rings.Enabled;

            // The attachment owns its own settings instance.
            attachment.AttachmentSettings = rings.Settings.Clone();

            SyncAttachmentAnimation(
                attachment,
                rings.RotationAnimation);

            SyncAttachmentAnimation(
                attachment,
                rings.TurbulenceAnimation);
        }

        private static void SyncAttachmentAnimation(
            CelestialBodyAttachment attachment,
            CelestialBodyAnimation animation)
        {
            CelestialBodyAnimation? existing =
                attachment.Animations.FirstOrDefault(
                    item => item.AnimationType == animation.AnimationType);

            if (existing is null)
            {
                attachment.Animations.Add(animation);
            }
            else if (!ReferenceEquals(existing, animation))
            {
                int index = attachment.Animations.IndexOf(existing);
                attachment.Animations[index] = animation;
            }
        }


        private static CelestialBodyAnimation GetOrCreateAnimation(
            CelestialBody body,
            CelestialBodyAnimationType animationType)
        {
            CelestialBodyAnimation? animation =
                body.Animations.FirstOrDefault(
                    a => a.AnimationType == animationType);

            if (animation is not null)
                return animation;

            animation = new CelestialBodyAnimation
            {
                AnimationType = animationType,
                Enabled = true
            };

            body.Animations.Add(animation);

            return animation;
        }

        private static void RemoveUnusedAnimations(
            CelestialBody body,
            params CelestialBodyAnimationType[] retainedTypes)
        {
            HashSet<CelestialBodyAnimationType> retained = [.. retainedTypes];

            for (int i = body.Animations.Count - 1; i >= 0; i--)
            {
                if (!retained.Contains(body.Animations[i].AnimationType))
                    body.Animations.RemoveAt(i);
            }
        }
    }
}
