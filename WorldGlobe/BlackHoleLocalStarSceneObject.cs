using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.WorldGlobe.Particles;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.WorldGlobe;

internal sealed class BlackHoleLocalStarSceneObject :
    ILocalStarSceneObject,
    IWorldGlobeAnimatedObject
{
    private const int DiskAngularSegments = 384;
    private const int DiskRadialSegments = 64;

    private const double SecondsPerDiskRevolution = 60.0;
    private const double DefaultJetPulseRate = 0.325;

    // Maximum disk opacity, applied through the texture alpha channel.
    private const double DiskOpacity = 0.58;

    private readonly Model3DGroup _root;
    private readonly Model3DGroup _positionGroup;
    private readonly Model3DGroup _orientationGroup;
    private readonly Model3DGroup _diskSpinGroup;
    private readonly Model3DGroup _diskGroup;
    private readonly Model3DGroup _jetGroup;

    private readonly TranslateTransform3D _translation;
    private readonly QuaternionRotation3D _orientationRotation;
    private readonly QuaternionRotation3D _diskSpinRotation;

    private readonly ScaleTransform3D _blackHoleScale;
    private readonly ScaleTransform3D _diskScale;
    private readonly ScaleTransform3D _jetScale;

    private readonly GeometryModel3D _blackHoleModel;
    private readonly GeometryModel3D _photonRingModel;

    private readonly LinearGradientBrush _photonRingBrush;
    private readonly ImageBrush _accretionDiskBrush;

    private readonly ImageBrush _jetOuterBrush;
    private readonly ImageBrush _jetCoreBrush;

    private readonly PointLight _light;

    private Color _lastDiskColor;
    private WorldRotationDirection _lastDiskDirection;

    private double _diskSpinAngle;
    private double _rotationRate;
    private double _jetPulseRateHz = DefaultJetPulseRate;

    private WorldRotationDirection _rotationDirection = WorldRotationDirection.Prograde;

    private readonly JetParticleBehavior _northJetBehavior;
    private readonly JetParticleBehavior _southJetBehavior;

    private readonly DeterministicParticleSystem3D _northJetParticles;
    private readonly DeterministicParticleSystem3D _southJetParticles;
    private readonly DeterministicParticleSystem3D _diskParticles;

    private readonly AccretionDiskParticleBehavior _diskParticleBehavior;

    private readonly Model3DGroup _diskParticleScaleGroup;
    private readonly ScaleTransform3D _diskParticleScale;

    public LocalStarType Type => LocalStarType.BlackHole;

    public Model3DGroup Root => _root;

    public BlackHoleLocalStarSceneObject(LocalStar star, MeshGeometry3D unitSphereMesh)
    {
        ArgumentNullException.ThrowIfNull(star);
        ArgumentNullException.ThrowIfNull(unitSphereMesh);

        // -------------------------------------------------------------
        // Scene transforms
        // -------------------------------------------------------------

        _translation = new TranslateTransform3D();

        _orientationRotation = new QuaternionRotation3D(
            Quaternion.Identity);

        _diskSpinRotation = new QuaternionRotation3D(
            Quaternion.Identity);

        _blackHoleScale = new ScaleTransform3D(1, 1, 1);
        _diskScale = new ScaleTransform3D(1, 1, 1);
        _jetScale = new ScaleTransform3D(1, 1, 1);

        _root = new Model3DGroup
        {
            Transform = star.LocationTransform
        };

        _positionGroup = new Model3DGroup
        {
            Transform = _translation
        };

        _orientationGroup = new Model3DGroup
        {
            Transform = new RotateTransform3D(_orientationRotation)
        };

        _diskSpinGroup = new Model3DGroup
        {
            Transform = new RotateTransform3D(_diskSpinRotation)
        };

        _diskGroup = new Model3DGroup
        {
            Transform = _diskScale
        };

        _jetGroup = new Model3DGroup
        {
            Transform = _jetScale
        };

        _root.Children.Add(_positionGroup);
        _positionGroup.Children.Add(_orientationGroup);

        // -------------------------------------------------------------
        // Event horizon
        // -------------------------------------------------------------

        Material blackMaterial = CreateBlackMaterial();

        _blackHoleModel = new GeometryModel3D
        {
            Geometry = unitSphereMesh,
            Material = blackMaterial,
            BackMaterial = blackMaterial,
            Transform = _blackHoleScale
        };

        // -------------------------------------------------------------
        // Photon ring
        // -------------------------------------------------------------

        _photonRingBrush = CreatePhotonRingBrush(star.StarColor);

        Material photonRingMaterial =
            CreateEmissiveMaterial(_photonRingBrush);

        _photonRingModel = new GeometryModel3D
        {
            Geometry = CreateAnnulusMesh(0.98, 1.075),
            Material = photonRingMaterial,
            BackMaterial = photonRingMaterial
        };

        _diskGroup.Children.Add(_photonRingModel);

        // -------------------------------------------------------------
        // Accretion disk
        // -------------------------------------------------------------

        _lastDiskColor = star.StarColor;
        _lastDiskDirection = star.RotationDirection;

        _accretionDiskBrush = new ImageBrush(
            CreateAccretionDiskTexture(
                star.StarColor,
                star.RotationDirection))
        {
            Stretch = Stretch.Fill,
            TileMode = TileMode.None,
            Opacity = 1.0
        };

        Material accretionDiskMaterial =
            CreateEmissiveMaterial(_accretionDiskBrush);

        var accretionDiskModel = new GeometryModel3D
        {
            Geometry = CreateAccretionDiskMesh(1.075, 3.25),
            Material = accretionDiskMaterial,
            BackMaterial = accretionDiskMaterial
        };

        _diskGroup.Children.Add(accretionDiskModel);
        _diskSpinGroup.Children.Add(_diskGroup);

        // -------------------------------------------------------------
        // Plasma jet textures
        // -------------------------------------------------------------

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

        Material outerJetMaterial =
            CreateEmissiveMaterial(_jetOuterBrush);

        Material coreJetMaterial =
            CreateEmissiveMaterial(_jetCoreBrush);

        AddJet(true, outerJetMaterial, coreJetMaterial);
        AddJet(false, outerJetMaterial, coreJetMaterial);

        // -------------------------------------------------------------
        // Deterministic jet particles
        // -------------------------------------------------------------

        _northJetBehavior = new JetParticleBehavior(
            north: true,
            jetLength: 8.4,
            speed: 2.625);

        _northJetParticles = new DeterministicParticleSystem3D(
            new ParticleEmitterSettings(
                emissionRate: 22.0,
                lifetimeSeconds: 3.2,
                maxParticles: 88,
                seed: 1101,
                emissionJitterFraction: 0.30,
                lifetimeVariation: 0.18),
            _northJetBehavior);

        _southJetBehavior = new JetParticleBehavior(
            north: false,
            jetLength: 8.4,
            speed: 2.625);

        _southJetParticles = new DeterministicParticleSystem3D(
            new ParticleEmitterSettings(
                emissionRate: 22.0,
                lifetimeSeconds: 3.2,
                maxParticles: 88,
                seed: 2202,
                emissionJitterFraction: 0.30,
                lifetimeVariation: 0.18),
            _southJetBehavior);

        // The jet group already scales to the black hole's radius.
        _jetGroup.Children.Add(_northJetParticles.Root);
        _jetGroup.Children.Add(_southJetParticles.Root);

        // -------------------------------------------------------------
        // Deterministic accretion-disk particles
        // -------------------------------------------------------------

        _diskParticleBehavior = new AccretionDiskParticleBehavior();

        _diskParticles = new DeterministicParticleSystem3D(
            new ParticleEmitterSettings(
                emissionRate: 6.0,
                lifetimeSeconds: 2.5,
                maxParticles: 24,
                seed: 3303,
                emissionJitterFraction: 0.35,
                lifetimeVariation: 0.20),
            _diskParticleBehavior);

        // Disk particles have their own scale group. Unlike the disk mesh,
        // they must not be children of the disk-spin group: their behavior
        // calculates their orbital positions directly from animation time.
        _diskParticleScale = new ScaleTransform3D(1, 1, 1);

        _diskParticleScaleGroup = new Model3DGroup
        {
            Transform = _diskParticleScale
        };

        _diskParticleScaleGroup.Children.Add(_diskParticles.Root);

        // -------------------------------------------------------------
        // Local light
        // -------------------------------------------------------------

        _light = new PointLight
        {
            Position = new Point3D(0, 0, 0),
            Color = Colors.White,
            ConstantAttenuation = 1.0,
            LinearAttenuation = 0.0
        };

        // The main orientation affects the disk plane and both jets.
        // The independent spin transform affects only the disk.
        _orientationGroup.Children.Add(_jetGroup);
        _orientationGroup.Children.Add(_diskParticleScaleGroup);
        _orientationGroup.Children.Add(_diskSpinGroup);
        _orientationGroup.Children.Add(_blackHoleModel);
        _orientationGroup.Children.Add(_light);

        Update(star);
    }

    // =================================================================
    // Local-star lifecycle
    // =================================================================

    public void Update(LocalStar star)
    {
        ArgumentNullException.ThrowIfNull(star);

        double radius = Math.Max(0.001, star.Radius);

        _root.Transform = star.LocationTransform;

        _translation.OffsetX = star.Center.X;
        _translation.OffsetY = star.Center.Y;
        _translation.OffsetZ = star.Center.Z;

        _orientationRotation.Quaternion = star.RotationQuaternion;

        _blackHoleScale.ScaleX = radius;
        _blackHoleScale.ScaleY = radius;
        _blackHoleScale.ScaleZ = radius;

        _diskScale.ScaleX = radius;
        _diskScale.ScaleY = radius;
        _diskScale.ScaleZ = radius;

        _jetScale.ScaleX = radius;
        _jetScale.ScaleY = radius;
        _jetScale.ScaleZ = radius;

        _diskParticleScale.ScaleX = radius;
        _diskParticleScale.ScaleY = radius;
        _diskParticleScale.ScaleZ = radius;

        _rotationRate = double.IsFinite(star.RotationRate)
            ? Math.Clamp(star.RotationRate, 0.0, 10.0)
            : 0.0;

        _jetPulseRateHz = double.IsFinite(star.JetPulseRate)
            ? Math.Clamp(star.JetPulseRate, 0.0, 2.0)
            : DefaultJetPulseRate;

        UpdatePhotonRingBrush(star.StarColor);

        // Reverse the spiral handedness when the rotation direction
        // changes. Also refresh the texture if the selected color changes.
        if (_lastDiskColor != star.StarColor ||
            _lastDiskDirection != star.RotationDirection)
        {
            _accretionDiskBrush.ImageSource =
                CreateAccretionDiskTexture(
                    star.StarColor,
                    star.RotationDirection);

            _lastDiskColor = star.StarColor;
            _lastDiskDirection = star.RotationDirection;
        }

        _light.Color = ScaleLightColor(
            Colors.White,
            star.LightIntensity);

        double distanceFromOrigin =
            (star.Center - new Point3D(0, 0, 0)).Length;

        double effectiveRange = Math.Max(
            Math.Max(0.1, star.LightRange),
            distanceFromOrigin + 2.0);

        _light.QuadraticAttenuation =
            1.0 / (effectiveRange * effectiveRange);
    }

    // =================================================================
    // Shared animation interface
    // =================================================================

    public void AdvanceAnimation(WorldGlobeAnimationFrame frame)
    {
        if (frame.DeltaSeconds <= 0.0)
            return;

        if (_rotationRate > 0.0)
        {
            double direction =
                _rotationDirection == WorldRotationDirection.Prograde
                    ? 1.0
                    : -1.0;

            double degreesPerSecond =
                360.0 / SecondsPerDiskRevolution * _rotationRate;

            _diskSpinAngle +=
                direction * degreesPerSecond * frame.DeltaSeconds;

            _diskSpinAngle %= 360.0;

            if (_diskSpinAngle < 0.0)
                _diskSpinAngle += 360.0;

            _diskSpinRotation.Quaternion = new Quaternion(
                new Vector3D(0, 1, 0),
                _diskSpinAngle);
        }

        double pulse;

        if (_jetPulseRateHz <= 0.0)
        {
            pulse = 0.5;
        }
        else
        {
            double time = frame.TimeSeconds;
            double baseFrequency = _jetPulseRateHz;

            // Two slow timing variations.
            // Their combined influence changes the instantaneous
            // pulse rate by approximately +/- 17%.
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

        _jetOuterBrush.Opacity = 0.58 + 0.22 * pulse;
        _jetCoreBrush.Opacity = 0.72 + 0.20 * pulse;

        // Evaluate every particle system at the same absolute time.
        _northJetParticles.AdvanceTo(frame.TimeSeconds);
        _southJetParticles.AdvanceTo(frame.TimeSeconds);
        _diskParticles.AdvanceTo(frame.TimeSeconds);
    }

    public void ResetAnimation()
    {
        _diskSpinAngle = 0.0;
        _diskSpinRotation.Quaternion = Quaternion.Identity;

        _jetOuterBrush.Opacity = 0.69;
        _jetCoreBrush.Opacity = 0.82;

        _northJetParticles.Reset();
        _southJetParticles.Reset();
        _diskParticles.Reset();
    }

    // =================================================================
    // Plasma jets
    // =================================================================

    private void AddJet(bool north, Material outerMaterial, Material coreMaterial)
    {
        var outerJet = new GeometryModel3D
        {
            Geometry = CreateJetMesh(north, 1.0),
            Material = outerMaterial,
            BackMaterial = outerMaterial
        };

        var coreJet = new GeometryModel3D
        {
            Geometry = CreateJetMesh(north, 0.30),
            Material = coreMaterial,
            BackMaterial = coreMaterial
        };

        _jetGroup.Children.Add(outerJet);
        _jetGroup.Children.Add(coreJet);
    }

    private static MeshGeometry3D CreateJetMesh(bool north, double radialScale)
    {
        const int axialSegments = 80;
        const int angularSegments = 40;

        const double startY = 0.80;
        const double jetLength = 8.40;

        var mesh = new MeshGeometry3D();
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

                var normal = new Vector3D(cos, 0, sin);
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

        // Long, gradual taper toward the jet's far end.
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

            // Fade in quickly near the base, then begin the longer
            // transparent fade after the plume has developed.
            double fadeIn = SmoothStep(t / 0.08);

            const double fadeOutStart = 0.28;

            double fadeProgress = Math.Clamp(
                (t - fadeOutStart) / (1.0 - fadeOutStart),
                0.0,
                1.0);

            // Quadratic decay: relatively bright initially, falling off
            // more strongly toward the end of the plume.
            double remainingOpacity = 1.0 - fadeProgress;
            double fadeOut = remainingOpacity * remainingOpacity;

            double axialAlpha = fadeIn * fadeOut;

            // Give the plume a little more brightness through its
            // middle instead of making it a uniformly colored tube.
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
                    // A pale violet plume with a brighter white-violet
                    // region toward its base and middle.
                    Color violet = Color.FromRgb(151, 145, 255);
                    Color pale = Color.FromRgb(210, 214, 255);

                    double blend = Math.Clamp(
                        0.25 + 0.55 * brightness, 0.0, 1.0);

                    color = BlendColors(violet, pale, blend);
                }

                int offset = y * stride + x * 4;

                // Bgra32 uses B, G, R, A channel order.
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

    // =================================================================
    // Accretion disk texture
    // =================================================================

    private static BitmapSource CreateAccretionDiskTexture(Color starColor, WorldRotationDirection direction)
    {
        const int width = 256;
        const int height = 1024;

        int stride = width * 4;
        byte[] pixels = new byte[stride * height];

        var stops = CreateDiskColorStops(starColor);

        double winding =
            direction == WorldRotationDirection.Prograde ? -1.0 : 1.0;

        for (int y = 0; y < height; y++)
        {
            double angularFraction = y / (double)(height - 1);
            double angle = angularFraction * Math.PI * 2.0;

            for (int x = 0; x < width; x++)
            {
                double radialFraction = x / (double)(width - 1);

                Color baseColor = SampleDiskColor(
                    stops,
                    radialFraction);

                // Smooth noise at three spatial scales.
                // The angular direction wraps, so the texture seam remains
                // continuous when the disk completes a revolution.
                double broadNoise = SamplePeriodicNoise(
                    radialFraction, angularFraction, 7, 13);

                double mediumNoise = SamplePeriodicNoise(
                    radialFraction, angularFraction, 19, 31);

                double fineNoise = SamplePeriodicNoise(
                    radialFraction, angularFraction, 43, 67);

                // Distort the spiral phase so its shape varies instead
                // of following perfectly uniform mathematical curves.
                double phaseDistortion =
                    (broadNoise - 0.5) * 2.8
                    + (mediumNoise - 0.5) * 1.2;

                double streak1 = Math.Sin(
                    angle * 13.0
                    + winding * radialFraction * 27.0
                    + phaseDistortion);

                double streak2 = Math.Sin(
                    angle * 29.0
                    + winding * radialFraction * 51.0
                    + phaseDistortion * 1.7);

                double streak3 = Math.Sin(
                    angle * 57.0
                    + winding * radialFraction * 93.0
                    + (fineNoise - 0.5) * 2.2);

                // Bright knots break up the continuous spiral streaks.
                double knotPhase =
                    angle * 5.0
                    + winding * radialFraction * 18.0
                    + (mediumNoise - 0.5) * 2.0;

                double brightKnots = Math.Pow(
                    Math.Max(0.0, Math.Sin(knotPhase)),
                    7.0);

                double turbulence =
                    0.77
                    + 0.13 * streak1
                    + 0.09 * streak2
                    + 0.055 * streak3
                    + 0.12 * brightKnots
                    + 0.42 * (broadNoise - 0.5)
                    + 0.34 * (mediumNoise - 0.5)
                    + 0.24 * (fineNoise - 0.5);

                double brightness = Math.Clamp(
                    turbulence, 0.25, 1.35);

                // A weighted noise mixture produces translucent gaps
                // and bright, denser areas within the disk.
                double density =
                    0.48 * broadNoise
                    + 0.34 * mediumNoise
                    + 0.18 * fineNoise;

                double opacityVariation = 0.25
                    + 0.75 * Math.Pow(density, 1.25);

                // Continue fading toward the outer edge.
                double outerFade = Math.Clamp(
                    (1.0 - radialFraction) / 0.16,
                    0.0,
                    1.0);

                byte alpha = (byte)Math.Clamp(
                    Math.Round(
                        255.0
                        * DiskOpacity
                        * outerFade
                        * opacityVariation),
                    0.0,
                    255.0);

                int offset = y * stride + x * 4;

                // BGRA channel order.
                pixels[offset] =
                    ScaleByte(baseColor.B, brightness);

                pixels[offset + 1] =
                    ScaleByte(baseColor.G, brightness);

                pixels[offset + 2] =
                    ScaleByte(baseColor.R, brightness);

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

    private static double SamplePeriodicNoise(
        double radialFraction,
        double angularFraction,
        int radialCells,
        int angularCells)
    {
        double x = radialFraction * radialCells;
        double wrappedAngle =
            angularFraction - Math.Floor(angularFraction);

        double y = wrappedAngle * angularCells;

        int x0 = (int)Math.Floor(x);
        int x1 = x0 + 1;

        int y0 = (int)Math.Floor(y);
        int y1 = (y0 + 1) % angularCells;

        double tx = SmoothStep(x - x0);
        double ty = SmoothStep(y - Math.Floor(y));

        double a = HashNoise(x0, y0);
        double b = HashNoise(x1, y0);
        double c = HashNoise(x0, y1);
        double d = HashNoise(x1, y1);

        double top = Lerp(a, b, tx);
        double bottom = Lerp(c, d, tx);

        return Lerp(top, bottom, ty);
    }

    private static double HashNoise(int x, int y)
    {
        uint n = unchecked(
            (uint)x * 374761393u
            + (uint)y * 668265263u);

        n = unchecked(
            (n ^ (n >> 13)) * 1274126177u);

        n ^= n >> 16;

        return (n & 0x00FFFFFF) / 16777215.0;
    }

    private static (double Position, Color Color)[]
        CreateDiskColorStops(Color starColor)
    {
        return
        [
            (0.00, Color.FromRgb(255, 255, 244)),
            (0.10, Color.FromRgb(255, 247, 200)),
            (0.25, Color.FromRgb(255, 220, 128)),
            (0.43, Color.FromRgb(255, 168, 70)),
            (0.61, Color.FromRgb(255, 112, 42)),
            (0.77, TintDiskColor(Color.FromRgb(218, 49, 28), starColor, 0.20)),
            (0.90, Color.FromRgb(155, 35, 25)),
            (1.00, Color.FromRgb(100, 20, 15))
        ];
    }

    private static Color SampleDiskColor(
        (double Position, Color Color)[] stops,
        double position)
    {
        if (position <= stops[0].Position)
            return stops[0].Color;

        for (int i = 1; i < stops.Length; i++)
        {
            if (position > stops[i].Position)
                continue;

            var previous = stops[i - 1];
            var next = stops[i];

            double span = next.Position - previous.Position;
            double t = span <= 0.0
                ? 0.0
                : (position - previous.Position) / span;

            return BlendColors(previous.Color, next.Color, t);
        }

        return stops[^1].Color;
    }

    // =================================================================
    // Photon ring and materials
    // =================================================================

    private static LinearGradientBrush CreatePhotonRingBrush(Color starColor)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0.5),
            EndPoint = new Point(1, 0.5),
            MappingMode = BrushMappingMode.RelativeToBoundingBox
        };

        brush.GradientStops.Add(
            new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.00));

        brush.GradientStops.Add(
            new GradientStop(Colors.White, 0.25));

        brush.GradientStops.Add(
            new GradientStop(Color.FromRgb(255, 252, 235), 0.48));

        brush.GradientStops.Add(
            new GradientStop(
                Color.FromArgb(
                    190, starColor.R, starColor.G, starColor.B),
                0.75));

        brush.GradientStops.Add(
            new GradientStop(Color.FromArgb(0, 255, 255, 255), 1.00));

        return brush;
    }

    private void UpdatePhotonRingBrush(Color starColor)
    {
        _photonRingBrush.GradientStops[3].Color =
            Color.FromArgb(
                190,
                starColor.R,
                starColor.G,
                starColor.B);
    }

    private static Material CreateBlackMaterial()
    {
        var brush = new SolidColorBrush(Colors.Black);

        var material = new MaterialGroup();
        material.Children.Add(new DiffuseMaterial(brush));
        material.Children.Add(new EmissiveMaterial(brush));

        return material;
    }

    private static Material CreateEmissiveMaterial(Brush brush)
    {
        var material = new MaterialGroup();
        material.Children.Add(new DiffuseMaterial(brush));
        material.Children.Add(new EmissiveMaterial(brush));

        return material;
    }

    // =================================================================
    // Mesh generation
    // =================================================================

    private static MeshGeometry3D CreateAnnulusMesh(double innerRadius, double outerRadius)
    {
        var mesh = new MeshGeometry3D();

        for (int i = 0; i <= DiskAngularSegments; i++)
        {
            double angle =
                2.0 * Math.PI * i / DiskAngularSegments;

            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            double v = (double)i / DiskAngularSegments;

            mesh.Positions.Add(new Point3D(
                innerRadius * cos, 0, innerRadius * sin));

            mesh.TextureCoordinates.Add(new Point(0, v));
            mesh.Normals.Add(new Vector3D(0, 1, 0));

            mesh.Positions.Add(new Point3D(
                outerRadius * cos, 0, outerRadius * sin));

            mesh.TextureCoordinates.Add(new Point(1, v));
            mesh.Normals.Add(new Vector3D(0, 1, 0));
        }

        for (int i = 0; i < DiskAngularSegments; i++)
        {
            int innerCurrent = i * 2;
            int outerCurrent = innerCurrent + 1;
            int innerNext = innerCurrent + 2;
            int outerNext = innerCurrent + 3;

            mesh.TriangleIndices.Add(innerCurrent);
            mesh.TriangleIndices.Add(innerNext);
            mesh.TriangleIndices.Add(outerCurrent);

            mesh.TriangleIndices.Add(innerNext);
            mesh.TriangleIndices.Add(outerNext);
            mesh.TriangleIndices.Add(outerCurrent);
        }

        return mesh;
    }

    private static MeshGeometry3D CreateAccretionDiskMesh(
        double innerRadius,
        double outerRadius)
    {
        var mesh = new MeshGeometry3D();
        int stride = DiskAngularSegments + 1;

        for (int radialIndex = 0;
             radialIndex <= DiskRadialSegments;
             radialIndex++)
        {
            double radialFraction =
                radialIndex / (double)DiskRadialSegments;

            double radius = innerRadius +
                (outerRadius - innerRadius) * radialFraction;

            for (int angularIndex = 0;
                 angularIndex <= DiskAngularSegments;
                 angularIndex++)
            {
                double heightNoise = SamplePeriodicNoise(
                    radialFraction,
                    angularIndex / (double)DiskAngularSegments,
                    9,
                    17);

                double fineHeightNoise = SamplePeriodicNoise(
                    radialFraction,
                    angularIndex / (double)DiskAngularSegments,
                    25,
                    41);

                double angle = 2.0 * Math.PI
                    * angularIndex / DiskAngularSegments;

                // Small height variations, measured relative to the black hole radius.
                double y =
                    (heightNoise - 0.5) * 0.045
                    + (fineHeightNoise - 0.5) * 0.018
                    + Math.Sin(angle * 3.0 + radialFraction * 8.0) * 0.006;

                mesh.Positions.Add(new Point3D(
                    radius * Math.Cos(angle),
                    y,
                    radius * Math.Sin(angle)));

                mesh.TextureCoordinates.Add(new Point(
                    radialFraction,
                    angularIndex / (double)DiskAngularSegments));

                mesh.Normals.Add(new Vector3D(0, 1, 0));
            }
        }

        for (int radialIndex = 0;
             radialIndex < DiskRadialSegments;
             radialIndex++)
        {
            for (int angularIndex = 0;
                 angularIndex < DiskAngularSegments;
                 angularIndex++)
            {
                int innerCurrent =
                    radialIndex * stride + angularIndex;

                int innerNext = innerCurrent + 1;
                int outerCurrent = innerCurrent + stride;
                int outerNext = outerCurrent + 1;

                mesh.TriangleIndices.Add(innerCurrent);
                mesh.TriangleIndices.Add(innerNext);
                mesh.TriangleIndices.Add(outerCurrent);

                mesh.TriangleIndices.Add(innerNext);
                mesh.TriangleIndices.Add(outerNext);
                mesh.TriangleIndices.Add(outerCurrent);
            }
        }

        return mesh;
    }

    // =================================================================
    // Utility methods
    // =================================================================

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

    private static Color TintDiskColor(
        Color diskColor,
        Color starColor,
        double amount)
    {
        return BlendColors(diskColor, starColor, amount);
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

    private static Color ScaleLightColor(
        Color color,
        double intensity)
    {
        intensity = Math.Clamp(intensity, 0.0, 1.0);

        return Color.FromRgb(
            ScaleByte(color.R, intensity),
            ScaleByte(color.G, intensity),
            ScaleByte(color.B, intensity));
    }
}