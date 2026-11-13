using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Infrastructure;

namespace RealmStudioX.WPF.ViewModels.Dialogs;

public class CoronaViewModel(Action? updateActiveCorona = null) : ViewModelBase
{
    private readonly Action? _updateActiveCorona = updateActiveCorona;

    private bool _enableProminences = true;

    public bool EnableProminences
    {
        get => _enableProminences;
        set
        {
            if (SetProperty(ref _enableProminences, value))
                UpdateActiveCorona();
        }
    }

    private bool _enableFlares = true;

    public bool EnableFlares
    {
        get => _enableFlares;
        set
        {
            if (SetProperty(ref _enableFlares, value))
                UpdateActiveCorona();
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
                UpdateActiveCorona();
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
                UpdateActiveCorona();
        }
    }

    private const double MinimumActivityGeometryScale = 0.10;
    private const double MaximumActivityGeometryScale = 1.0;
    private const double DefaultActivityGeometryScale = 1.0;

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
                UpdateActiveCorona();
        }
    }

    private const double MinimumActivityThicknessScale = 0.5;
    private const double MaximumActivityThicknessScale = 2.0;
    private const double DefaultActivityThicknessScale = 1.0;

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
                UpdateActiveCorona();
        }
    }


    #region Point Light

    private bool _enablePointLight = true;

    public bool EnablePointLight
    {
        get => _enablePointLight;
        set
        {
            if (SetProperty(ref _enablePointLight, value))
                UpdateActiveCorona();
        }
    }

    private const double MinimumPointLightIntensity = 0.0;
    private const double MaximumPointLightIntensity = 1.0;
    private const double DefaultPointLightIntensity = 0.1;
    private double _pointLightIntensity = DefaultPointLightIntensity;

    public double PointLightIntensity
    {
        get => _pointLightIntensity;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(value, MinimumPointLightIntensity, MaximumPointLightIntensity)
                : DefaultPointLightIntensity;

            if (SetProperty(ref _pointLightIntensity, value))
                UpdateActiveCorona();
        }
    }

    private const double MinimumPointLightRange = 0.1;
    private const double MaximumPointLightRange = 100.0;
    private const double DefaultPointLightRange = 10.0;
    private double _pointLightRange = DefaultPointLightRange;

    public double PointLightRange
    {
        get => _pointLightRange;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(value, MinimumPointLightRange, MaximumPointLightRange)
                : DefaultPointLightRange;

            if (SetProperty(ref _pointLightRange, value))
                UpdateActiveCorona();
        }
    }

    #endregion

    private void UpdateActiveCorona()
    {
        _updateActiveCorona?.Invoke();
    }
}
