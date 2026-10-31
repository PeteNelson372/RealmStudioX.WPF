namespace RealmStudioX.WPF.WorldGlobe;

/// <summary>
/// Appearance and event-frequency settings for the Corona local-star type.
/// Animation enablement remains global to WorldGlobeSceneViewModel.
/// </summary>
public sealed class CoronaSettings : SolarActivitySettings, ILocalStarDetailSettings
{
    public CoronaSettings Clone() => new()
    {
        EnableProminences = EnableProminences,
        EnableFlares = EnableFlares,
        ActivityIntensity = ActivityIntensity,
        ActivityFrequency = ActivityFrequency,
        ActivityGeometryScale = ActivityGeometryScale,
        ActivityThicknessScale = ActivityThicknessScale
    };
}
