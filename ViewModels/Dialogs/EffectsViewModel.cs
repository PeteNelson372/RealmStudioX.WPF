
using RealmStudioX._3D.ShaderEffects;
using RealmStudioX._3D.Views.Controls;
using RealmStudioX.Core.Celestial;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.ViewModels.Dialogs;

public sealed class EffectsViewModel : ViewModelBase
{
    private readonly ModelViewerControl _modelViewer;
    private bool _isRestoringSettings;

    public EffectsViewModel(ModelViewerControl modelViewer)
    {
        _modelViewer = modelViewer
            ?? throw new ArgumentNullException(nameof(modelViewer));

        ApplyEnabledEffects();

        ApplyBackgroundScintillation();
    }

    #region Blur

    private bool _enableBlur;

    public bool EnableBlur
    {
        get => _enableBlur;
        set
        {
            if (SetProperty(ref _enableBlur, value))
                ApplyEnabledEffects();
        }
    }

    private double _blurAmount = 5.0;

    public double BlurAmount
    {
        get => _blurAmount;
        set
        {
            value = ClampFinite(value, 0.0, 100.0, 5.0);

            if (SetProperty(ref _blurAmount, value))
                ApplyEnabledEffects();
        }
    }

    #endregion

    #region Bleach Bypass

    private bool _enableBleachBypass;

    public bool EnableBleachBypass
    {
        get => _enableBleachBypass;
        set
        {
            if (SetProperty(ref _enableBleachBypass, value))
                ApplyEnabledEffects();
        }
    }

    private double _bleachBypassOpacity = 50.0;

    public double BleachBypassOpacity
    {
        get => _bleachBypassOpacity;
        set
        {
            value = ClampFinite(value, 0.0, 100.0, 50.0);

            if (SetProperty(ref _bleachBypassOpacity, value))
                ApplyEnabledEffects();
        }
    }

    #endregion

    #region Bloom

    private bool _enableBloom;

    public bool EnableBloom
    {
        get => _enableBloom;
        set
        {
            if (SetProperty(ref _enableBloom, value))
                ApplyEnabledEffects();
        }
    }

    private double _bloomBlurWidth = 1.0;

    public double BloomBlurWidth
    {
        get => _bloomBlurWidth;
        private set
        {
            value = ClampFinite(value, 0.0, 10.0, 1.0);

            if (SetProperty(ref _bloomBlurWidth, value))
                ApplyEnabledEffects();
        }
    }

    private double _bloomIntensity = 0.25;

    public double BloomIntensity
    {
        get => _bloomIntensity;
        set
        {
            value = ClampFinite(value, 0.0, 2.0, 0.25);

            if (SetProperty(ref _bloomIntensity, value))
                ApplyEnabledEffects();
        }
    }

    private double _bloomHighlightThreshold = 0.90;

    public double BloomHighlightThreshold
    {
        get => _bloomHighlightThreshold;
        private set
        {
            value = ClampFinite(value, 0.0, 1.0, 0.90);

            if (SetProperty(ref _bloomHighlightThreshold, value))
                ApplyEnabledEffects();
        }
    }

    private double _bloomHighlightIntensity = 0.15;

    public double BloomHighlightIntensity
    {
        get => _bloomHighlightIntensity;
        private set
        {
            value = ClampFinite(value, 0.0, 2.0, 0.15);

            if (SetProperty(ref _bloomHighlightIntensity, value))
                ApplyEnabledEffects();
        }
    }

    #endregion

    #region Sepia

    private bool _enableSepia;

    public bool EnableSepia
    {
        get => _enableSepia;
        set
        {
            if (SetProperty(ref _enableSepia, value))
                ApplyEnabledEffects();
        }
    }

    private double _sepiaAmount = 50.0;

    public double SepiaAmount
    {
        get => _sepiaAmount;
        set
        {
            value = ClampFinite(value, 0.0, 100.0, 50.0);

            if (SetProperty(ref _sepiaAmount, value))
                ApplyEnabledEffects();
        }
    }

    #endregion

    #region Grayscale

    private bool _enableGrayscale;

    public bool EnableGrayscale
    {
        get => _enableGrayscale;
        set
        {
            if (SetProperty(ref _enableGrayscale, value))
                ApplyEnabledEffects();
        }
    }

    private double _desaturationFactor = 50.0;

    public double DesaturationFactor
    {
        get => _desaturationFactor;
        set
        {
            value = ClampFinite(value, 0.0, 100.0, 50.0);

            if (SetProperty(ref _desaturationFactor, value))
                ApplyEnabledEffects();
        }
    }

    #endregion


    #region Lens Flare

    private bool _enableLensFlare;

    public bool EnableLensFlare
    {
        get => _enableLensFlare;
        set
        {
            if (SetProperty(ref _enableLensFlare, value))
                ApplyEnabledEffects();
        }
    }

    private double _lensFlareIntensity = 0.8;

    public double LensFlareIntensity
    {
        get => _lensFlareIntensity;
        set
        {
            value = ClampFinite(value, 0.0, 3.0, 0.8);

            if (SetProperty(ref _lensFlareIntensity, value))
                ApplyEnabledEffects();
        }
    }

    private double _lensFlareGlowSize = 0.035;

    public double LensFlareGlowSize
    {
        get => _lensFlareGlowSize;
        set
        {
            value = ClampFinite(value, 0.005, 0.15, 0.035);

            if (SetProperty(ref _lensFlareGlowSize, value))
                ApplyEnabledEffects();
        }
    }

    private double _lensFlareStreakLength = 0.65;

    public double LensFlareStreakLength
    {
        get => _lensFlareStreakLength;
        set
        {
            value = ClampFinite(value, 0.05, 1.5, 0.65);

            if (SetProperty(ref _lensFlareStreakLength, value))
                ApplyEnabledEffects();
        }
    }

    private double _lensFlareGhostIntensity = 0.35;

    public double LensFlareGhostIntensity
    {
        get => _lensFlareGhostIntensity;
        set
        {
            value = ClampFinite(value, 0.0, 1.0, 0.35);

            if (SetProperty(ref _lensFlareGhostIntensity, value))
                ApplyEnabledEffects();
        }
    }

    private double _lensFlareStarburstIntensity = 0.2;

    public double LensFlareStarburstIntensity
    {
        get => _lensFlareStarburstIntensity;
        set
        {
            value = ClampFinite(value, 0.0, 1.0, 0.2);

            if (SetProperty(ref _lensFlareStarburstIntensity, value))
                ApplyEnabledEffects();
        }
    }

    private LensFlareCoreEffect? _lensFlareCoreEffect;
    private LensFlareStreakEffect? _lensFlareStreakEffect;
    private LensFlareGhostEffect? _lensFlareGhostEffect;
    private LensFlareStarburstEffect? _lensFlareStarburstEffect;

    private Point _lensFlareSourcePosition = new(0.5, 0.5);

    private float _lensFlareAspectRatio = 1.0f;

    private float _lensFlareSourceVisibility;

    /// <summary>
    /// Updates the projected position of the Local Star.
    /// Called by WorldGlobeViewer as the camera moves.
    /// </summary>
    public void UpdateLensFlareSource(
        Point? sourcePosition,
        double aspectRatio)
    {
        if (sourcePosition.HasValue)
            _lensFlareSourcePosition = sourcePosition.Value;

        _lensFlareSourceVisibility =
            sourcePosition.HasValue ? 1.0f : 0.0f;

        _lensFlareAspectRatio =
            double.IsFinite(aspectRatio) && aspectRatio > 0.0
                ? (float)aspectRatio
                : 1.0f;

        UpdateLensFlarePasses();
    }

    private void UpdateLensFlarePasses()
    {
        foreach (LensFlarePassEffect? effect in new LensFlarePassEffect?[]
        {
        _lensFlareCoreEffect,
        _lensFlareStreakEffect,
        _lensFlareGhostEffect,
        _lensFlareStarburstEffect
        })
        {
            if (effect is null)
                continue;

            effect.SourcePosition = _lensFlareSourcePosition;
            effect.AspectRatio = _lensFlareAspectRatio;
            effect.SourceVisibility = _lensFlareSourceVisibility;
        }
    }

    #endregion

    #region Star Scintillation

    private bool _enableStarScintillation;

    public bool EnableStarScintillation
    {
        get => _enableStarScintillation;
        set
        {
            if (SetProperty(ref _enableStarScintillation, value))
                ApplyBackgroundScintillation();
        }
    }

    private double _starScintillationIntensity = 1.0;

    public double StarScintillationIntensity
    {
        get => _starScintillationIntensity;
        set
        {
            value = ClampFinite(value, 0.0, 2.0, 1.0);

            if (SetProperty(ref _starScintillationIntensity, value))
                ApplyBackgroundScintillation();
        }
    }

    private double _starScintillationSpeed = 2.0;

    public double StarScintillationSpeed
    {
        get => _starScintillationSpeed;
        set
        {
            value = ClampFinite(value, 0.1, 4.0, 1.0);

            if (SetProperty(ref _starScintillationSpeed, value))
                ApplyBackgroundScintillation();
        }
    }

    private double _starScintillationThreshold = 0.72;

    public double StarScintillationThreshold
    {
        get => _starScintillationThreshold;
        set
        {
            value = ClampFinite(value, 0.0, 1.0, 0.72);

            if (SetProperty(ref _starScintillationThreshold, value))
                ApplyBackgroundScintillation();
        }
    }

    private double _starScintillationContrastThreshold = 0.12;

    public double StarScintillationContrastThreshold
    {
        get => _starScintillationContrastThreshold;
        set
        {
            value = ClampFinite(value, 0.0, 1.0, 0.12);

            if (SetProperty(ref _starScintillationContrastThreshold, value))
                ApplyBackgroundScintillation();
        }
    }

    private double _starScintillationMaximumStarSize = 5.0;

    public double StarScintillationMaximumStarSize
    {
        get => _starScintillationMaximumStarSize;
        set
        {
            value = ClampFinite(value, 1.0, 8.0, 3.0);

            if (SetProperty(ref _starScintillationMaximumStarSize, value))
                ApplyBackgroundScintillation();
        }
    }

    private void ApplyBackgroundScintillation()
    {
        if (_isRestoringSettings)
            return;

        _modelViewer.SetBackgroundScintillation(
            EnableStarScintillation,
            (float)StarScintillationIntensity,
            (float)StarScintillationSpeed,
            (float)StarScintillationThreshold,
            (float)StarScintillationContrastThreshold,
            (float)StarScintillationMaximumStarSize);
    }

    #endregion

    private void ApplyEnabledEffects()
    {
        if (_isRestoringSettings)
            return;

        BlurEffect? blur = EnableBlur
            ? new BlurEffect
            {
                Radius = BlurAmount,
                KernelType = KernelType.Gaussian,
                RenderingBias = RenderingBias.Quality
            }
            : null;

        BleachBypassEffect? bleachBypass = EnableBleachBypass
            ? new BleachBypassEffect
            {
                Opacity = BleachBypassOpacity / 100.0
            }
            : null;

        double dpiScale = VisualTreeHelper.GetDpi(_modelViewer).DpiScaleX;

        int pixelWidth = (int)Math.Ceiling(_modelViewer.ActualWidth * dpiScale);
        int pixelHeight = (int)Math.Ceiling(_modelViewer.ActualHeight * dpiScale);

        float texelWidth = pixelWidth > 0 ? 1.0f / pixelWidth : 0.0f;
        float texelHeight = pixelHeight > 0 ? 1.0f / pixelHeight : 0.0f;

        NvidiaBloomDownsampleEffect? bloomDownsample = EnableBloom
            ? new NvidiaBloomDownsampleEffect
            {
                HighlightThreshold = (float)BloomHighlightThreshold,
                TexelWidth = texelWidth,
                TexelHeight = texelHeight
            }
            : null;

        NvidiaBloomHorizontalBlurEffect? bloomHorizontal = EnableBloom
            ? new NvidiaBloomHorizontalBlurEffect
            {
                BlurWidth = (float)BloomBlurWidth,
                TexelSize = texelWidth
            }
            : null;

        NvidiaBloomVerticalBlurEffect? bloomVertical = EnableBloom
            ? new NvidiaBloomVerticalBlurEffect
            {
                BlurWidth = (float)BloomBlurWidth,
                TexelSize = texelHeight
            }
            : null;

        NvidiaBloomCompositeEffect? bloomComposite = EnableBloom
            ? new NvidiaBloomCompositeEffect
            {
                Scene = _modelViewer.CreateBloomSceneBrush(),
                SceneIntensity = 1.0f,
                GlowIntensity = (float)BloomIntensity,
                HighlightIntensity = (float)BloomHighlightIntensity
            }
            : null;

        SepiaEffect? sepia = EnableSepia
            ? new SepiaEffect
            {
                GrayscaleFactor = (float)(SepiaAmount / 100.0)
            }
            : null;

        GrayscaleEffect? grayscale = EnableGrayscale
            ? new GrayscaleEffect
            {
                DesaturationFactor = DesaturationFactor / 100.0
            }
            : null;

        _lensFlareCoreEffect = EnableLensFlare
            ? new LensFlareCoreEffect()
            {
                SourcePosition = _lensFlareSourcePosition,
                AspectRatio = _lensFlareAspectRatio,
                Intensity = (float)LensFlareIntensity,
                GlowSize = (float)LensFlareGlowSize,
                SourceVisibility = _lensFlareSourceVisibility
            }
            : null;

        _lensFlareStreakEffect = EnableLensFlare
            ? new LensFlareStreakEffect()
            {
                SourcePosition = _lensFlareSourcePosition,
                AspectRatio = _lensFlareAspectRatio,
                Intensity = (float)LensFlareIntensity,
                GlowSize = (float)LensFlareGlowSize,
                StreakLength = (float)LensFlareStreakLength,
                SourceVisibility = _lensFlareSourceVisibility
            }
            : null;

        _lensFlareGhostEffect = EnableLensFlare
            ? new LensFlareGhostEffect()
            {
                SourcePosition = _lensFlareSourcePosition,
                AspectRatio = _lensFlareAspectRatio,
                Intensity = (float)LensFlareIntensity,
                GlowSize = (float)LensFlareGlowSize,
                GhostIntensity = (float)LensFlareGhostIntensity,
                SourceVisibility = _lensFlareSourceVisibility
            }
            : null;

        _lensFlareStarburstEffect = EnableLensFlare
            ? new LensFlareStarburstEffect()
            {
                SourcePosition = _lensFlareSourcePosition,
                AspectRatio = _lensFlareAspectRatio,
                Intensity = (float)LensFlareIntensity,
                GlowSize = (float)LensFlareGlowSize,
                StarburstIntensity = (float)LensFlareStarburstIntensity,
                SourceVisibility = _lensFlareSourceVisibility
            }
            : null;

        _modelViewer.SetPostProcessingEffects(
            [
                grayscale,
                sepia,
                bloomDownsample,
                bloomHorizontal,
                bloomVertical,
                bloomComposite,
                _lensFlareCoreEffect,
                _lensFlareStreakEffect,
                _lensFlareGhostEffect,
                _lensFlareStarburstEffect,
                bleachBypass,
                blur
            ]);
    }

    public SceneEffectsSettings CaptureSettings()
    {
        return new SceneEffectsSettings
        {
            // Blur
            EnableBlur = EnableBlur,
            BlurAmount = BlurAmount,

            // Bleach bypass
            EnableBleachBypass = EnableBleachBypass,
            BleachBypassOpacity = BleachBypassOpacity,

            // Bloom
            EnableBloom = EnableBloom,
            BloomBlurWidth = BloomBlurWidth,
            BloomIntensity = BloomIntensity,
            BloomHighlightThreshold = BloomHighlightThreshold,
            BloomHighlightIntensity = BloomHighlightIntensity,

            // Sepia
            EnableSepia = EnableSepia,
            SepiaAmount = SepiaAmount,

            // Grayscale
            EnableGrayscale = EnableGrayscale,
            DesaturationFactor = DesaturationFactor,

            // Lens flare
            EnableLensFlare = EnableLensFlare,
            LensFlareIntensity = LensFlareIntensity,
            LensFlareGlowSize = LensFlareGlowSize,
            LensFlareStreakLength = LensFlareStreakLength,
            LensFlareGhostIntensity = LensFlareGhostIntensity,
            LensFlareStarburstIntensity = LensFlareStarburstIntensity,

            // Background star scintillation
            EnableStarScintillation = EnableStarScintillation,
            StarScintillationIntensity = StarScintillationIntensity,
            StarScintillationSpeed = StarScintillationSpeed,
            StarScintillationThreshold = StarScintillationThreshold,
            StarScintillationContrastThreshold =
                StarScintillationContrastThreshold,
            StarScintillationMaximumStarSize =
                StarScintillationMaximumStarSize
        };
    }

    public void RestoreSettings(SceneEffectsSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _isRestoringSettings = true;

        try
        {
            // Blur
            EnableBlur = settings.EnableBlur;
            BlurAmount = settings.BlurAmount;

            // Bleach bypass
            EnableBleachBypass = settings.EnableBleachBypass;
            BleachBypassOpacity = settings.BleachBypassOpacity;

            // Bloom
            EnableBloom = settings.EnableBloom;
            BloomBlurWidth = settings.BloomBlurWidth;
            BloomIntensity = settings.BloomIntensity;
            BloomHighlightThreshold = settings.BloomHighlightThreshold;
            BloomHighlightIntensity = settings.BloomHighlightIntensity;

            // Sepia
            EnableSepia = settings.EnableSepia;
            SepiaAmount = settings.SepiaAmount;

            // Grayscale
            EnableGrayscale = settings.EnableGrayscale;
            DesaturationFactor = settings.DesaturationFactor;

            // Lens flare
            EnableLensFlare = settings.EnableLensFlare;
            LensFlareIntensity = settings.LensFlareIntensity;
            LensFlareGlowSize = settings.LensFlareGlowSize;
            LensFlareStreakLength = settings.LensFlareStreakLength;
            LensFlareGhostIntensity = settings.LensFlareGhostIntensity;
            LensFlareStarburstIntensity = settings.LensFlareStarburstIntensity;

            // Background star scintillation
            EnableStarScintillation = settings.EnableStarScintillation;
            StarScintillationIntensity = settings.StarScintillationIntensity;
            StarScintillationSpeed = settings.StarScintillationSpeed;
            StarScintillationThreshold = settings.StarScintillationThreshold;
            StarScintillationContrastThreshold =
                settings.StarScintillationContrastThreshold;
            StarScintillationMaximumStarSize =
                settings.StarScintillationMaximumStarSize;
        }
        finally
        {
            _isRestoringSettings = false;
        }

        // Rebuild the post-processing chain once, after all values are restored.
        ApplyEnabledEffects();
        ApplyBackgroundScintillation();
    }

    private static double ClampFinite(
        double value,
        double minimum,
        double maximum,
        double fallback)
    {
        return double.IsFinite(value)
            ? Math.Clamp(value, minimum, maximum)
            : fallback;
    }
}
