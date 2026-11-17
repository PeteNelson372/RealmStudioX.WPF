using HelixToolkit;
using HelixToolkit.Maths;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using RealmStudioX.Core;
using System.Numerics;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.Views.Celestial;

/// <summary>
/// Camera-facing solar activity for the SharpDX Sun renderer.
/// Events are scheduled deterministically from the celestial animation timeline.
/// Each strand uses an outer filament and a brighter core, with CPU-updated meshes.
/// </summary>
internal sealed class SolarActivityEffect3DX
{
    private const double DefaultActivityFrequency = 8.0;
    private const int MaximumConcurrentEvents = 18;
    private const uint InitialRandomState = 0x9E3779B9;

    private const int CurveSegments = 40;
    private const int TubeRadialSegments = 7;

    private const double OuterFilamentWidthScale = 1.18;

    private readonly GroupModel3D _root = new();
    private readonly List<ActivityEvent> _activeEvents = [];

    private Color _starColor = Colors.White;

    private bool _enableProminences = true;
    private bool _enableFlares = true;
    private bool _animationEnabled;

    private double _activityIntensity = 1.0;
    private double _activityFrequency = DefaultActivityFrequency;
    private double _activityGeometryScale = 0.30;
    private double _activityThicknessScale = 1.4;

    private bool _timelineInitialized;
    private bool _firstEmissionPending = true;

    private double _currentTime;
    private double _nextEmissionTime = double.PositiveInfinity;

    private uint _randomState = InitialRandomState;

    public Element3D Root => _root;

    internal int ActiveEventCount => _activeEvents.Count;

    public void Update(
        Color starColor,
        SunAnimationSettings settings,
        bool animationEnabled)
    {
        ArgumentNullException.ThrowIfNull(settings);

        bool wasEnabled = HasActivityEnabled;
        double oldFrequency = _activityFrequency;
        bool colorChanged = _starColor != starColor;

        _starColor = starColor;
        _animationEnabled = animationEnabled;
        _enableProminences = settings.EnableProminences;
        _enableFlares = settings.EnableFlares;

        _activityIntensity = Clamp(
            settings.ActivityIntensity, 1.0, 0.0, 2.0);

        _activityFrequency = Clamp(
            settings.ActivityFrequency, DefaultActivityFrequency, 0.0, 30.0);

        _activityGeometryScale = Clamp(
            settings.ActivityGeometryScale, 0.30, 0.1, 1.0);

        _activityThicknessScale = Clamp(
            settings.ActivityThicknessScale, 1.4, 0.5, 2.0);

        bool isEnabled = HasActivityEnabled;

        if (!isEnabled || _activityFrequency <= 0.0)
        {
            _nextEmissionTime = double.PositiveInfinity;
        }
        else if (_timelineInitialized &&
                 (!wasEnabled || oldFrequency <= 0.0 ||
                  oldFrequency != _activityFrequency))
        {
            ScheduleNextEmission(_currentTime);
        }

        if (colorChanged)
        {
            foreach (ActivityEvent activityEvent in _activeEvents)
                UpdateEventColors(activityEvent);
        }

        // Settings changes should affect already-active filaments immediately.
        ApplyCurrentEventOpacities();
    }

    public void AdvanceAnimation(CelestialAnimationFrame frame)
    {
        double time = double.IsFinite(frame.TimeSeconds)
            ? Math.Max(0.0, frame.TimeSeconds)
            : 0.0;

        // Support timeline restarts and playback scrubbing.
        if (_timelineInitialized && time < _currentTime - 1e-6)
            ResetAnimation();

        if (!_timelineInitialized)
        {
            _timelineInitialized = true;
            _currentTime = time;
            ScheduleNextEmission(time);
            return;
        }

        _currentTime = time;

        // Deterministic catch-up when frames are delayed.
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

        // Remove expired events and update the remaining geometry at absolute time.
        for (int i = _activeEvents.Count - 1; i >= 0; i--)
        {
            ActivityEvent activityEvent = _activeEvents[i];
            double age = time - activityEvent.StartTime;

            if (age >= activityEvent.Duration)
            {
                RemoveEventAt(i);
                continue;
            }

            UpdateEventAnimation(activityEvent, Math.Max(0.0, age));
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
    }

    private bool HasActivityEnabled =>
        _animationEnabled && (_enableProminences || _enableFlares);

    private void ScheduleNextEmission(double fromTime)
    {
        if (!HasActivityEnabled || _activityFrequency <= 0.0)
        {
            _nextEmissionTime = double.PositiveInfinity;
            return;
        }

        if (_firstEmissionPending)
        {
            _firstEmissionPending = false;
            _nextEmissionTime = fromTime + Lerp(1.8, 3.2, NextRandomUnit());
            return;
        }

        _nextEmissionTime = fromTime + SampleEmissionInterval();
    }

    private double SampleEmissionInterval()
    {
        double ratePerSecond = _activityFrequency / 60.0;
        if (ratePerSecond <= 0.0)
            return double.PositiveInfinity;

        // Exponential intervals produce irregular, Poisson-like event spacing.
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

        double anchorAngle = NextRandomUnit() * Math.PI * 2.0;
        int strandCount = NextRandomUnit() < 0.62 ? 2 : 3;
        double angularSpread = Lerp(0.035, 0.095, NextRandomUnit());

        for (int strand = 0; strand < strandCount; strand++)
        {
            double strandOffset =
                (strand / (double)(strandCount - 1) - 0.5) * angularSpread;

            double angle = anchorAngle + strandOffset
                + Lerp(-0.018, 0.018, NextRandomUnit());

            ActivityEvent activityEvent = CreateActivityEvent(
                isFlare, angle, startTime + strand * 0.025);

            if (_activeEvents.Count >= MaximumConcurrentEvents)
                RemoveEventAt(0);

            _activeEvents.Add(activityEvent);
            _root.Children.Add(activityEvent.OuterModel);
            _root.Children.Add(activityEvent.CoreModel);

            UpdateEventColors(activityEvent);
        }
    }

    private ActivityEvent CreateActivityEvent(
        bool isFlare,
        double angle,
        double startTime)
    {
        double width = Lerp(0.012, 0.027, NextRandomUnit());
        double planeBend = Lerp(-0.19, 0.19, NextRandomUnit());
        double noisePhaseA = NextRandomUnit() * Math.PI * 2.0;
        double noisePhaseB = NextRandomUnit() * Math.PI * 2.0;
        double noisePhaseC = NextRandomUnit() * Math.PI * 2.0;

        double length;
        double duration;
        double span = 0.0;

        if (isFlare)
        {
            length = Lerp(0.38, 0.86, NextRandomUnit());
            duration = Lerp(1.35, 2.75, NextRandomUnit());
        }
        else
        {
            span = Lerp(0.34, 0.82, NextRandomUnit());
            length = Lerp(0.22, 0.52, NextRandomUnit());
            duration = Lerp(3.0, 5.8, NextRandomUnit());
        }

        List<Vector3> baseCenterline = CreateBaseCenterline(
            isFlare,
            angle,
            length,
            span,
            planeBend,
            noisePhaseA,
            noisePhaseB,
            noisePhaseC);

        Func<double, double> outerRadius = isFlare
            ? t => width * (0.80 * Math.Pow(1.0 - t, 1.08) + 0.018)
            : t => width * (0.13 + 0.87 *
                Math.Pow(Math.Max(0.0, Math.Sin(Math.PI * t)), 0.43));

        Func<double, double> coreRadius = isFlare
            ? t => width * 0.31 * (0.80 * Math.Pow(1.0 - t, 1.05) + 0.016)
            : t => width * 0.32 * (0.10 + 0.90 *
                Math.Pow(Math.Max(0.0, Math.Sin(Math.PI * t)), 0.39));

        MeshGeometry3D outerMesh = CreateTubeMesh();
        MeshGeometry3D coreMesh = CreateTubeMesh();

        PhongMaterial outerMaterial = new()
        {
            DiffuseColor = new Color4(1, 1, 1, 0),
            EmissiveColor = new Color4(1, 1, 1, 0),
            SpecularColor = new Color4(0, 0, 0, 0)
        };

        PhongMaterial coreMaterial = new()
        {
            DiffuseColor = new Color4(1, 1, 1, 0),
            EmissiveColor = new Color4(1, 1, 1, 0),
            SpecularColor = new Color4(0, 0, 0, 0)
        };

        MeshGeometryModel3D outerModel = new()
        {
            Geometry = outerMesh,
            Material = outerMaterial,
            IsTransparent = true
        };

        MeshGeometryModel3D coreModel = new()
        {
            Geometry = coreMesh,
            Material = coreMaterial,
            IsTransparent = true
        };

        return new ActivityEvent(
            isFlare,
            angle,
            startTime,
            duration,
            outerModel,
            coreModel,
            outerMaterial,
            coreMaterial,
            outerMesh,
            coreMesh,
            baseCenterline,
            outerRadius,
            coreRadius,
            noisePhaseA,
            noisePhaseB,
            noisePhaseC);
    }

    private static List<Vector3> CreateBaseCenterline(
        bool isFlare,
        double angle,
        double length,
        double span,
        double planeBend,
        double noisePhaseA,
        double noisePhaseB,
        double noisePhaseC)
    {
        List<Vector3> centerline = new(CurveSegments + 1);

        for (int i = 0; i <= CurveSegments; i++)
        {
            double t = i / (double)CurveSegments;
            double radius;
            double localAngle;
            double depth;

            if (isFlare)
            {
                double radialTurbulence = t * (
                    0.035 * Math.Sin(t * 8.4 + noisePhaseA) +
                    0.017 * Math.Sin(t * 20.7 + noisePhaseB) +
                    0.009 * Math.Sin(t * 33.0 - noisePhaseC));

                double angularTurbulence = t * (
                    0.060 * Math.Sin(t * 4.7 + noisePhaseB) +
                    0.034 * Math.Sin(t * 13.6 + noisePhaseC) +
                    0.015 * Math.Sin(t * 27.0 + noisePhaseA));

                radius = 1.012 + length * t + radialTurbulence;
                localAngle = angle
                    + planeBend * Math.Sin(Math.PI * t)
                    + planeBend * 0.18 * t
                    + angularTurbulence;

                depth = 0.012
                    + 0.018 * Math.Sin(Math.PI * t)
                    + 0.020 * t * Math.Sin(t * 10.5 + noisePhaseC)
                    + 0.008 * t * Math.Sin(t * 24.0 + noisePhaseA);
            }
            else
            {
                double arch = Math.Pow(
                    Math.Max(0.0, Math.Sin(Math.PI * t)), 0.86);

                double radialTurbulence = arch * (
                    0.045 * Math.Sin(t * 13.2 + noisePhaseA) +
                    0.020 * Math.Sin(t * 25.5 + noisePhaseB) +
                    0.011 * Math.Sin(t * 37.0 + noisePhaseC));

                double angularTurbulence = arch * (
                    0.034 * Math.Sin(t * 7.1 + noisePhaseB) +
                    0.020 * Math.Sin(t * 17.0 + noisePhaseC));

                localAngle = angle + (t - 0.5) * span + angularTurbulence;
                radius = 1.012 + length * arch + radialTurbulence;
                depth = 0.012
                    + 0.014 * Math.Sin(Math.PI * t)
                    + 0.013 * arch * Math.Sin(t * 15.0 + noisePhaseC);
            }

            centerline.Add(new Vector3(
                (float)(radius * Math.Cos(localAngle)),
                (float)(radius * Math.Sin(localAngle)),
                (float)depth));
        }

        return centerline;
    }

    private static MeshGeometry3D CreateTubeMesh()
    {
        MeshGeometry3D mesh = new()
        {
            Positions = new Vector3Collection(),
            Normals = new Vector3Collection(),
            Indices = new IntCollection()
        };

        for (int i = 0; i < CurveSegments; i++)
        {
            for (int j = 0; j < TubeRadialSegments; j++)
            {
                int nextJ = (j + 1) % TubeRadialSegments;
                int a = i * TubeRadialSegments + j;
                int b = i * TubeRadialSegments + nextJ;
                int c = (i + 1) * TubeRadialSegments + j;
                int d = (i + 1) * TubeRadialSegments + nextJ;

                mesh.Indices.Add(a);
                mesh.Indices.Add(c);
                mesh.Indices.Add(b);

                mesh.Indices.Add(b);
                mesh.Indices.Add(c);
                mesh.Indices.Add(d);
            }
        }

        return mesh;
    }

    private static void UpdateTubeMeshPositions(
        MeshGeometry3D mesh,
        IReadOnlyList<Vector3> centerline,
        Func<double, double> radiusAt,
        double radiusScale)
    {
        Vector3Collection positions = new();
        Vector3Collection normals = new();
        int longitudinalSegments = centerline.Count - 1;

        for (int i = 0; i < centerline.Count; i++)
        {
            Vector3 tangent = i == 0
                ? centerline[1] - centerline[0]
                : i == centerline.Count - 1
                    ? centerline[^1] - centerline[^2]
                    : centerline[i + 1] - centerline[i - 1];

            if (tangent.LengthSquared() < 1e-12f)
                tangent = Vector3.UnitX;
            else
                tangent = Vector3.Normalize(tangent);

            Vector3 reference =
                Math.Abs(Vector3.Dot(tangent, Vector3.UnitZ)) < 0.90f
                    ? Vector3.UnitZ
                    : Vector3.UnitY;

            Vector3 normal = Vector3.Cross(tangent, reference);
            if (normal.LengthSquared() < 1e-12f)
                normal = Vector3.UnitY;
            else
                normal = Vector3.Normalize(normal);

            Vector3 binormal = Vector3.Cross(tangent, normal);
            if (binormal.LengthSquared() < 1e-12f)
                binormal = Vector3.UnitZ;
            else
                binormal = Vector3.Normalize(binormal);

            double t = i / (double)longitudinalSegments;
            float tubeRadius = (float)Math.Max(
                0.0001, radiusAt(t) * radiusScale);

            for (int j = 0; j < TubeRadialSegments; j++)
            {
                double radialAngle =
                    2.0 * Math.PI * j / TubeRadialSegments;

                Vector3 radial =
                    normal * (float)Math.Cos(radialAngle) +
                    binormal * (float)Math.Sin(radialAngle);

                positions.Add(centerline[i] + radial * tubeRadius);
                normals.Add(radial);
            }
        }

        mesh.Positions = positions;
        mesh.Normals = normals;
    }

    private void UpdateEventAnimation(ActivityEvent activityEvent, double age)
    {
        UpdateEventGeometry(activityEvent, age);

        double progress = Math.Clamp(age / activityEvent.Duration, 0.0, 1.0);
        double fadeIn = SmoothStep(
            0.0, activityEvent.IsFlare ? 0.10 : 0.16, progress);

        double fadeOut = activityEvent.IsFlare
            ? 1.0 - SmoothStep(0.34, 1.0, progress)
            : 1.0 - SmoothStep(0.64, 1.0, progress);

        double pulse = 0.88 + 0.12 * Math.Sin(
            age * (activityEvent.IsFlare ? 9.0 : 3.1)
            + activityEvent.PulsePhase);

        double opacity = Math.Clamp(
            fadeIn * fadeOut * pulse * _activityIntensity, 0.0, 1.0);

        bool enabled = activityEvent.IsFlare
            ? _enableFlares
            : _enableProminences;

        if (!enabled || !_animationEnabled)
            opacity = 0.0;

        double growth = activityEvent.IsFlare
            ? Lerp(0.24, 1.0, SmoothStep(0.0, 0.22, progress))
            : Lerp(0.22, 1.0, SmoothStep(0.0, 0.34, progress));

        double eventScale = _activityGeometryScale * growth;
        Vector3 anchor = new(
            (float)Math.Cos(activityEvent.Angle),
            (float)Math.Sin(activityEvent.Angle),
            0.0f);

        List<Vector3> scaledCenterline = new(
            activityEvent.AnimatedCenterline.Count);

        foreach (Vector3 point in activityEvent.AnimatedCenterline)
            scaledCenterline.Add(anchor + (point - anchor) * (float)eventScale);

        double tubeScale = _activityThicknessScale * eventScale;

        UpdateTubeMeshPositions(
            activityEvent.OuterMesh,
            scaledCenterline,
            activityEvent.OuterRadiusAt,
            tubeScale * OuterFilamentWidthScale);

        UpdateTubeMeshPositions(
            activityEvent.CoreMesh,
            scaledCenterline,
            activityEvent.CoreRadiusAt,
            tubeScale);

        activityEvent.OuterMaterial.DiffuseColor = ToColor4(
            activityEvent.OuterColor,
            opacity * (activityEvent.IsFlare ? 0.58 : 0.48));

        activityEvent.OuterMaterial.EmissiveColor =
            activityEvent.OuterMaterial.DiffuseColor;

        activityEvent.CoreMaterial.DiffuseColor = ToColor4(
            activityEvent.CoreColor,
            opacity * (activityEvent.IsFlare ? 1.0 : 0.92));

        activityEvent.CoreMaterial.EmissiveColor =
            activityEvent.CoreMaterial.DiffuseColor;
    }

    private void UpdateEventGeometry(ActivityEvent activityEvent, double age)
    {
        int count = activityEvent.BaseCenterline.Count;

        for (int i = 0; i < count; i++)
        {
            double t = i / (double)(count - 1);
            Vector3 basePoint = activityEvent.BaseCenterline[i];

            Vector3 tangent = i == 0
                ? activityEvent.BaseCenterline[1] - basePoint
                : i == count - 1
                    ? basePoint - activityEvent.BaseCenterline[i - 1]
                    : activityEvent.BaseCenterline[i + 1]
                        - activityEvent.BaseCenterline[i - 1];

            if (tangent.LengthSquared() < 1e-12f)
                tangent = Vector3.UnitX;
            else
                tangent = Vector3.Normalize(tangent);

            // Travelling waves deform the strand while keeping its ends anchored.
            double envelope = activityEvent.IsFlare
                ? Math.Pow(t, 0.82)
                : Math.Sin(Math.PI * t);

            double waveA = Math.Sin(
                t * 12.8 - age * 4.2 + activityEvent.NoisePhaseA);
            double waveB = Math.Sin(
                t * 25.7 + age * 3.1 + activityEvent.NoisePhaseB);
            double waveC = Math.Sin(
                t * 6.1 - age * 2.2 + activityEvent.NoisePhaseC);

            double sideways = envelope *
                (0.028 * waveA + 0.013 * waveB + 0.014 * waveC);

            double depth = envelope * (
                0.010 * Math.Sin(
                    t * 17.2 - age * 3.8 + activityEvent.NoisePhaseB)
                + 0.005 * Math.Sin(
                    t * 31.0 + age * 2.7 + activityEvent.NoisePhaseA));

            Vector3 perpendicular = new(-tangent.Y, tangent.X, 0.0f);
            if (perpendicular.LengthSquared() < 1e-12f)
                perpendicular = Vector3.UnitX;
            else
                perpendicular = Vector3.Normalize(perpendicular);

            activityEvent.AnimatedCenterline[i] = basePoint
                + perpendicular * (float)sideways
                + Vector3.UnitZ * (float)depth;
        }
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
        activityEvent.OuterColor = BlendWithWhite(
            _starColor, activityEvent.IsFlare ? 0.34 : 0.28);

        activityEvent.CoreColor = BlendWithWhite(
            _starColor, activityEvent.IsFlare ? 0.92 : 0.82);
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
        // Xorshift32: identical sequence after every reset.
        uint x = _randomState;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        _randomState = x == 0 ? InitialRandomState : x;
        return (_randomState + 0.5) / (uint.MaxValue + 1.0);
    }

    private static Color BlendWithWhite(Color color, double amount) =>
        Color.FromRgb(
            ToByte(color.R + (255 - color.R) * amount),
            ToByte(color.G + (255 - color.G) * amount),
            ToByte(color.B + (255 - color.B) * amount));

    private static byte ToByte(double value) =>
        (byte)Math.Clamp((int)Math.Round(value), 0, 255);

    private static Color4 ToColor4(Color color, double opacity)
    {
        float alpha = (float)Math.Clamp(opacity * color.ScA, 0.0, 1.0);
        return new Color4(color.ScR, color.ScG, color.ScB, alpha);
    }

    private static double Lerp(double a, double b, double t) =>
        a + (b - a) * t;

    private static double SmoothStep(double edge0, double edge1, double value)
    {
        double t = Math.Clamp((value - edge0) / (edge1 - edge0), 0.0, 1.0);
        return t * t * (3.0 - 2.0 * t);
    }

    private static double Clamp(
        double value,
        double fallback,
        double minimum,
        double maximum) =>
        double.IsFinite(value)
            ? Math.Clamp(value, minimum, maximum)
            : fallback;

    private sealed class ActivityEvent
    {
        public ActivityEvent(
            bool isFlare,
            double angle,
            double startTime,
            double duration,
            MeshGeometryModel3D outerModel,
            MeshGeometryModel3D coreModel,
            PhongMaterial outerMaterial,
            PhongMaterial coreMaterial,
            MeshGeometry3D outerMesh,
            MeshGeometry3D coreMesh,
            List<Vector3> baseCenterline,
            Func<double, double> outerRadiusAt,
            Func<double, double> coreRadiusAt,
            double noisePhaseA,
            double noisePhaseB,
            double noisePhaseC)
        {
            IsFlare = isFlare;
            Angle = angle;
            StartTime = startTime;
            Duration = duration;
            OuterModel = outerModel;
            CoreModel = coreModel;
            OuterMaterial = outerMaterial;
            CoreMaterial = coreMaterial;
            OuterMesh = outerMesh;
            CoreMesh = coreMesh;
            BaseCenterline = baseCenterline;
            AnimatedCenterline = new List<Vector3>(baseCenterline);
            OuterRadiusAt = outerRadiusAt;
            CoreRadiusAt = coreRadiusAt;
            NoisePhaseA = noisePhaseA;
            NoisePhaseB = noisePhaseB;
            NoisePhaseC = noisePhaseC;
            PulsePhase = isFlare ? 1.7 : 0.6;
        }

        public bool IsFlare { get; }
        public double Angle { get; }
        public double StartTime { get; }
        public double Duration { get; }

        public MeshGeometryModel3D OuterModel { get; }
        public MeshGeometryModel3D CoreModel { get; }
        public PhongMaterial OuterMaterial { get; }
        public PhongMaterial CoreMaterial { get; }
        public MeshGeometry3D OuterMesh { get; }
        public MeshGeometry3D CoreMesh { get; }

        public List<Vector3> BaseCenterline { get; }
        public List<Vector3> AnimatedCenterline { get; }

        public Func<double, double> OuterRadiusAt { get; }
        public Func<double, double> CoreRadiusAt { get; }

        public double NoisePhaseA { get; }
        public double NoisePhaseB { get; }
        public double NoisePhaseC { get; }
        public double PulsePhase { get; }

        public Color OuterColor { get; set; }
        public Color CoreColor { get; set; }
    }
}
