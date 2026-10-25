namespace RealmStudioX.WPF.WorldGlobe.Particles;

internal interface IDeterministicParticleBehavior
{
    bool TryEvaluate(
        long particleIndex,
        double emissionTimeSeconds,
        double ageSeconds,
        double lifetimeSeconds,
        int seed,
        out ParticleState state);
}