namespace RealmStudioX.WPF.Views.Celestial;

public sealed class BlackHoleAnimationSettings : ICelestialBodyAnimationSettings
{
    public bool EnableJets { get; set; } = true;

    public double JetLength { get; set; } = 8.4;
    public double JetPulseRate { get; set; } = 0.325;

    public bool EnableJetParticles { get; set; } = true;
    public double JetParticleSpeed { get; set; } = 2.625;
    public double JetParticleEmissionRate { get; set; } = 22.0;
    public double JetParticleLifetime { get; set; } = 3.2;
    public double JetParticleDispersion { get; set; } = 1.0;

    public bool EnableDiskParticles { get; set; } = true;
    public double DiskParticleEmissionRate { get; set; } = 6.0;
    public double DiskParticleLifetime { get; set; } = 2.5;
    public double DiskParticleOutflowSpeed { get; set; } = 1.0;

    public ICelestialBodyAnimationSettings Clone()
    {
        return new BlackHoleAnimationSettings
        {
            EnableJets = EnableJets,
            JetLength = JetLength,
            JetPulseRate = JetPulseRate,

            EnableJetParticles = EnableJetParticles,
            JetParticleSpeed = JetParticleSpeed,
            JetParticleEmissionRate = JetParticleEmissionRate,
            JetParticleLifetime = JetParticleLifetime,
            JetParticleDispersion = JetParticleDispersion,

            EnableDiskParticles = EnableDiskParticles,
            DiskParticleEmissionRate = DiskParticleEmissionRate,
            DiskParticleLifetime = DiskParticleLifetime,
            DiskParticleOutflowSpeed = DiskParticleOutflowSpeed
        };
    }
}