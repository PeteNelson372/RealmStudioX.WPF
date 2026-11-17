
using HelixToolkit;
using HelixToolkit.Maths;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.Views.Celestial.Particles;
using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MeshGeometry3D = HelixToolkit.SharpDX.MeshGeometry3D;

namespace RealmStudioX.WPF.Views.Celestial;

internal sealed class BlackHoleCelestialBodyRenderer3DX : LocalStarCelestialBodyRenderer3DX
{
    private const int PhotonRingSegments = 192;

    private const double PhotonRingInnerRadius = 0.98;
    private const double PhotonRingOuterRadius = 1.075;

    private readonly MeshGeometryModel3D _eventHorizon = new();
    private readonly MeshGeometryModel3D _photonRing = new();

    private readonly BlackHoleAccretionDisk3DX _accretionDisk = new();

    private const double DefaultJetLength = 8.4;
    private const double DefaultJetPulseRate = 0.325;

    private readonly GroupModel3D _jetVisualRoot = new();
    private readonly GroupModel3D _jetSurfaceGroup = new();

    private readonly MeshGeometryModel3D _northOuterJet = new();
    private readonly MeshGeometryModel3D _northCoreJet = new();
    private readonly MeshGeometryModel3D _southOuterJet = new();
    private readonly MeshGeometryModel3D _southCoreJet = new();

    private readonly DiffuseMaterial _jetOuterMaterial;
    private readonly DiffuseMaterial _jetCoreMaterial;

    private double _jetLength = DefaultJetLength;
    private double _jetPulseRate = DefaultJetPulseRate;
    private bool _jetsEnabled;

    private const int JetParticlePoolSize = 110;
    private const int DiskParticlePoolSize = 30;

    private readonly JetParticleBehavior _northJetBehavior;
    private readonly JetParticleBehavior _southJetBehavior;

    private readonly DeterministicParticleSystem3DX _northJetParticles;
    private readonly DeterministicParticleSystem3DX _southJetParticles;

    private readonly AccretionDiskParticleBehavior _diskParticleBehavior;
    private readonly DeterministicParticleSystem3DX _diskParticles;

    private bool _activityEnabled;
    private bool _jetParticlesEnabled;
    private bool _diskParticlesEnabled;

    public override CelestialBodyRendererType Type => CelestialBodyRendererType.BlackHole;

    public BlackHoleCelestialBodyRenderer3DX()
    {
        // The event horizon is a completely opaque, unlit black sphere.
        _eventHorizon.Geometry = SphereMesh;
        _eventHorizon.Material = new DiffuseMaterial
        {
            DiffuseColor = new Color4(0, 0, 0, 1),
            EnableUnLit = true
        };
        _eventHorizon.IsTransparent = false;

        // The photon ring is a separate, emissive annulus.
        _photonRing.Geometry = CreatePhotonRingMesh();
        _photonRing.Material = new DiffuseMaterial
        {
            DiffuseColor = new Color4(1.0f, 0.88f, 0.58f, 1.0f),
            EnableUnLit = true
        };
        _photonRing.IsTransparent = true;
        _photonRing.CullMode =
            SharpDX.Direct3D11.CullMode.None;

        // Plasma jet textures are generated procedurally.
        _jetOuterMaterial = CreateJetMaterial(
            core: false,
            opacity: 0.69f);

        _jetCoreMaterial = CreateJetMaterial(
            core: true,
            opacity: 0.82f);

        // Build the north and south plumes.
        _northOuterJet.Geometry = CreateJetMesh(
            north: true, 1.0, _jetLength);

        _northCoreJet.Geometry = CreateJetMesh(
            north: true, 0.30, _jetLength);

        _southOuterJet.Geometry = CreateJetMesh(
            north: false, 1.0, _jetLength);

        _southCoreJet.Geometry = CreateJetMesh(
            north: false, 0.30, _jetLength);

        ConfigureJetModel(_northOuterJet, _jetOuterMaterial);
        ConfigureJetModel(_northCoreJet, _jetCoreMaterial);
        ConfigureJetModel(_southOuterJet, _jetOuterMaterial);
        ConfigureJetModel(_southCoreJet, _jetCoreMaterial);

        _jetSurfaceGroup.Children.Add(_northOuterJet);
        _jetSurfaceGroup.Children.Add(_northCoreJet);
        _jetSurfaceGroup.Children.Add(_southOuterJet);
        _jetSurfaceGroup.Children.Add(_southCoreJet);

        _jetSurfaceGroup.Visibility = Visibility.Collapsed;

        // Create the particle behaviors before constructing their systems.
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

        _diskParticleBehavior = new AccretionDiskParticleBehavior
        {
            OutflowSpeed = 1.0
        };

        // Construct the particle systems.
        _northJetParticles = new DeterministicParticleSystem3DX(
            SphereMesh,
            CreateJetEmitterSettings(22.0, 3.2, 1101),
            _northJetBehavior);

        _southJetParticles = new DeterministicParticleSystem3DX(
            SphereMesh,
            CreateJetEmitterSettings(22.0, 3.2, 2202),
            _southJetBehavior);

        _diskParticles = new DeterministicParticleSystem3DX(
            SphereMesh,
            CreateDiskEmitterSettings(6.0, 2.5),
            _diskParticleBehavior);

        // Assemble the jet hierarchy.
        _jetVisualRoot.Children.Add(_jetSurfaceGroup);
        _jetVisualRoot.Children.Add(_northJetParticles.Root);
        _jetVisualRoot.Children.Add(_southJetParticles.Root);

        // Attach the scene objects.
        // The photon ring and disk are behind the opaque event horizon.
        BodyVisualRoot.Children.Add(_photonRing);
        BodyVisualRoot.Children.Add(_accretionDisk.Model);
        BodyVisualRoot.Children.Add(_diskParticles.Root);
        BodyVisualRoot.Children.Add(_eventHorizon);

        // The jet hierarchy shares the body's transform and axial tilt.
        BodyVisualRoot.Children.Add(_jetVisualRoot);
    }


    protected override void UpdateBodyVisuals(
        CelestialBody body,
        CelestialTextureSource? textureSource)
    {
        _accretionDisk.Update(body.BaseColor);

        CelestialBodyAnimation? animation =
            body.Animations.FirstOrDefault(
                item => item.AnimationType ==
                        CelestialBodyAnimationType.BlackHoleActivity);

        BlackHoleAnimationSettings settings =
            animation?.Settings as BlackHoleAnimationSettings
            ?? new BlackHoleAnimationSettings();

        _activityEnabled = animation?.Enabled == true;

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

        if (Math.Abs(jetLength - _jetLength) > 0.0001)
        {
            _jetLength = jetLength;

            _northOuterJet.Geometry =
                CreateJetMesh(true, 1.0, jetLength);

            _northCoreJet.Geometry =
                CreateJetMesh(true, 0.30, jetLength);

            _southOuterJet.Geometry =
                CreateJetMesh(false, 1.0, jetLength);

            _southCoreJet.Geometry =
                CreateJetMesh(false, 0.30, jetLength);
        }

        // Keep the particle behaviors synchronized with the settings.
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

        // The main jet switch controls both the jet surfaces and jet particles.
        bool jetsEnabled =
            _activityEnabled && settings.EnableJets;

        bool jetParticlesEnabled =
            jetsEnabled && settings.EnableJetParticles;

        bool diskParticlesEnabled =
            _activityEnabled && settings.EnableDiskParticles;

        SetJetsEnabled(jetsEnabled);
        SetJetParticlesEnabled(jetParticlesEnabled);
        SetDiskParticlesEnabled(diskParticlesEnabled);
    }

    public override void AdvanceAnimation(CelestialAnimationFrame frame)
    {
        base.AdvanceAnimation(frame);

        if (!_jetsEnabled || frame.DeltaSeconds <= 0.0)
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

        _jetOuterMaterial.DiffuseColor =
            new Color4(1, 1, 1, (float)(0.58 + 0.22 * pulse));

        _jetCoreMaterial.DiffuseColor =
            new Color4(1, 1, 1, (float)(0.72 + 0.20 * pulse));

        if (_jetParticlesEnabled)
        {
            _northJetParticles.AdvanceTo(frame.TimeSeconds);
            _southJetParticles.AdvanceTo(frame.TimeSeconds);
        }

        if (_diskParticlesEnabled)
        {
            _diskParticles.AdvanceTo(frame.TimeSeconds);
        }
    }

    public override void ResetAnimation()
    {
        base.ResetAnimation();

        ResetJetOpacity();

        _northJetParticles.Reset();
        _southJetParticles.Reset();
        _diskParticles.Reset();
    }

    private void ResetJetOpacity()
    {
        _jetOuterMaterial.DiffuseColor =
            new Color4(1, 1, 1, 0.69f);

        _jetCoreMaterial.DiffuseColor =
            new Color4(1, 1, 1, 0.82f);
    }

    private void SetJetsEnabled(bool enabled)
    {
        _jetSurfaceGroup.Visibility = enabled
            ? Visibility.Visible
            : Visibility.Collapsed;

        _jetsEnabled = enabled;

        if (!enabled)
            ResetJetOpacity();
    }

    private void SetJetParticlesEnabled(bool enabled)
    {
        _jetParticlesEnabled = enabled;

        Visibility visibility = enabled
            ? Visibility.Visible
            : Visibility.Collapsed;

        _northJetParticles.Root.Visibility = visibility;
        _southJetParticles.Root.Visibility = visibility;

        if (!enabled)
        {
            _northJetParticles.Reset();
            _southJetParticles.Reset();
        }
    }

    private void SetDiskParticlesEnabled(bool enabled)
    {
        _diskParticlesEnabled = enabled;

        _diskParticles.Root.Visibility = enabled
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (!enabled)
            _diskParticles.Reset();
    }

    private static ParticleEmitterSettings CreateJetEmitterSettings(
        double emissionRate,
        double lifetime,
        int seed)
    {
        return new ParticleEmitterSettings(
            emissionRate: ClampSetting(
                emissionRate, 22.0, 0.0, 60.0),
            lifetimeSeconds: ClampSetting(
                lifetime, 3.2, 0.1, 20.0),
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
            emissionRate: ClampSetting(
                emissionRate, 6.0, 0.0, 40.0),
            lifetimeSeconds: ClampSetting(
                lifetime, 2.5, 0.1, 20.0),
            maxParticles: DiskParticlePoolSize,
            seed: 3303,
            emissionJitterFraction: 0.35,
            lifetimeVariation: 0.20);
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

    private static MeshGeometry3D CreatePhotonRingMesh()
    {
        MeshGeometry3D mesh = new()
        {
            Positions = new Vector3Collection(),
            Normals = new Vector3Collection(),
            TextureCoordinates = new Vector2Collection(),
            Indices = new IntCollection()
        };

        for (int i = 0; i <= PhotonRingSegments; i++)
        {
            double angle =
                2.0 * Math.PI * i / PhotonRingSegments;

            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            float u = (float)i / PhotonRingSegments;

            mesh.Positions.Add(new Vector3(
                (float)PhotonRingInnerRadius * cos,
                0,
                (float)PhotonRingInnerRadius * sin));

            mesh.Positions.Add(new Vector3(
                (float)PhotonRingOuterRadius * cos,
                0,
                (float)PhotonRingOuterRadius * sin));

            mesh.Normals.Add(new Vector3(0, 1, 0));
            mesh.Normals.Add(new Vector3(0, 1, 0));

            mesh.TextureCoordinates.Add(new Vector2(u, 0));
            mesh.TextureCoordinates.Add(new Vector2(u, 1));
        }

        for (int i = 0; i < PhotonRingSegments; i++)
        {
            int innerCurrent = i * 2;
            int outerCurrent = innerCurrent + 1;

            int innerNext = innerCurrent + 2;
            int outerNext = innerCurrent + 3;

            mesh.Indices.Add(innerCurrent);
            mesh.Indices.Add(innerNext);
            mesh.Indices.Add(outerCurrent);

            mesh.Indices.Add(outerCurrent);
            mesh.Indices.Add(innerNext);
            mesh.Indices.Add(outerNext);
        }

        return mesh;
    }

    private void RefreshJetEnabledState()
    {
        CelestialBodyAnimation? animation =
            Body.Animations.FirstOrDefault(
                item => item.AnimationType ==
                        CelestialBodyAnimationType.BlackHoleActivity);

        BlackHoleAnimationSettings settings =
            animation?.Settings as BlackHoleAnimationSettings
            ?? new BlackHoleAnimationSettings();

        bool enabled = animation?.Enabled == true && settings.EnableJets;

        if (enabled != _jetsEnabled)
            SetJetsEnabled(enabled);
    }

    private static void ConfigureJetModel(
    MeshGeometryModel3D model,
    DiffuseMaterial material)
    {
        model.Material = material;
        model.IsTransparent = true;
        model.CullMode = SharpDX.Direct3D11.CullMode.None;
    }

    private static DiffuseMaterial CreateJetMaterial(
        bool core,
        float opacity)
    {
        BitmapSource bitmap = CreateJetTexture(core);

        return new DiffuseMaterial
        {
            DiffuseColor = new Color4(1, 1, 1, opacity),
            DiffuseMap = CreateTextureModel(bitmap),
            EnableUnLit = true
        };
    }

    private static TextureModel CreateTextureModel(BitmapSource bitmap)
    {
        MemoryStream stream = new();

        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(stream);

        stream.Position = 0;

        return new TextureModel(stream, true);
    }

    private static MeshGeometry3D CreateJetMesh(
        bool north,
        double radialScale,
        double jetLength)
    {
        const int axialSegments = 80;
        const int angularSegments = 40;
        const double startY = 0.80;

        MeshGeometry3D mesh = new()
        {
            Positions = new Vector3Collection(),
            Normals = new Vector3Collection(),
            TextureCoordinates = new Vector2Collection(),
            Indices = new IntCollection()
        };

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

                mesh.Positions.Add(new Vector3(
                    (float)(centerX + radius * irregularity * cos),
                    (float)y,
                    (float)(centerZ + radius * irregularity * sin)));

                mesh.TextureCoordinates.Add(new Vector2(
                    (float)(angularIndex / (double)angularSegments),
                    (float)t));

                Vector3 normal = new(
                    (float)cos, 0, (float)sin);

                normal = Vector3.Normalize(normal);
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

                mesh.Indices.Add(current);
                mesh.Indices.Add(nextAround);
                mesh.Indices.Add(nextAxial);

                mesh.Indices.Add(nextAround);
                mesh.Indices.Add(nextAxialAround);
                mesh.Indices.Add(nextAxial);
            }
        }

        return mesh;
    }

    private static double GetJetRadius(double t)
    {
        // Narrow at the pole, then flare rapidly into the plume.
        if (t < 0.12)
        {
            double u = SmoothStep(t / 0.12);
            return Lerp(0.035, 0.195, u);
        }

        // Maintain a relatively broad shoulder near the base.
        if (t < 0.30)
        {
            double u = SmoothStep((t - 0.12) / 0.18);
            return Lerp(0.195, 0.165, u);
        }

        // Long, gradual taper toward the far end.
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
                        maximumAlpha *
                        axialAlpha *
                        angularVariation),
                    0.0,
                    255.0);

                System.Windows.Media.Color color;

                if (core)
                {
                    color = System.Windows.Media.Color.FromRgb(
                        ScaleByte(255, brightness),
                        ScaleByte(252, brightness),
                        ScaleByte(255, brightness));
                }
                else
                {
                    System.Windows.Media.Color violet =
                        System.Windows.Media.Color.FromRgb(151, 145, 255);

                    System.Windows.Media.Color pale =
                        System.Windows.Media.Color.FromRgb(210, 214, 255);

                    double blend = Math.Clamp(
                        0.25 + 0.55 * brightness,
                        0.0,
                        1.0);

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

    private static double SmoothStep(double value)
    {
        value = Math.Clamp(value, 0.0, 1.0);
        return value * value * (3.0 - 2.0 * value);
    }

    private static double Lerp(double start, double end, double t)
    {
        return start + (end - start) * Math.Clamp(t, 0.0, 1.0);
    }

    private static System.Windows.Media.Color BlendColors(
        System.Windows.Media.Color start,
        System.Windows.Media.Color end,
        double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);

        return System.Windows.Media.Color.FromRgb(
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
