using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Infrastructure;

namespace RealmStudioX.WPF.ViewModels.Dialogs;

public class SunViewModel(WorldGlobeSceneViewModel globeSceneViewModel) : ViewModelBase
{
    private readonly WorldGlobeSceneViewModel _globeSceneViewModel =
        globeSceneViewModel ?? throw new ArgumentNullException(nameof(globeSceneViewModel));

    private bool _enableProminences = true;

    public bool EnableProminences
    {
        get => _enableProminences;
        set
        {
            if (SetProperty(ref _enableProminences, value))
                UpdateActiveSun();
        }
    }

    private bool _enableFlares = true;

    public bool EnableFlares
    {
        get => _enableFlares;
        set
        {
            if (SetProperty(ref _enableFlares, value))
                UpdateActiveSun();
        }
    }

    private bool _enableCorona = true;

    public bool EnableCorona
    {
        get => _enableCorona;
        set
        {
            if (SetProperty(ref _enableCorona, value))
                UpdateActiveSun();
        }
    }

    private const double MinimumCoronaOpacity = 0.05;
    private const double MaximumCoronaOpacity = 0.30;
    private const double DefaultCoronaOpacity = 0.18;

    private double _coronaOpacity = DefaultCoronaOpacity;

    public double CoronaOpacity
    {
        get => _coronaOpacity;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(value, MinimumCoronaOpacity, MaximumCoronaOpacity)
                : DefaultActivityIntensity;

            if (SetProperty(ref _coronaOpacity, value))
                UpdateActiveSun();
        }
    }

    private const double MinimumCoronaRotationSpeed = 0.0;
    private const double MaximumCoronaRotationSpeed = 25.0;
    private const double DefaultCoronaRotationSpeed = 12.0;

    private double _coronaRotationSpeed = DefaultCoronaRotationSpeed;

    public double CoronaRotationSpeed
    {
        get => _coronaRotationSpeed;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(value, MinimumCoronaRotationSpeed, MaximumCoronaRotationSpeed)
                : DefaultActivityIntensity;

            if (SetProperty(ref _coronaRotationSpeed, value))
                UpdateActiveSun();
        }
    }

    private const double MinimumActivityIntensity = 0.0;
    private const double MaximumActivityIntensity = 2.0;
    private const double DefaultActivityIntensity = 1.0;

    private double _activityIntensity = DefaultActivityIntensity;

    public double ActivityIntensity
    {
        get => _activityIntensity;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(value, MinimumActivityIntensity, MaximumActivityIntensity)
                : DefaultActivityIntensity;

            if (SetProperty(ref _activityIntensity, value))
                UpdateActiveSun();
        }
    }

    private const double MinimumActivityFrequency = 0.0;
    private const double MaximumActivityFrequency = 30.0;
    private const double DefaultActivityFrequency = 8.0;

    private double _activityFrequency = DefaultActivityFrequency;

    /// <summary>Mean number of new flare/prominence events per minute. Zero disables emissions.</summary>
    public double ActivityFrequency
    {
        get => _activityFrequency;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(value, MinimumActivityFrequency, MaximumActivityFrequency)
                : DefaultActivityFrequency;

            if (SetProperty(ref _activityFrequency, value))
                UpdateActiveSun();
        }
    }

    private const double MinimumActivityGeometryScale = 0.1;
    private const double MaximumActivityGeometryScale = 1.0;
    private const double DefaultActivityGeometryScale = 0.30;

    private double _activityGeometryScale = DefaultActivityGeometryScale;

    public double ActivityGeometryScale
    {
        get => _activityGeometryScale;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(value, MinimumActivityGeometryScale, MaximumActivityGeometryScale)
                : DefaultActivityFrequency;

            if (SetProperty(ref _activityGeometryScale, value))
                UpdateActiveSun();
        }
    }

    private const double MinimumActivityThicknessScale = 0.5;
    private const double MaximumActivityThicknessScale = 2.0;
    private const double DefaultActivityThicknessScale = 1.4;

    private double _activityThicknessScale = DefaultActivityThicknessScale;

    public double ActivityThicknessScale
    {
        get => _activityThicknessScale;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(value, MinimumActivityThicknessScale, MaximumActivityThicknessScale)
                : DefaultActivityFrequency;

            if (SetProperty(ref _activityThicknessScale, value))
                UpdateActiveSun();
        }
    }

    private void UpdateActiveSun()
    {
        if (_globeSceneViewModel.StarViewModel.SelectedLocalStarType == LocalStarType.Sun)
            _globeSceneViewModel.StarViewModel.UpdateCurrentLocalStar();
    }
}
