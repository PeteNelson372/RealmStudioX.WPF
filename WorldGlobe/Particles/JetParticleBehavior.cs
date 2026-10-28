using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.WorldGlobe.Particles;

internal sealed class JetParticleBehavior : IDeterministicParticleBehavior
{
    private readonly bool _north;
    private double _jetLength;
    private double _speed;
    private double _dispersion;

    public double JetLength
    {
        get => _jetLength;
        set => _jetLength = ClampFinite(value, 8.4, 0.1, 30.0);
    }

    public double Speed
    {
        get => _speed;
        set => _speed = ClampFinite(value, 2.625, 0.0, 20.0);
    }

    public double Dispersion
    {
        get => _dispersion;
        set => _dispersion = ClampFinite(value, 1.0, 0.0, 4.0);
    }

    public JetParticleBehavior(
        bool north,
        double jetLength = 8.4,
        double speed = 2.625,
        double dispersion = 1.0)
    {
        _north = north;
        JetLength = jetLength;
        Speed = speed;
        Dispersion = dispersion;
    }

    public bool TryEvaluate(
        long particleIndex,
        double emissionTimeSeconds,
        double ageSeconds,
        double lifetimeSeconds,
        int seed,
        out ParticleState state)
    {
        state = default;

        if (lifetimeSeconds <= 0.0 || ageSeconds < 0.0 || ageSeconds >= lifetimeSeconds)
            return false;

        double ageFraction = Math.Clamp(ageSeconds / lifetimeSeconds, 0.0, 1.0);
        double direction = _north ? 1.0 : -1.0;
        const double startY = 0.83;

        double distanceTravelled = Speed * ageSeconds;
        double distanceFraction = distanceTravelled / JetLength;

        if (distanceFraction >= 1.0)
            return false;

        double y = direction * (startY + distanceTravelled);

        double lateralAngle =
            ParticleRandom.Sample(seed, particleIndex, 10) * Math.PI * 2.0;

        double spreadFactor = Math.Sqrt(
            ParticleRandom.Sample(seed, particleIndex, 11));

        double spread = (0.025 + 0.38 * distanceFraction) * Dispersion;
        double lateralDistance = spread * spreadFactor;

        double phase =
            ParticleRandom.Sample(seed, particleIndex, 12) * Math.PI * 2.0;

        double wobble = 0.025 * distanceFraction * Dispersion;
        double x = Math.Cos(lateralAngle) * lateralDistance
            + Math.Sin(ageSeconds * 2.1 + phase) * wobble;
        double z = Math.Sin(lateralAngle) * lateralDistance
            + Math.Cos(ageSeconds * 1.7 + phase) * wobble;

        double fadeIn = SmoothStep(ageFraction / 0.06);
        double fadeOut = Math.Pow(1.0 - Math.Clamp(
            Math.Max(ageFraction, distanceFraction), 0.0, 1.0), 2.0);

        double opacityVariation = 0.65 + 0.35 *
            ParticleRandom.Sample(seed, particleIndex, 13);

        double opacity = fadeIn * fadeOut * opacityVariation;
        double size = 0.018 + 0.034 * ParticleRandom.Sample(seed, particleIndex, 14);

        double colorVariation = ParticleRandom.Sample(seed, particleIndex, 15);
        Color color = colorVariation < 0.25
            ? Color.FromRgb(211, 220, 255)
            : Color.FromRgb(255, 255, 255);

        state = new ParticleState(new Point3D(x, y, z), size, color, opacity);
        return true;
    }

    private static double ClampFinite(double value, double fallback, double minimum, double maximum) =>
        double.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : fallback;

    private static double SmoothStep(double value)
    {
        value = Math.Clamp(value, 0.0, 1.0);
        return value * value * (3.0 - 2.0 * value);
    }
}
