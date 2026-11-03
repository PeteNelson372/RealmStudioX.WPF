namespace RealmStudioX.WPF.Views.Celestial;

public sealed class RingTurbulenceSettings : ICelestialBodyAnimationSettings
{
    public double Intensity { get; set; } = 0.2;
    public double UpdateRate { get; set; } = 30.0;

    public ICelestialBodyAnimationSettings Clone()
    {
        return new RingTurbulenceSettings
        {
            Intensity = Intensity,
            UpdateRate = UpdateRate,
        };
    }
}