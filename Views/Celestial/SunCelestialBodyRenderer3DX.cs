
using HelixToolkit;
using HelixToolkit.Maths;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.EditorUtilities;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using MeshGeometry3D = HelixToolkit.SharpDX.MeshGeometry3D;

namespace RealmStudioX.WPF.Views.Celestial;

internal sealed class SunCelestialBodyRenderer3DX :
    LocalStarCelestialBodyRenderer3DX,
    ICelestialBodyVisualSettingsUpdater3DX
{
    private static readonly Lazy<BitmapSource> SunTexture =
        new(() => WorldGlobeAssets.LoadBitmap("sol-texture.jpg"));

    private readonly MeshGeometryModel3D _sunSurface = new();
    private readonly MeshGeometryModel3D _corona = new();

    private readonly Func<BitmapSource, TextureModel> _textureModelFactory;

    private const double CoronaRadiusScale = 1.02;

    private readonly SunCoronaAnimationParameters _coronaAnimation =
        SunCoronaAnimationParameters.CreateDefault();

    private readonly Quaternion _baseCoronaRotation = Quaternion.Identity;

    private readonly SolarActivityEffect3DX _solarActivity = new();

    // Reusable corona transform objects.
    private readonly ScaleTransform3D _coronaScale =
        new(CoronaRadiusScale, CoronaRadiusScale, CoronaRadiusScale);

    private readonly QuaternionRotation3D _coronaQuaternionRotation =
        new(Quaternion.Identity);

    private readonly Transform3DGroup _coronaTransform = new();

    private readonly RotateTransform3D _coronaRotationTransform;

    // Created once and retained for the lifetime of this renderer.
    private PhongMaterial? _sunMaterial;
    private PhongMaterial? _coronaMaterial;

    private double _coronaElapsedTime;
    private double _coronaAnimationAngle;
    private double _coronaRotationRate = 12.0;
    private bool _enableCorona = true;

    public override CelestialBodyRendererType Type => CelestialBodyRendererType.Sun;
    protected override bool SupportsPointLight => true;

    public SunCelestialBodyRenderer3DX(
        Func<BitmapSource, TextureModel> textureModelFactory)
    {
        _textureModelFactory = textureModelFactory
            ?? throw new ArgumentNullException(nameof(textureModelFactory));

        _sunSurface.Geometry = SphereMesh;

        _corona.Geometry = SphereMesh;
        _corona.IsTransparent = true;

        _coronaRotationTransform =
            new RotateTransform3D(_coronaQuaternionRotation);

        _coronaTransform.Children.Add(_coronaScale);
        _coronaTransform.Children.Add(_coronaRotationTransform);

        _corona.Transform = _coronaTransform;

        BodyVisualRoot.Children.Add(_corona);
        BodyVisualRoot.Children.Add(_sunSurface);

        // Solar activity is rendered on the camera-facing billboard layer,
        // independently of the rotating Sun surface and corona.
        BillboardVisualRoot.Children.Add(_solarActivity.Root);
    }

    protected override void UpdateBodyVisuals(
        CelestialBody body,
        CelestialTextureSource? textureSource)
    {
        EnsureMaterials();

        ApplyVisualSettings(body);
    }

    /// <summary>
    /// Updates Sun settings without recreating texture models, materials,
    /// geometry, or attachment visuals.
    /// </summary>
    public void UpdateVisualSettings(CelestialBodyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(node.Body);

        UpdatePlacement(node);

        // Normally the materials have already been created by the initial
        // full renderer update. Keep this path safe if that ever changes.
        if (_sunMaterial is null || _coronaMaterial is null)
        {
            UpdateBodyVisuals(node.Body, node.Body.TextureSource);
            return;
        }

        ApplyVisualSettings(node.Body);
    }

    private void EnsureMaterials()
    {
        if (_sunMaterial is not null && _coronaMaterial is not null)
            return;

        BitmapSource texture = SunTexture.Value;

        // Keep separate texture models for the two materials, as in the
        // original implementation, but create them only once.
        TextureModel sunTextureModel = _textureModelFactory(texture);
        TextureModel coronaTextureModel = _textureModelFactory(texture);

        _sunMaterial = new PhongMaterial
        {
            DiffuseColor = new Color4(1, 1, 1, 1),
            DiffuseMap = sunTextureModel,
            SpecularColor = new Color4(0, 0, 0, 1),
            SpecularShininess = 1
        };

        _coronaMaterial = new PhongMaterial
        {
            DiffuseColor = new Color4(1, 1, 1, 0.18f),
            DiffuseMap = coronaTextureModel,
            SpecularColor = new Color4(0, 0, 0, 0),
            SpecularShininess = 1
        };

        _sunSurface.Material = _sunMaterial;
        _corona.Material = _coronaMaterial;
    }

    private void ApplyVisualSettings(CelestialBody body)
    {
        CelestialBodyAnimation? solarActivity =
            body.Animations.FirstOrDefault(
                item => item.AnimationType ==
                        CelestialBodyAnimationType.SolarActivity);

        SunAnimationSettings settings =
            solarActivity?.Settings as SunAnimationSettings
            ?? new SunAnimationSettings();

        double opacity = double.IsFinite(settings.CoronaOpacity)
            ? Math.Clamp(settings.CoronaOpacity, 0.05, 0.30)
            : 0.18;

        // Preserve the existing Sun surface material formula.
        _sunMaterial!.EmissiveColor = new Color4(
            Math.Clamp(body.BaseColor.ScR * (float)opacity * 2.5f, 0, 1.0f),
            Math.Clamp(body.BaseColor.ScG * (float)opacity * 2.5f, 0, 1.0f),
            Math.Clamp(body.BaseColor.ScB * (float)opacity * 2.5f, 0, 1.0f),
            Math.Clamp(body.BaseColor.ScA * (float)opacity, 0, 1.0f));

        // Preserve the existing corona material settings.
        _coronaMaterial!.DiffuseColor = new Color4(
            1.0f,
            1.0f,
            1.0f,
            (float)opacity);

        _enableCorona = settings.EnableCorona;

        _coronaRotationRate =
            double.IsFinite(settings.CoronaRotationSpeed)
                ? Math.Clamp(settings.CoronaRotationSpeed, 0, 25)
                : 12.0;

        _corona.Visibility = _enableCorona
            ? Visibility.Visible
            : Visibility.Hidden;

        _solarActivity.Update(
            body.BaseColor,
            settings,
            solarActivity?.Enabled == true);

        // Recalculate the current orientation using the updated rotation rate.
        SetCoronaAnimationTime(_coronaElapsedTime);
    }

    public override void AdvanceAnimation(CelestialAnimationFrame frame)
    {
        base.AdvanceAnimation(frame);

        SetCoronaAnimationTime(frame.TimeSeconds);
        _solarActivity.AdvanceAnimation(frame);
    }

    public override void ResetAnimation()
    {
        base.ResetAnimation();

        SetCoronaAnimationTime(0.0);
        _solarActivity.ResetAnimation();
    }

    private void SetCoronaAnimationTime(double timeSeconds)
    {
        _coronaElapsedTime = double.IsFinite(timeSeconds)
            ? Math.Max(0.0, timeSeconds)
            : 0.0;

        _coronaAnimationAngle =
            _coronaAnimation.RotationDirection *
            _coronaRotationRate *
            _coronaElapsedTime;

        _coronaAnimationAngle %= 360.0;

        if (_coronaAnimationAngle < 0.0)
            _coronaAnimationAngle += 360.0;

        ApplyCoronaTransform();
    }

    private void ApplyCoronaTransform()
    {
        Quaternion primaryRotation = new(
            _coronaAnimation.RotationAxis,
            _coronaAnimationAngle);

        double wobbleAngleA =
            Math.Sin(
                _coronaElapsedTime *
                _coronaAnimation.WobbleFrequencyA *
                Math.PI * 2.0 +
                _coronaAnimation.WobblePhaseA) *
            _coronaAnimation.WobbleAmplitudeA;

        double wobbleAngleB =
            Math.Sin(
                _coronaElapsedTime *
                _coronaAnimation.WobbleFrequencyB *
                Math.PI * 2.0 +
                _coronaAnimation.WobblePhaseB) *
            _coronaAnimation.WobbleAmplitudeB;

        Quaternion wobbleRotationA = new(
            _coronaAnimation.WobbleAxisA,
            wobbleAngleA);

        Quaternion wobbleRotationB = new(
            _coronaAnimation.WobbleAxisB,
            wobbleAngleB);

        Quaternion coronaRotation = Quaternion.Multiply(
            _baseCoronaRotation,
            Quaternion.Multiply(
                primaryRotation,
                Quaternion.Multiply(
                    wobbleRotationA,
                    wobbleRotationB)));

        // Update the existing rotation transform rather than replacing
        // the entire Transform3D hierarchy on every animation frame.
        _coronaQuaternionRotation.Quaternion = coronaRotation;
    }
}
