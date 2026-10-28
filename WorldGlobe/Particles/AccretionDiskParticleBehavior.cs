using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.WorldGlobe.Particles;

/// <summary>
/// Emits deterministic particles that stream outward across the accretion disk.
/// OutflowSpeed is expressed in local scene units per second and is independent
/// of the particle's configured lifetime.
/// </summary>
internal sealed class AccretionDiskParticleBehavior : IDeterministicParticleBehavior
{
    private const double InnerRadiusMinimum = 1.04;
    private const double InnerRadiusVariation = 0.14;
    private const double DiskFadeStartRadius = 2.75;
    private const double DiskFadeEndRadius = 3.80;

    private double _outflowSpeed = 1.0;

    public double OutflowSpeed
    {
        get => _outflowSpeed;
        set => _outflowSpeed = double.IsFinite(value)
            ? Math.Clamp(value, 0.0, 5.0)
            : 1.0;
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

        if (!double.IsFinite(ageSeconds) ||
            !double.IsFinite(lifetimeSeconds) ||
            lifetimeSeconds <= 0.0 ||
            ageSeconds < 0.0 ||
            ageSeconds >= lifetimeSeconds)
        {
            return false;
        }

        // Normalized age is used only for life-cycle effects such as fade-in,
        // fade-out and size/color variation. It does not determine travel speed.
        double ageFraction = Math.Clamp(ageSeconds / lifetimeSeconds, 0.0, 1.0);

        double initialAngle = ParticleRandom.Sample(seed, particleIndex, 20)
            * Math.PI * 2.0;
        double phase = ParticleRandom.Sample(seed, particleIndex, 21)
            * Math.PI * 2.0;

        double initialRadius = InnerRadiusMinimum
            + ParticleRandom.Sample(seed, particleIndex, 22)
                * InnerRadiusVariation;

        // Speed is in scene units per second. Increasing lifetime therefore
        // lets a particle live longer without slowing its outward movement.
        double radius = initialRadius + OutflowSpeed * ageSeconds;

        // Give each trajectory slight, deterministic angular irregularity while
        // keeping its overall motion radial instead of orbital.
        double angle = initialAngle
            + 0.025 * Math.Sin(ageSeconds * 1.4 + phase)
            + 0.010 * Math.Sin(ageSeconds * 2.7 + phase * 1.7);

        double x = radius * Math.Cos(angle);
        double z = radius * Math.Sin(angle);

        double heightOffset =
            (ParticleRandom.Sample(seed, particleIndex, 23) - 0.5) * 0.10;
        double verticalWobble = Math.Sin(ageSeconds * 1.8 + phase) * 0.035;
        double y = heightOffset + verticalWobble;

        // Age fading is independent of outflow speed. A separate spatial fade
        // lets a particle dissolve as it leaves the visible accretion disk.
        double fadeIn = SmoothStep(ageFraction / 0.06);
        double fadeOut = Math.Pow(1.0 - ageFraction, 1.4);
        double edgeFade = 1.0 - SmoothStep(
            (radius - DiskFadeStartRadius) /
            (DiskFadeEndRadius - DiskFadeStartRadius));

        double opacityVariation = 0.55 + 0.45
            * ParticleRandom.Sample(seed, particleIndex, 24);

        double opacity = fadeIn * fadeOut * edgeFade * opacityVariation;

        double size = 0.014 + 0.022
            * ParticleRandom.Sample(seed, particleIndex, 25);

        // Color follows radial distance, so faster particles cool in color
        // more quickly because they travel farther—not because of their age.
        double radialFraction = Math.Clamp(
            (radius - InnerRadiusMinimum) /
            (DiskFadeEndRadius - InnerRadiusMinimum),
            0.0,
            1.0);

        Color color = GetParticleColor(radialFraction);

        state = new ParticleState(
            new Point3D(x, y, z),
            size,
            color,
            opacity);

        return true;
    }

    private static Color GetParticleColor(double radialFraction)
    {
        Color yellow = Color.FromRgb(255, 238, 105);
        Color orange = Color.FromRgb(255, 137, 38);
        Color red = Color.FromRgb(211, 40, 28);

        return radialFraction < 0.40
            ? BlendColors(yellow, orange, radialFraction / 0.40)
            : BlendColors(orange, red, (radialFraction - 0.40) / 0.60);
    }

    private static double SmoothStep(double value)
    {
        value = Math.Clamp(value, 0.0, 1.0);
        return value * value * (3.0 - 2.0 * value);
    }

    private static Color BlendColors(Color start, Color end, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);

        return Color.FromRgb(
            LerpByte(start.R, end.R, t),
            LerpByte(start.G, end.G, t),
            LerpByte(start.B, end.B, t));
    }

    private static byte LerpByte(byte start, byte end, double t) =>
        (byte)Math.Clamp(Math.Round(start + (end - start) * t), 0.0, 255.0);
}
