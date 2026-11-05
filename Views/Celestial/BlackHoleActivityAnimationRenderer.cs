
using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.WorldGlobe;
using RealmStudioX.WPF.WorldGlobe.Particles;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Celestial;

internal sealed class BlackHoleActivityAnimationRenderer :
    ICelestialBodyAnimationRenderer
{
    private const int JetParticlePoolSize = 110;
    private const int DiskParticlePoolSize = 30;

    private const double DefaultJetLength = 8.4;
    private const double DefaultJetPulseRate = 0.325;

    private readonly Model3DGroup _root = new();
    private readonly Model3DGroup _jetSurfaceGroup = new();

    private readonly ImageBrush _jetOuterBrush;
    private readonly ImageBrush _jetCoreBrush;

    private readonly GeometryModel3D _northOuterJet;
    private readonly GeometryModel3D _northCoreJet;
    private readonly GeometryModel3D _southOuterJet;
    private readonly GeometryModel3D _southCoreJet;

    private readonly JetParticleBehavior _northJetBehavior;
    private readonly JetParticleBehavior _southJetBehavior;

    private readonly DeterministicParticleSystem3D _northJetParticles;
    private readonly DeterministicParticleSystem3D _southJetParticles;

    private readonly AccretionDiskParticleBehavior _diskParticleBehavior;
    private readonly DeterministicParticleSystem3D _diskParticles;

    private ICelestialBodyVisualTarget? _target;

    private double _configuredJetLength = DefaultJetLength;
    private double _jetPulseRate = DefaultJetPulseRate;

    private bool _enabled;
    private bool _enableJets;
    private bool _enableJetParticles;
    private bool _enableDiskParticles;

    public CelestialBodyAnimationType AnimationType =>
        CelestialBodyAnimationType.BlackHoleActivity;

    public BlackHoleActivityAnimationRenderer()
    {
        _jetOuterBrush = new ImageBrush(CreateJetTexture(core: false))
        {
            Stretch = Stretch.Fill,
            TileMode = TileMode.None,
            Opacity = 0.69
        };

        _jetCoreBrush = new ImageBrush(CreateJetTexture(core: true))
        {
            Stretch = Stretch.Fill,
            TileMode = TileMode.None,
            Opacity = 0.82
        };

        Material outerMaterial = CreateEmissiveMaterial(_jetOuterBrush);
        Material coreMaterial = CreateEmissiveMaterial(_jetCoreBrush);

        _northOuterJet = CreateJetModel(
            north: true, 1.0, outerMaterial);

        _northCoreJet = CreateJetModel(
            north: true, 0.30, coreMaterial);

        _southOuterJet = CreateJetModel(
            north: false, 1.0, outerMaterial);

        _southCoreJet = CreateJetModel(
            north: false, 0.30, coreMaterial);

        _jetSurfaceGroup.Children.Add(_northOuterJet);
        _jetSurfaceGroup.Children.Add(_northCoreJet);
        _jetSurfaceGroup.Children.Add(_southOuterJet);
        _jetSurfaceGroup.Children.Add(_southCoreJet);

        _root.Children.Add(_jetSurfaceGroup);

        _northJetBehavior = new JetParticleBehavior(
            north: true,
            jetLength: DefaultJetLength,
            speed: 2.625,
            dispersion: 1.0);

        _southJetBehavior = new JetParticleBehavior(
            north: false,
            jetLength: DefaultJetLength,
            speed: 2.625,
            dispersion: 1.0);

        _northJetParticles = new DeterministicParticleSystem3D(
            CreateJetEmitterSettings(22.0, 3.2, 1101),
            _northJetBehavior);

        _southJetParticles = new DeterministicParticleSystem3D(
            CreateJetEmitterSettings(22.0, 3.2, 2202),
            _southJetBehavior);

        _root.Children.Add(_northJetParticles.Root);
        _root.Children.Add(_southJetParticles.Root);

        _diskParticleBehavior = new AccretionDiskParticleBehavior
        {
            OutflowSpeed = 1.0
        };

        _diskParticles = new DeterministicParticleSystem3D(
            CreateDiskEmitterSettings(6.0, 2.5),
            _diskParticleBehavior);

        _root.Children.Add(_diskParticles.Root);
    }

    public void Update(
        CelestialBodyAnimation animation,
        ICelestialBodyAnimationTarget target)
    {
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(target);

        if (target is not ICelestialBodyVisualTarget visualTarget)
        {
            throw new InvalidOperationException(
                "BlackHoleActivity requires a celestial body visual target.");
        }

        if (_target is not null &&
            !ReferenceEquals(_target, visualTarget))
        {
            _target.BodyVisualRoot.Children.Remove(_root);
        }

        _target = visualTarget;

        if (!_target.BodyVisualRoot.Children.Contains(_root))
        {
            _target.BodyVisualRoot.Children.Add(_root);
        }

        BlackHoleAnimationSettings settings =
            animation.Settings as BlackHoleAnimationSettings
            ?? throw new InvalidOperationException(
                "BlackHoleActivity requires BlackHoleAnimationSettings.");

        _enabled = animation.Enabled;

        ApplySettings(settings);
    }

    private void ApplySettings(BlackHoleAnimationSettings settings)
    {
        _enableJets = _enabled && settings.EnableJets;

        // Jet particles are subordinate to the main jet switch.
        _enableJetParticles =
            _enableJets && settings.EnableJetParticles;

        _enableDiskParticles =
            _enabled && settings.EnableDiskParticles;

        _jetPulseRate = ClampSetting(
            settings.JetPulseRate,
            DefaultJetPulseRate,
            0.0,
            2.0);

        double jetLength = ClampSetting(
            settings.JetLength,
            DefaultJetLength,
            0.1,
            30.0);

        if (Math.Abs(jetLength - _configuredJetLength) > 0.0001)
        {
            _configuredJetLength = jetLength;

            _northOuterJet.Geometry =
                CreateJetMesh(true, 1.0, jetLength);

            _northCoreJet.Geometry =
                CreateJetMesh(true, 0.30, jetLength);

            _southOuterJet.Geometry =
                CreateJetMesh(false, 1.0, jetLength);

            _southCoreJet.Geometry =
                CreateJetMesh(false, 0.30, jetLength);
        }

        double jetSpeed = ClampSetting(
            settings.JetParticleSpeed,
            2.625,
            0.0,
            20.0);

        double jetDispersion = ClampSetting(
            settings.JetParticleDispersion,
            1.0,
            0.0,
            4.0);

        _northJetBehavior.JetLength = jetLength;
        _northJetBehavior.Speed = jetSpeed;
        _northJetBehavior.Dispersion = jetDispersion;

        _southJetBehavior.JetLength = jetLength;
        _southJetBehavior.Speed = jetSpeed;
        _southJetBehavior.Dispersion = jetDispersion;

        _northJetParticles.UpdateSettings(
            CreateJetEmitterSettings(
                settings.JetParticleEmissionRate,
                settings.JetParticleLifetime,
                1101));

        _southJetParticles.UpdateSettings(
            CreateJetEmitterSettings(
                settings.JetParticleEmissionRate,
                settings.JetParticleLifetime,
                2202));

        _diskParticleBehavior.OutflowSpeed = ClampSetting(
            settings.DiskParticleOutflowSpeed,
            1.0,
            0.0,
            5.0);

        _diskParticles.UpdateSettings(
            CreateDiskEmitterSettings(
                settings.DiskParticleEmissionRate,
                settings.DiskParticleLifetime));

        SetChildEnabled(_root, _jetSurfaceGroup, _enableJets);
        SetChildEnabled(
            _root,
            _northJetParticles.Root,
            _enableJetParticles);

        SetChildEnabled(
            _root,
            _southJetParticles.Root,
            _enableJetParticles);

        SetChildEnabled(
            _root,
            _diskParticles.Root,
            _enableDiskParticles);

        if (!_enableJetParticles)
        {
            _northJetParticles.Reset();
            _southJetParticles.Reset();
        }

        if (!_enableDiskParticles)
            _diskParticles.Reset();

        if (!_enableJets)
            ResetJetOpacity();
    }

    public void AdvanceAnimation(WorldGlobeAnimationFrame frame)
    {
        if (!_enabled || frame.DeltaSeconds <= 0.0)
            return;

        double pulse;

        if (_jetPulseRate <= 0.0)
        {
            pulse = 0.5;
        }
        else
        {
            double time = frame.TimeSeconds;
            double baseFrequency = _jetPulseRate;

            // Preserve the legacy slow pulse-rate modulation.
            const double variation1 = 0.12;
            const double variation2 = 0.05;

            const double modulationFrequency1 = 0.071;
            const double modulationFrequency2 = 0.113;

            double phase =
                2.0 * Math.PI * baseFrequency * time;

            phase += baseFrequency * variation1
                / modulationFrequency1
                * Math.Sin(
                    2.0 * Math.PI * modulationFrequency1 * time + 1.1);

            phase += baseFrequency * variation2
                / modulationFrequency2
                * Math.Sin(
                    2.0 * Math.PI * modulationFrequency2 * time + 2.4);

            pulse = 0.5 + 0.5 * Math.Sin(phase);
        }

        if (_enableJets)
        {
            _jetOuterBrush.Opacity = 0.58 + 0.22 * pulse;
            _jetCoreBrush.Opacity = 0.72 + 0.20 * pulse;
        }

        if (_enableJetParticles)
        {
            _northJetParticles.AdvanceTo(frame.TimeSeconds);
            _southJetParticles.AdvanceTo(frame.TimeSeconds);
        }

        if (_enableDiskParticles)
            _diskParticles.AdvanceTo(frame.TimeSeconds);
    }

    public void ResetAnimation()
    {
        ResetJetOpacity();

        _northJetParticles.Reset();
        _southJetParticles.Reset();
        _diskParticles.Reset();
    }

    private void ResetJetOpacity()
    {
        _jetOuterBrush.Opacity = 0.69;
        _jetCoreBrush.Opacity = 0.82;
    }

    private static GeometryModel3D CreateJetModel(
        bool north,
        double radialScale,
        Material material)
    {
        return new GeometryModel3D
        {
            Geometry = CreateJetMesh(
                north,
                radialScale,
                DefaultJetLength),
            Material = material,
            BackMaterial = material
        };
    }

    private static MeshGeometry3D CreateJetMesh(
        bool north,
        double radialScale,
        double jetLength)
    {
        const int axialSegments = 80;
        const int angularSegments = 40;
        const double startY = 0.80;

        MeshGeometry3D mesh = new();
        int stride = angularSegments + 1;

        double phase = north ? 0.0 : 1.1;

        for (int axialIndex = 0;
             axialIndex <= axialSegments;
             axialIndex++)
        {
            double t = axialIndex / (double)axialSegments;

            double distance = startY + t * jetLength;
            double y = north ? distance : -distance;

            double radius = GetJetRadius(t) * radialScale;

            for (int angularIndex = 0;
                 angularIndex <= angularSegments;
                 angularIndex++)
            {
                double angle =
                    2.0 * Math.PI * angularIndex / angularSegments;

                double cos = Math.Cos(angle);
                double sin = Math.Sin(angle);

                double irregularity =
                    1.0
                    + 0.025 * Math.Sin(angle * 3.0 + t * 14.0 + phase)
                    + 0.015 * Math.Sin(angle * 7.0 - t * 23.0);

                double centerX =
                    0.035 * Math.Sin(t * 7.5 + phase) * t;

                double centerZ =
                    0.030 * Math.Sin(t * 9.0 + phase) * t;

                mesh.Positions.Add(new Point3D(
                    centerX + radius * irregularity * cos,
                    y,
                    centerZ + radius * irregularity * sin));

                mesh.TextureCoordinates.Add(new Point(
                    angularIndex / (double)angularSegments,
                    t));

                Vector3D normal = new(cos, 0, sin);
                normal.Normalize();
                mesh.Normals.Add(normal);
            }
        }

        for (int axialIndex = 0;
             axialIndex < axialSegments;
             axialIndex++)
        {
            for (int angularIndex = 0;
                 angularIndex < angularSegments;
                 angularIndex++)
            {
                int current =
                    axialIndex * stride + angularIndex;

                int nextAround = current + 1;
                int nextAxial = current + stride;
                int nextAxialAround = nextAxial + 1;

                mesh.TriangleIndices.Add(current);
                mesh.TriangleIndices.Add(nextAround);
                mesh.TriangleIndices.Add(nextAxial);

                mesh.TriangleIndices.Add(nextAround);
                mesh.TriangleIndices.Add(nextAxialAround);
                mesh.TriangleIndices.Add(nextAxial);
            }
        }

        return mesh;
    }

    private static double GetJetRadius(double t)
    {
        if (t < 0.12)
        {
            double u = SmoothStep(t / 0.12);
            return Lerp(0.035, 0.195, u);
        }

        if (t < 0.30)
        {
            double u = SmoothStep((t - 0.12) / 0.18);
            return Lerp(0.195, 0.165, u);
        }

        double taper = (t - 0.30) / 0.70;
        double curvedTaper = Math.Pow(taper, 0.80);

        return Lerp(0.165, 0.035, curvedTaper);
    }

    private static BitmapSource CreateJetTexture(bool core)
    {
        const int width = 128;
        const int height = 512;

        int stride = width * 4;
        byte[] pixels = new byte[stride * height];

        for (int y = 0; y < height; y++)
        {
            double t = y / (double)(height - 1);

            double fadeIn = SmoothStep(t / 0.08);

            const double fadeOutStart = 0.28;

            double fadeProgress = Math.Clamp(
                (t - fadeOutStart) / (1.0 - fadeOutStart),
                0.0,
                1.0);

            double remainingOpacity = 1.0 - fadeProgress;
            double fadeOut = remainingOpacity * remainingOpacity;

            double axialAlpha = fadeIn * fadeOut;

            double brightness = 0.78 +
                0.22 * Math.Sin(Math.PI * Math.Clamp(t, 0.0, 1.0));

            for (int x = 0; x < width; x++)
            {
                double u = x / (double)(width - 1);
                double angle = u * Math.PI * 2.0;

                double angularVariation = core
                    ? 0.96 + 0.04 * Math.Sin(angle * 3.0 + t * 8.0)
                    : 0.80
                      + 0.12 * Math.Sin(angle * 3.0 + t * 9.0)
                      + 0.08 * Math.Sin(angle * 5.0 - t * 17.0);

                angularVariation = Math.Clamp(
                    angularVariation, 0.55, 1.0);

                byte maximumAlpha = core ? (byte)238 : (byte)205;

                byte alpha = (byte)Math.Clamp(
                    Math.Round(
                        maximumAlpha * axialAlpha * angularVariation),
                    0.0,
                    255.0);

                Color color;

                if (core)
                {
                    color = Color.FromRgb(
                        ScaleByte(255, brightness),
                        ScaleByte(252, brightness),
                        ScaleByte(255, brightness));
                }
                else
                {
                    Color violet = Color.FromRgb(151, 145, 255);
                    Color pale = Color.FromRgb(210, 214, 255);

                    double blend = Math.Clamp(
                        0.25 + 0.55 * brightness, 0.0, 1.0);

                    color = BlendColors(violet, pale, blend);
                }

                int offset = y * stride + x * 4;

                pixels[offset] = ScaleByte(color.B, brightness);
                pixels[offset + 1] = ScaleByte(color.G, brightness);
                pixels[offset + 2] = ScaleByte(color.R, brightness);
                pixels[offset + 3] = alpha;
            }
        }

        BitmapSource bitmap = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            stride);

        if (bitmap.CanFreeze)
            bitmap.Freeze();

        return bitmap;
    }

    private static Material CreateEmissiveMaterial(Brush brush)
    {
        MaterialGroup material = new();
        material.Children.Add(new DiffuseMaterial(brush));
        material.Children.Add(new EmissiveMaterial(brush));
        return material;
    }

    private static ParticleEmitterSettings CreateJetEmitterSettings(
        double emissionRate,
        double lifetime,
        int seed)
    {
        return new ParticleEmitterSettings(
            emissionRate: ClampSetting(emissionRate, 22.0, 0.0, 60.0),
            lifetimeSeconds: ClampSetting(lifetime, 3.2, 0.1, 20.0),
            maxParticles: JetParticlePoolSize,
            seed: seed,
            emissionJitterFraction: 0.30,
            lifetimeVariation: 0.18);
    }

    private static ParticleEmitterSettings CreateDiskEmitterSettings(
        double emissionRate,
        double lifetime)
    {
        return new ParticleEmitterSettings(
            emissionRate: ClampSetting(emissionRate, 6.0, 0.0, 40.0),
            lifetimeSeconds: ClampSetting(lifetime, 2.5, 0.1, 20.0),
            maxParticles: DiskParticlePoolSize,
            seed: 3303,
            emissionJitterFraction: 0.35,
            lifetimeVariation: 0.20);
    }

    private static void SetChildEnabled(
        Model3DGroup parent,
        Model3D child,
        bool enabled)
    {
        bool isPresent = parent.Children.Contains(child);

        if (enabled && !isPresent)
            parent.Children.Add(child);
        else if (!enabled && isPresent)
            parent.Children.Remove(child);
    }

    private static double ClampSetting(
        double value,
        double fallback,
        double minimum,
        double maximum)
    {
        return double.IsFinite(value)
            ? Math.Clamp(value, minimum, maximum)
            : fallback;
    }

    private static double SmoothStep(double value)
    {
        value = Math.Clamp(value, 0.0, 1.0);
        return value * value * (3.0 - 2.0 * value);
    }

    private static double Lerp(double start, double end, double t)
    {
        return start + (end - start) * Math.Clamp(t, 0.0, 1.0);
    }

    private static Color BlendColors(
        Color start,
        Color end,
        double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);

        return Color.FromRgb(
            LerpByte(start.R, end.R, t),
            LerpByte(start.G, end.G, t),
            LerpByte(start.B, end.B, t));
    }

    private static byte LerpByte(byte start, byte end, double t)
    {
        return (byte)Math.Clamp(
            Math.Round(start + (end - start) * t),
            0.0,
            255.0);
    }

    private static byte ScaleByte(byte value, double scale)
    {
        return (byte)Math.Clamp(
            Math.Round(value * scale),
            0.0,
            255.0);
    }
}
