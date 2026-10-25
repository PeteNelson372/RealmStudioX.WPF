using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.WorldGlobe.Particles;

internal sealed class AccretionDiskParticleBehavior : IDeterministicParticleBehavior
{
    public bool TryEvaluate(
        long particleIndex,
        double emissionTimeSeconds,
        double ageSeconds,
        double lifetimeSeconds,
        int seed,
        out ParticleState state)
    {
        state = default;

        if (lifetimeSeconds <= 0.0 ||
            ageSeconds < 0.0 ||
            ageSeconds >= lifetimeSeconds)
        {
            return false;
        }

        double t = Math.Clamp(
            ageSeconds / lifetimeSeconds, 0.0, 1.0);

        // Each particle has a stable launch angle.
        double launchAngle =
            ParticleRandom.Sample(seed, particleIndex, 20)
            * Math.PI * 2.0;

        double phase =
            ParticleRandom.Sample(seed, particleIndex, 21)
            * Math.PI * 2.0;

        // Begin just outside the event horizon.
        double initialRadius = 1.04
            + ParticleRandom.Sample(seed, particleIndex, 22) * 0.14;

        // Stream outward rather than orbiting around the black hole.
        double outerRadius = 3.20;

        double radius = initialRadius
            + (outerRadius - initialRadius) * t;

        // Minimal angular wobble; the overall trajectory remains radial.
        double angle = launchAngle
            + 0.025 * Math.Sin(ageSeconds * 1.4 + phase)
            + 0.010 * Math.Sin(ageSeconds * 2.7 + phase * 1.7);

        double x = radius * Math.Cos(angle);
        double z = radius * Math.Sin(angle);

        // Small, deterministic variation above and below the disk plane.
        double initialHeight =
            (ParticleRandom.Sample(seed, particleIndex, 23) - 0.5)
            * 0.10;

        double heightWobble =
            Math.Sin(ageSeconds * 1.8 + phase) * 0.025;

        double y = initialHeight + heightWobble;

        double fadeIn = SmoothStep(t / 0.06);
        double fadeOut = Math.Pow(1.0 - t, 1.4);

        double opacityVariation =
            0.55 + 0.45 *
            ParticleRandom.Sample(seed, particleIndex, 24);

        double opacity =
            fadeIn * fadeOut * opacityVariation;

        double size =
            0.014 +
            0.022 * ParticleRandom.Sample(seed, particleIndex, 25);

        // The particles become cooler in color as they move outward.
        Color color = GetParticleColor(t);

        state = new ParticleState(
            new Point3D(x, y, z),
            size,
            color,
            opacity);

        return true;
    }

    private static Color GetParticleColor(double t)
    {
        Color yellow = Color.FromRgb(255, 238, 105);
        Color orange = Color.FromRgb(255, 137, 38);
        Color red = Color.FromRgb(211, 40, 28);

        if (t < 0.40)
            return BlendColors(yellow, orange, t / 0.40);

        return BlendColors(
            orange,
            red,
            (t - 0.40) / 0.60);
    }

    private static double SmoothStep(double value)
    {
        value = Math.Clamp(value, 0.0, 1.0);
        return value * value * (3.0 - 2.0 * value);
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
}