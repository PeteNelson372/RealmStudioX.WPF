namespace RealmStudioX.WPF.WorldGlobe.Particles;

internal sealed class ParticleEmitterSettings
{
    public double EmissionRate { get; }
    public double LifetimeSeconds { get; }
    public int MaxParticles { get; }
    public int Seed { get; }

    // Maximum fraction of one emission interval used to delay
    // an individual emission. Keep below 0.5 to preserve ordering.
    public double EmissionJitterFraction { get; }

    // Fractional variation around the configured particle lifetime.
    public double LifetimeVariation { get; }

    public ParticleEmitterSettings(
        double emissionRate,
        double lifetimeSeconds,
        int maxParticles,
        int seed,
        double emissionJitterFraction = 0.25,
        double lifetimeVariation = 0.15)
    {
        if (!double.IsFinite(emissionRate) || emissionRate < 0.0)
            throw new ArgumentOutOfRangeException(nameof(emissionRate));

        if (!double.IsFinite(lifetimeSeconds) || lifetimeSeconds <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(lifetimeSeconds));

        if (maxParticles <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxParticles));

        if (!double.IsFinite(emissionJitterFraction) ||
            emissionJitterFraction < 0.0 ||
            emissionJitterFraction >= 0.5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(emissionJitterFraction));
        }

        if (!double.IsFinite(lifetimeVariation) ||
            lifetimeVariation < 0.0 ||
            lifetimeVariation >= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lifetimeVariation));
        }

        EmissionRate = emissionRate;
        LifetimeSeconds = lifetimeSeconds;
        MaxParticles = maxParticles;
        Seed = seed;
        EmissionJitterFraction = emissionJitterFraction;
        LifetimeVariation = lifetimeVariation;
    }
}