using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.WorldGlobe;

/// <summary>
/// A reusable, camera-facing solar-activity effect. The corona glow is a single
/// radial billboard; prominences and flares are emitted as bundles of deterministic,
/// irregular filaments on the apparent limb. Filament meshes deform each frame.
/// </summary>
internal sealed class SolarActivityEffect3D
{
    private const int GlowTextureSize = 384;
    private const int GlowDiscSegments = 128;
    private const int TubeRadialSegments = 7;
    private const int CurveSegments = 40;
    private const int MaximumConcurrentEvents = 18;
    private const double CoronaTextureUpdateInterval = 0.09;
    private const double DefaultActivityFrequency = 8.0; // Events per minute.
    private const uint InitialRandomState = 0x9E3779B9;

    private readonly Model3DGroup _root = new();
    private readonly List<ActivityEvent> _activeEvents = [];
    private readonly ImageBrush? _glowBrush;
    private readonly SolidColorBrush? _limbBrush;
    private readonly ScaleTransform3D _glowScale = new(1.0, 1.0, 1.0);
    private readonly ScaleTransform3D _limbScale = new(1.0, 1.0, 1.0);

    private Color _starColor = Colors.White;
    private bool _enableProminences = true;
    private bool _enableFlares = true;
    private double _activityIntensity = 1.0;
    private double _activityFrequency = DefaultActivityFrequency;

    private bool _timelineInitialized;
    private bool _firstEmissionPending = true;
    private double _currentTime;
    private double _nextEmissionTime = double.PositiveInfinity;
    private uint _randomState = InitialRandomState;
    private double _lastGlowTextureUpdateTime = double.NegativeInfinity;

    private double _activityGeometryScale = 1.0;
    private double _activityThicknessScale = 1.0;

    public Model3DGroup Root => _root;

    /// <param name="includeCoronaGlow">
    /// True for the eclipse view. The same event system can be used over a visible Sun.
    /// </param>
    public SolarActivityEffect3D(
        Color starColor,
        bool includeCoronaGlow,
        double activityFrequency = DefaultActivityFrequency,
        double activityGeometryScale = 1.0,
        double activityThicknessScale = 1.0)
    {
        _activityGeometryScale = double.IsFinite(activityGeometryScale)
            ? Math.Clamp(activityGeometryScale, 0.1, 1.0)
            : 1.0;

        _activityThicknessScale = double.IsFinite(activityThicknessScale)
            ? Math.Clamp(activityThicknessScale, 0.5, 2.0)
            : 1.0;

        // This translation puts the effects just in front of the sphere's front
        // surface. The surrounding parent group rotates this plane to face the camera.
        _root.Transform = new TranslateTransform3D(0, 0, 1.008);

        if (includeCoronaGlow)
        {
            _glowBrush = new ImageBrush(CreateCoronaGlowTexture(starColor, 0.0))
            {
                Stretch = Stretch.Fill,
                TileMode = TileMode.None,
                Opacity = 1.0
            };

            Material glowMaterial = new EmissiveMaterial(_glowBrush);
            GeometryModel3D glowModel = new()
            {
                Geometry = CreateDiscMesh(1.92, GlowDiscSegments),
                Material = glowMaterial,
                BackMaterial = glowMaterial,
                Transform = _glowScale
            };
            _root.Children.Add(glowModel);

            // Opaque center cap. The glow texture intentionally has a transparent
            // center, and WPF's transparent-material sorting can otherwise allow
            // the starfield to show through the eclipsed disk. This camera-facing
            // black disk sits just in front of the sphere and behind the outer limb.
            SolidColorBrush opaqueBlackBrush = new(Colors.Black)
            {
                Opacity = 1.0
            };
            opaqueBlackBrush.Freeze();
            Material opaqueBlackMaterial = new DiffuseMaterial(opaqueBlackBrush);
            if (opaqueBlackMaterial.CanFreeze)
                opaqueBlackMaterial.Freeze();

            GeometryModel3D centralOcclusionModel = new()
            {
                Geometry = CreateDiscMesh(1.005, GlowDiscSegments),
                Material = opaqueBlackMaterial,
                BackMaterial = opaqueBlackMaterial,
                Transform = new TranslateTransform3D(0, 0, 0.012)
            };
            _root.Children.Add(centralOcclusionModel);

            _limbBrush = new SolidColorBrush(BlendWithWhite(starColor, 0.80))
            {
                Opacity = 0.84
            };
            Material limbMaterial = new EmissiveMaterial(_limbBrush);
            GeometryModel3D limbModel = new()
            {
                Geometry = CreateAnnulusMesh(0.985, 1.018, 192),
                Material = limbMaterial,
                BackMaterial = limbMaterial,
                Transform = _limbScale
            };
            _root.Children.Add(limbModel);
        }

        Update(
            starColor,
            enableProminences: true,
            enableFlares: true,
            activityIntensity: 1.0,
            activityFrequency: activityFrequency,
            activityGeometryScale: _activityGeometryScale,
            activityThicknessScale: _activityThicknessScale);
    }

    public void Update(
        Color starColor,
        bool enableProminences,
        bool enableFlares,
        double activityIntensity,
        double activityFrequency,
        double activityGeometryScale,
        double activityThicknessScale)
    {
        bool wasEnabled = HasActivityEnabled;
        double oldFrequency = _activityFrequency;
        bool colorChanged = _starColor != starColor;

        _starColor = starColor;
        _enableProminences = enableProminences;
        _enableFlares = enableFlares;
        _activityIntensity = Clamp(activityIntensity, 1.0, 0.0, 2.0);
        _activityFrequency = Clamp(activityFrequency, DefaultActivityFrequency, 0.0, 30.0);

        _activityGeometryScale = Clamp(activityGeometryScale, 1.0, 0.1, 1.0);

        _activityThicknessScale = Clamp(activityThicknessScale, 1.0, 0.5, 2.0);

        if (colorChanged)
        {
            if (_glowBrush is not null)
                _glowBrush.ImageSource = CreateCoronaGlowTexture(starColor, _currentTime);

            if (_limbBrush is not null)
                _limbBrush.Color = BlendWithWhite(starColor, 0.80);

            foreach (ActivityEvent activityEvent in _activeEvents)
                UpdateEventColors(activityEvent);
        }

        bool isEnabled = HasActivityEnabled;
        if (!isEnabled || _activityFrequency <= 0.0)
        {
            _nextEmissionTime = double.PositiveInfinity;
        }
        else if (_timelineInitialized &&
                 (!wasEnabled || oldFrequency <= 0.0 || oldFrequency != _activityFrequency))
        {
            ScheduleNextEmission(_currentTime);
        }

        ApplyCurrentEventOpacities();
    }

    /// <summary>
    /// Called from the shared WorldGlobe animation timeline. The first call only
    /// schedules the first emission, so the Corona begins without any flares/loops.
    /// </summary>
    public void AdvanceAnimation(WorldGlobeAnimationFrame frame)
    {
        double time = double.IsFinite(frame.TimeSeconds)
            ? Math.Max(0.0, frame.TimeSeconds)
            : 0.0;

        if (_timelineInitialized && time < _currentTime - 1e-6)
            ResetAnimation();

        if (!_timelineInitialized)
        {
            _timelineInitialized = true;
            _currentTime = time;
            UpdateCoronaMotion(time);
            ScheduleNextEmission();
            return;
        }

        _currentTime = time;
        UpdateCoronaMotion(time);

        // Catch up deterministically if a frame is delayed. The guard prevents a
        // long suspension from causing an excessive burst of mesh creation.
        int catchUpCount = 0;
        while (_nextEmissionTime <= time && catchUpCount < 64)
        {
            double eventStart = _nextEmissionTime;
            SpawnRandomEvent(eventStart);
            ScheduleNextEmission(eventStart);
            catchUpCount++;
        }

        if (_nextEmissionTime <= time)
            ScheduleNextEmission(time);

        for (int i = _activeEvents.Count - 1; i >= 0; i--)
        {
            ActivityEvent activityEvent = _activeEvents[i];
            double age = time - activityEvent.StartTime;
            if (age >= activityEvent.Duration)
            {
                RemoveEventAt(i);
                continue;
            }

            UpdateEventAnimation(activityEvent, age);
        }
    }

    public void ResetAnimation()
    {
        for (int i = _activeEvents.Count - 1; i >= 0; i--)
            RemoveEventAt(i);

        _randomState = InitialRandomState;
        _timelineInitialized = false;
        _firstEmissionPending = true;
        _currentTime = 0.0;
        _nextEmissionTime = double.PositiveInfinity;
        _lastGlowTextureUpdateTime = double.NegativeInfinity;
        _glowScale.ScaleX = _glowScale.ScaleY = 1.0;
        _limbScale.ScaleX = _limbScale.ScaleY = 1.0;
    }

    private bool HasActivityEnabled => _enableProminences || _enableFlares;

    private void ScheduleNextEmission(double fromTime)
    {
        if (!HasActivityEnabled || _activityFrequency <= 0.0)
        {
            _nextEmissionTime = double.PositiveInfinity;
            return;
        }

        if (_firstEmissionPending)
        {
            // Leave a short quiet opening so the initial eclipsed Sun is clearly
            // visible before the first randomly emitted prominence or flare.
            _firstEmissionPending = false;
            _nextEmissionTime = fromTime + Lerp(1.8, 3.2, NextRandomUnit());
            return;
        }

        _nextEmissionTime = fromTime + SampleEmissionInterval();
    }

    private void ScheduleNextEmission() => ScheduleNextEmission(_currentTime);

    private double SampleEmissionInterval()
    {
        // Exponential intervals produce irregular, Poisson-like event spacing.
        // ActivityFrequency is an average number of events per minute.
        double ratePerSecond = _activityFrequency / 60.0;
        if (ratePerSecond <= 0.0)
            return double.PositiveInfinity;

        double u = NextRandomUnit();
        double interval = -Math.Log(1.0 - u) / ratePerSecond;
        return Math.Max(0.65, interval);
    }

    private void SpawnRandomEvent(double startTime)
    {
        if (!HasActivityEnabled || _activityIntensity <= 0.0)
            return;

        bool isFlare;
        if (!_enableProminences)
            isFlare = true;
        else if (!_enableFlares)
            isFlare = false;
        else
            isFlare = NextRandomUnit() < 0.38;

        // Treat an emission as a small bundle of related plasma strands rather
        // than a single, perfectly isolated loop. Small offsets in angle, span,
        // and length make the strands overlap irregularly like tangled filaments.
        double anchorAngle = NextRandomUnit() * Math.PI * 2.0;
        int strandCount = NextRandomUnit() < 0.62 ? 2 : 3;
        double angularSpread = Lerp(0.035, 0.095, NextRandomUnit());

        for (int strand = 0; strand < strandCount; strand++)
        {
            double strandOffset = strandCount == 1
                ? 0.0
                : (strand / (double)(strandCount - 1) - 0.5) * angularSpread;
            double angle = anchorAngle + strandOffset
                + Lerp(-0.018, 0.018, NextRandomUnit());
            ActivityEvent activityEvent = CreateActivityEvent(
                isFlare,
                angle,
                startTime + strand * 0.025);

            if (_activeEvents.Count >= MaximumConcurrentEvents)
                RemoveEventAt(0);

            _activeEvents.Add(activityEvent);
            _root.Children.Add(activityEvent.OuterModel);
            _root.Children.Add(activityEvent.CoreModel);
            UpdateEventColors(activityEvent);
            UpdateEventAnimation(activityEvent, 0.0);
        }
    }

    private ActivityEvent CreateActivityEvent(bool isFlare, double angle, double startTime)
    {
        double width = Lerp(0.012, 0.027, NextRandomUnit());

        double planeBend = Lerp(-0.19, 0.19, NextRandomUnit());
        
        double noisePhaseA = NextRandomUnit() * Math.PI * 2.0;
        
        double noisePhaseB = NextRandomUnit() * Math.PI * 2.0;
        
        double noisePhaseC = NextRandomUnit() * Math.PI * 2.0;
        
        double length;
        
        double duration;
        
        List<Point3D> centerline = new(CurveSegments + 1);

        if (isFlare)
        {
            length = Lerp(0.38, 0.86, NextRandomUnit());
            duration = Lerp(1.35, 2.75, NextRandomUnit());
            
            for (int i = 0; i <= CurveSegments; i++)
            {
                double t = i / (double)CurveSegments;

                double radialTurbulence = t * (
                    0.035 * Math.Sin(t * 8.4 + noisePhaseA) +
                    0.017 * Math.Sin(t * 20.7 + noisePhaseB) +
                    0.009 * Math.Sin(t * 33.0 - noisePhaseC));
                
                double angularTurbulence = t * (
                    0.060 * Math.Sin(t * 4.7 + noisePhaseB) +
                    0.034 * Math.Sin(t * 13.6 + noisePhaseC) +
                    0.015 * Math.Sin(t * 27.0 + noisePhaseA));
                
                double radius = 1.012 + length * t + radialTurbulence;
                
                double bentAngle = angle + planeBend * Math.Sin(Math.PI * t)
                    + planeBend * 0.18 * t + angularTurbulence;
                
                double depth = 0.012
                    + 0.018 * Math.Sin(Math.PI * t)
                    + 0.020 * t * Math.Sin(t * 10.5 + noisePhaseC)
                    + 0.008 * t * Math.Sin(t * 24.0 + noisePhaseA);
                
                centerline.Add(new Point3D(
                    radius * Math.Cos(bentAngle),
                    radius * Math.Sin(bentAngle),
                    depth));
            }
        }
        else
        {
            double span = Lerp(0.34, 0.82, NextRandomUnit());
            
            length = Lerp(0.22, 0.52, NextRandomUnit());
            
            duration = Lerp(3.0, 5.8, NextRandomUnit());
            
            for (int i = 0; i <= CurveSegments; i++)
            {
                double t = i / (double)CurveSegments;
                
                double arch = Math.Pow(Math.Max(0.0, Math.Sin(Math.PI * t)), 0.86);
                
                double radialTurbulence = arch * (
                    0.045 * Math.Sin(t * 13.2 + noisePhaseA) +
                    0.020 * Math.Sin(t * 25.5 + noisePhaseB) +
                    0.011 * Math.Sin(t * 37.0 + noisePhaseC));
                
                double angularTurbulence = arch * (
                    0.034 * Math.Sin(t * 7.1 + noisePhaseB) +
                    0.020 * Math.Sin(t * 17.0 + noisePhaseC));
                
                double localAngle = angle + (t - 0.5) * span + angularTurbulence;
                
                double radius = 1.012 + length * arch + radialTurbulence;
                
                double depth = 0.012
                    + 0.014 * Math.Sin(Math.PI * t)
                    + 0.013 * arch * Math.Sin(t * 15.0 + noisePhaseC);
                
                centerline.Add(new Point3D(
                    radius * Math.Cos(localAngle),
                    radius * Math.Sin(localAngle),
                    depth));
            }
        }

        Func<double, double> outerRadius = isFlare
            ? t => width * (0.80 * Math.Pow(1.0 - t, 1.08) + 0.018)
            : t => width * (0.13 + 0.87 * Math.Pow(Math.Max(0.0, Math.Sin(Math.PI * t)), 0.43));
        
        Func<double, double> coreRadius = isFlare
            ? t => width * 0.31 * (0.80 * Math.Pow(1.0 - t, 1.05) + 0.016)
            : t => width * 0.32 * (0.10 + 0.90 * Math.Pow(Math.Max(0.0, Math.Sin(Math.PI * t)), 0.39));

        MeshGeometry3D outerMesh = CreateTubeMesh(
            centerline, outerRadius, TubeRadialSegments, _activityThicknessScale);

        MeshGeometry3D coreMesh = CreateTubeMesh(
            centerline, coreRadius, TubeRadialSegments, _activityThicknessScale);

        SolidColorBrush outerBrush = new(Colors.White) { Opacity = 0.0 };
        SolidColorBrush coreBrush = new(Colors.White) { Opacity = 0.0 };
        Material outerMaterial = new EmissiveMaterial(outerBrush);
        Material coreMaterial = new EmissiveMaterial(coreBrush);

        Point3D anchor = new(Math.Cos(angle), Math.Sin(angle), 0.0);
        ScaleTransform3D scale = new(0.15, 0.15, 0.15, anchor.X, anchor.Y, anchor.Z);

        GeometryModel3D outerModel = new()
        {
            Geometry = outerMesh,
            Material = outerMaterial,
            BackMaterial = outerMaterial,
            Transform = scale
        };

        GeometryModel3D coreModel = new()
        {
            Geometry = coreMesh,
            Material = coreMaterial,
            BackMaterial = coreMaterial,
            Transform = scale
        };

        return new ActivityEvent(
            isFlare, startTime, duration, outerModel, coreModel,
            outerBrush, coreBrush, scale, outerMesh, coreMesh,
            centerline, outerRadius, coreRadius,
            noisePhaseA, noisePhaseB, noisePhaseC);
    }

    private void UpdateEventAnimation(ActivityEvent activityEvent, double age)
    {
        UpdateEventGeometry(activityEvent, age);
        
        double progress = Math.Clamp(age / activityEvent.Duration, 0.0, 1.0);
        
        double fadeIn = SmoothStep(0.0, activityEvent.IsFlare ? 0.10 : 0.16, progress);
        
        double fadeOut = activityEvent.IsFlare
            ? 1.0 - SmoothStep(0.34, 1.0, progress)
            : 1.0 - SmoothStep(0.64, 1.0, progress);
        
        double pulse = 0.88 + 0.12 * Math.Sin(age * (activityEvent.IsFlare ? 9.0 : 3.1)
            + activityEvent.PulsePhase);
        
        double opacity = Math.Clamp(fadeIn * fadeOut * pulse * _activityIntensity, 0.0, 1.0);

        bool enabled = activityEvent.IsFlare ? _enableFlares : _enableProminences;
        if (!enabled)
            opacity = 0.0;

        double growth = activityEvent.IsFlare
            ? Lerp(0.24, 1.0, SmoothStep(0.0, 0.22, progress))
            : Lerp(0.22, 1.0, SmoothStep(0.0, 0.34, progress));

        double eventScale = _activityGeometryScale * growth;

        activityEvent.Scale.ScaleX = eventScale;
        activityEvent.Scale.ScaleY = eventScale;
        activityEvent.Scale.ScaleZ = eventScale;

        activityEvent.OuterBrush.Opacity = Math.Clamp(opacity * (activityEvent.IsFlare ? 0.78 : 0.64), 0.0, 1.0);
        activityEvent.CoreBrush.Opacity = Math.Clamp(opacity * (activityEvent.IsFlare ? 1.0 : 0.92), 0.0, 1.0);
    }

    private void ApplyCurrentEventOpacities()
    {
        foreach (ActivityEvent activityEvent in _activeEvents)
        {
            double age = Math.Max(0.0, _currentTime - activityEvent.StartTime);
            UpdateEventAnimation(activityEvent, age);
        }
    }

    private void UpdateEventColors(ActivityEvent activityEvent)
    {
        activityEvent.OuterBrush.Color = activityEvent.IsFlare
            ? BlendWithWhite(_starColor, 0.34)
            : BlendWithWhite(_starColor, 0.28);
        activityEvent.CoreBrush.Color = BlendWithWhite(_starColor, activityEvent.IsFlare ? 0.92 : 0.82);
    }

    private void RemoveEventAt(int index)
    {
        ActivityEvent activityEvent = _activeEvents[index];
        _root.Children.Remove(activityEvent.OuterModel);
        _root.Children.Remove(activityEvent.CoreModel);
        _activeEvents.RemoveAt(index);
    }

    private double NextRandomUnit()
    {
        // Xorshift32: simple deterministic PRNG so animation resets replay consistently.
        uint x = _randomState;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        _randomState = x == 0 ? InitialRandomState : x;
        return (_randomState + 0.5) / (uint.MaxValue + 1.0);
    }

    private void UpdateCoronaMotion(double time)
    {
        if (_glowBrush is null)
            return;

        // Two slow, out-of-phase oscillations keep the halo from looking like a
        // rigid, static sticker. The expansion is deliberately subtle so the
        // black disk remains visually anchored at the center.
        double slowPulse = Math.Sin(time * 0.78 + 0.25);
        double fasterPulse = Math.Sin(time * 1.63 + 1.15);
        double glowExpansion = 1.0 + 0.022 * slowPulse + 0.008 * fasterPulse;
        _glowScale.ScaleX = glowExpansion;
        _glowScale.ScaleY = glowExpansion;

        double limbPulse = 1.0 + 0.009 * Math.Sin(time * 0.92 + 0.8);
        _limbScale.ScaleX = limbPulse;
        _limbScale.ScaleY = limbPulse;
        if (_limbBrush is not null)
            _limbBrush.Opacity = 0.77 + 0.075 * Math.Sin(time * 0.86 + 0.35);

        // Rebuild the small procedural alpha texture about 11 times per second.
        // Its changing phase and radial structure make the streamer pattern itself
        // flow, independently of the whole-halo pulse.
        if (time - _lastGlowTextureUpdateTime >= CoronaTextureUpdateInterval)
        {
            _glowBrush.ImageSource = CreateCoronaGlowTexture(_starColor, time);
            _lastGlowTextureUpdateTime = time;
        }
    }

    private static BitmapSource CreateCoronaGlowTexture(Color starColor, double time)
    {
        int stride = GlowTextureSize * 4;
        byte[] pixels = new byte[GlowTextureSize * stride];
        Color tint = BlendWithWhite(starColor, 0.56);

        // Each angular band advances at a different speed. The radial terms also
        // drift, avoiding the appearance of a single texture merely rotating.
        double phaseA = time * 0.31;
        double phaseB = time * -0.23;
        double phaseC = time * 0.17;
        double phaseD = time * 0.43;

        for (int y = 0; y < GlowTextureSize; y++)
        {
            double ny = (y / (double)(GlowTextureSize - 1)) * 2.0 - 1.0;
            for (int x = 0; x < GlowTextureSize; x++)
            {
                double nx = (x / (double)(GlowTextureSize - 1)) * 2.0 - 1.0;
                double radius = Math.Sqrt(nx * nx + ny * ny);
                double angle = Math.Atan2(ny, nx);

                // Let the inner edge breathe very slightly, but preserve a clear
                // transparent center so the black sphere remains completely dark.
                double innerEdge = 0.515 + 0.006 * Math.Sin(angle * 3.0 + phaseC)
                    + 0.003 * Math.Sin(angle * 11.0 - phaseB);
                double innerFade = SmoothStep(innerEdge - 0.040, innerEdge + 0.040, radius);
                double outerEdge = 0.965 + 0.018 * Math.Sin(angle * 4.0 + phaseA)
                    + 0.010 * Math.Sin(angle * 9.0 - phaseD);
                // Use a longer outward fade so the corona dissipates gradually into
                // the starfield. The fade always reaches zero at the texture boundary,
                // avoiding a visible square/round cutoff at the edge of the billboard.
                double outerFade = 1.0 - SmoothStep(outerEdge - 0.19, 1.0, radius);

                double broadStreamers = Math.Sin(angle * 5.0 + radius * 8.0 - phaseA);
                double filamentBands = Math.Sin(angle * 11.0 - radius * 15.0 + phaseB
                    + 0.65 * Math.Sin(angle * 3.0 + phaseC));
                double fineFilaments = Math.Sin(angle * 23.0 + radius * 24.0 + phaseD
                    + 0.45 * Math.Sin(angle * 7.0 - phaseA));
                double rays = 0.69
                    + 0.105 * Math.Sin(angle * 7.0 + 0.7 + phaseA)
                    + 0.082 * Math.Sin(angle * 13.0 - 1.1 + phaseB)
                    + 0.054 * Math.Sin(angle * 23.0 + 0.4 + phaseC)
                    + 0.032 * Math.Sin(angle * 37.0 - 1.7 + phaseD)
                    + 0.095 * broadStreamers
                    + 0.055 * filamentBands
                    + 0.025 * fineFilaments;

                double radialFlow = radius * 34.0 - time * 1.8
                    + 1.9 * Math.Sin(angle * 4.0 + phaseB)
                    + 0.65 * Math.Sin(angle * 13.0 - phaseC);
                double softStructure = 0.78
                    + 0.14 * Math.Sin(radialFlow)
                    + 0.08 * Math.Sin(radius * 61.0 + Math.Sin(angle * 6.0 + phaseD) * 2.2 - time * 0.7);

                // Slightly sharpen a handful of streamers while keeping most of
                // the halo diffuse, more like wispy coronal plumes than sun rays.
                double streamerContrast = Math.Pow(Math.Clamp(0.5 + 0.5 * broadStreamers, 0.0, 1.0), 2.2);
                double density = Math.Clamp(rays * softStructure + 0.10 * streamerContrast, 0.0, 1.1);
                double alpha = Math.Clamp(innerFade * outerFade * density, 0.0, 1.0);

                int offset = y * stride + x * 4;
                pixels[offset] = tint.B;
                pixels[offset + 1] = tint.G;
                pixels[offset + 2] = tint.R;
                pixels[offset + 3] = (byte)Math.Clamp((int)Math.Round(alpha * 225.0), 0, 255);
            }
        }

        BitmapSource bitmap = BitmapSource.Create(
            GlowTextureSize, GlowTextureSize, 96, 96,
            PixelFormats.Bgra32, null, pixels, stride);
        bitmap.Freeze();
        return bitmap;
    }

    private static MeshGeometry3D CreateDiscMesh(double radius, int segments)
    {
        MeshGeometry3D mesh = new();
        Point3DCollection positions = [];
        Vector3DCollection normals = [];
        PointCollection textureCoordinates = [];
        Int32Collection indices = [];

        positions.Add(new Point3D(0, 0, 0));
        normals.Add(new Vector3D(0, 0, 1));
        textureCoordinates.Add(new Point(0.5, 0.5));
        for (int i = 0; i <= segments; i++)
        {
            double angle = 2.0 * Math.PI * i / segments;
            double x = radius * Math.Cos(angle);
            double y = radius * Math.Sin(angle);
            positions.Add(new Point3D(x, y, 0));
            normals.Add(new Vector3D(0, 0, 1));
            textureCoordinates.Add(new Point(0.5 + x / (2.0 * radius), 0.5 - y / (2.0 * radius)));
        }

        for (int i = 1; i <= segments; i++)
        {
            indices.Add(0);
            indices.Add(i);
            indices.Add(i + 1);
        }

        mesh.Positions = positions;
        mesh.Normals = normals;
        mesh.TextureCoordinates = textureCoordinates;
        mesh.TriangleIndices = indices;
        mesh.Freeze();
        return mesh;
    }

    private static MeshGeometry3D CreateAnnulusMesh(double innerRadius, double outerRadius, int segments)
    {
        MeshGeometry3D mesh = new();
        Point3DCollection positions = [];
        Vector3DCollection normals = [];
        Int32Collection indices = [];

        for (int i = 0; i <= segments; i++)
        {
            double angle = 2.0 * Math.PI * i / segments;
            double x = Math.Cos(angle);
            double y = Math.Sin(angle);
            positions.Add(new Point3D(innerRadius * x, innerRadius * y, 0.005));
            positions.Add(new Point3D(outerRadius * x, outerRadius * y, 0.005));
            normals.Add(new Vector3D(0, 0, 1));
            normals.Add(new Vector3D(0, 0, 1));
        }

        for (int i = 0; i < segments; i++)
        {
            int inner0 = i * 2;
            int outer0 = inner0 + 1;
            int inner1 = inner0 + 2;
            int outer1 = inner0 + 3;
            indices.Add(inner0); indices.Add(outer0); indices.Add(inner1);
            indices.Add(outer0); indices.Add(outer1); indices.Add(inner1);
        }

        mesh.Positions = positions;
        mesh.Normals = normals;
        mesh.TriangleIndices = indices;
        mesh.Freeze();
        return mesh;
    }

    private static MeshGeometry3D CreateTubeMesh(
        IReadOnlyList<Point3D> centerline,
        Func<double, double> radiusAt,
        int radialSegments,
        double radiusScale)
    {
        MeshGeometry3D mesh = new();
        Point3DCollection positions = [];
        Int32Collection indices = [];
        int longitudinalSegments = centerline.Count - 1;

        for (int i = 0; i < longitudinalSegments; i++)
        {
            for (int j = 0; j < radialSegments; j++)
            {
                int nextJ = (j + 1) % radialSegments;
                int a = i * radialSegments + j;
                int b = i * radialSegments + nextJ;
                int c = (i + 1) * radialSegments + j;
                int d = (i + 1) * radialSegments + nextJ;
                indices.Add(a); indices.Add(c); indices.Add(b);
                indices.Add(b); indices.Add(c); indices.Add(d);
            }
        }

        mesh.Positions = positions;
        mesh.TriangleIndices = indices;
        UpdateTubeMeshPositions(mesh, centerline, radiusAt, radialSegments, radiusScale);
        return mesh;
    }

    private static void UpdateTubeMeshPositions(
        MeshGeometry3D mesh,
        IReadOnlyList<Point3D> centerline,
        Func<double, double> radiusAt,
        int radialSegments,
        double radiusScale)
    {
        // Replacing the Positions collection once per frame is much cheaper than
        // raising a Freezable change notification for every individual vertex.
        Point3DCollection positions = [];
        int longitudinalSegments = centerline.Count - 1;

        for (int i = 0; i < centerline.Count; i++)
        {
            Vector3D tangent = i == 0
                ? centerline[1] - centerline[0]
                : i == centerline.Count - 1
                    ? centerline[^1] - centerline[^2]
                    : centerline[i + 1] - centerline[i - 1];
            if (tangent.LengthSquared < 1e-12)
                tangent = new Vector3D(1, 0, 0);
            tangent.Normalize();

            Vector3D reference = Math.Abs(Vector3D.DotProduct(tangent, new Vector3D(0, 0, 1))) < 0.90
                ? new Vector3D(0, 0, 1)
                : new Vector3D(0, 1, 0);
            Vector3D normal = Vector3D.CrossProduct(tangent, reference);
            if (normal.LengthSquared < 1e-12)
                normal = new Vector3D(0, 1, 0);
            normal.Normalize();
            Vector3D binormal = Vector3D.CrossProduct(tangent, normal);
            if (binormal.LengthSquared > 1e-12)
                binormal.Normalize();

            double t = i / (double)longitudinalSegments;
            double tubeRadius = Math.Max(0.0001, radiusAt(t) * radiusScale);
            for (int j = 0; j < radialSegments; j++)
            {
                double angle = 2.0 * Math.PI * j / radialSegments;
                Vector3D offset = normal * (tubeRadius * Math.Cos(angle))
                    + binormal * (tubeRadius * Math.Sin(angle));
                positions.Add(centerline[i] + offset);
            }
        }

        mesh.Positions = positions;
    }

    private void UpdateEventGeometry(ActivityEvent activityEvent, double age)
    {
        int count = activityEvent.BaseCenterline.Count;
        for (int i = 0; i < count; i++)
        {
            double t = i / (double)(count - 1);
            Point3D basePoint = activityEvent.BaseCenterline[i];
            Vector3D tangent = i == 0
                ? activityEvent.BaseCenterline[1] - basePoint
                : i == count - 1
                    ? basePoint - activityEvent.BaseCenterline[i - 1]
                    : activityEvent.BaseCenterline[i + 1] - activityEvent.BaseCenterline[i - 1];
            if (tangent.LengthSquared < 1e-12)
                tangent = new Vector3D(1, 0, 0);
            tangent.Normalize();

            // Waves travel along the tube at different rates. The envelope keeps
            // the attachment points anchored while the rest of the filament twists
            // and shimmers irregularly. Flares carry the motion outward from the limb.
            double envelope = activityEvent.IsFlare
                ? Math.Pow(t, 0.82)
                : Math.Sin(Math.PI * t);
            double waveA = Math.Sin(t * 12.8 - age * 4.2 + activityEvent.NoisePhaseA);
            double waveB = Math.Sin(t * 25.7 + age * 3.1 + activityEvent.NoisePhaseB);
            double waveC = Math.Sin(t * 6.1 - age * 2.2 + activityEvent.NoisePhaseC);
            double sideways = envelope * (0.028 * waveA + 0.013 * waveB + 0.014 * waveC);
            double depth = envelope * (
                0.010 * Math.Sin(t * 17.2 - age * 3.8 + activityEvent.NoisePhaseB)
                + 0.005 * Math.Sin(t * 31.0 + age * 2.7 + activityEvent.NoisePhaseA));

            Vector3D perpendicular = new(-tangent.Y, tangent.X, 0.0);
            if (perpendicular.LengthSquared < 1e-12)
                perpendicular = new Vector3D(1, 0, 0);
            else
                perpendicular.Normalize();

            activityEvent.AnimatedCenterline[i] = basePoint
                + perpendicular * sideways
                + new Vector3D(0, 0, depth);
        }

        UpdateTubeMeshPositions(
            activityEvent.OuterMesh,
            activityEvent.AnimatedCenterline,
            activityEvent.OuterRadiusAt,
            TubeRadialSegments,
            _activityThicknessScale);

        UpdateTubeMeshPositions(
            activityEvent.CoreMesh,
            activityEvent.AnimatedCenterline,
            activityEvent.CoreRadiusAt,
            TubeRadialSegments,
            _activityThicknessScale);
    }

    private static Color BlendWithWhite(Color color, double amount) => Color.FromRgb(
        ToByte(color.R + (255 - color.R) * amount),
        ToByte(color.G + (255 - color.G) * amount),
        ToByte(color.B + (255 - color.B) * amount));

    private static byte ToByte(double value) =>
        (byte)Math.Clamp((int)Math.Round(value), 0, 255);

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    private static double SmoothStep(double edge0, double edge1, double x)
    {
        double t = Math.Clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
        return t * t * (3.0 - 2.0 * t);
    }

    private static double Clamp(double value, double fallback, double minimum, double maximum) =>
        double.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : fallback;

    private sealed class ActivityEvent
    {
        public ActivityEvent(
            bool isFlare,
            double startTime,
            double duration,
            GeometryModel3D outerModel,
            GeometryModel3D coreModel,
            SolidColorBrush outerBrush,
            SolidColorBrush coreBrush,
            ScaleTransform3D scale,
            MeshGeometry3D outerMesh,
            MeshGeometry3D coreMesh,
            List<Point3D> baseCenterline,
            Func<double, double> outerRadiusAt,
            Func<double, double> coreRadiusAt,
            double noisePhaseA,
            double noisePhaseB,
            double noisePhaseC)
        {
            IsFlare = isFlare;
            StartTime = startTime;
            Duration = duration;
            OuterModel = outerModel;
            CoreModel = coreModel;
            OuterBrush = outerBrush;
            CoreBrush = coreBrush;
            Scale = scale;
            OuterMesh = outerMesh;
            CoreMesh = coreMesh;
            BaseCenterline = baseCenterline;
            AnimatedCenterline = new List<Point3D>(baseCenterline);
            OuterRadiusAt = outerRadiusAt;
            CoreRadiusAt = coreRadiusAt;
            NoisePhaseA = noisePhaseA;
            NoisePhaseB = noisePhaseB;
            NoisePhaseC = noisePhaseC;
            PulsePhase = isFlare ? 1.7 : 0.6;
        }

        public bool IsFlare { get; }
        public double StartTime { get; }
        public double Duration { get; }
        public GeometryModel3D OuterModel { get; }
        public GeometryModel3D CoreModel { get; }
        public SolidColorBrush OuterBrush { get; }
        public SolidColorBrush CoreBrush { get; }
        public ScaleTransform3D Scale { get; }
        public MeshGeometry3D OuterMesh { get; }
        public MeshGeometry3D CoreMesh { get; }
        public List<Point3D> BaseCenterline { get; }
        public List<Point3D> AnimatedCenterline { get; }
        public Func<double, double> OuterRadiusAt { get; }
        public Func<double, double> CoreRadiusAt { get; }
        public double NoisePhaseA { get; }
        public double NoisePhaseB { get; }
        public double NoisePhaseC { get; }
        public double PulsePhase { get; }
    }
}
