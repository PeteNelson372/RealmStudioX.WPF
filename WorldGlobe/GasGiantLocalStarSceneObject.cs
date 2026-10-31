using RealmStudioShapeRenderingLib;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.WorldGlobe;

internal sealed class GasGiantLocalStarSceneObject : ILocalStarSceneObject, IWorldGlobeAnimatedObject
{
    private readonly MeshGeometry3D _unitSphereMesh;

    private readonly Model3DGroup _positionGroup = new();

    private readonly GeometryModel3D _gasGiantSurface;

    private readonly DiffuseMaterial _surfaceMaterial;

    private string _loadedTexturePath = string.Empty;

    private double _surfaceRadius = 0.15;

    private double _rotationRate = 1.0;

    private WorldRotationDirection _rotationDirection = WorldRotationDirection.Prograde;

    private double _surfaceAnimationAngle;

    private Quaternion _baseSurfaceRotation = Quaternion.Identity;

    private const double SurfaceSecondsPerRevolution = 60.0;

    private readonly GasGiantAtmosphereEffect3D _atmosphereEffect;

    public Model3DGroup Root { get; } = new();

    public LocalStarType Type => LocalStarType.GasGiant;

    public GasGiantLocalStarSceneObject(LocalStar star, MeshGeometry3D unitSphereMesh)
    {
        GasGiantSettings settings =
            star.LocalStarDetailSettings as GasGiantSettings
            ?? new GasGiantSettings();

        _unitSphereMesh = unitSphereMesh;

        Root.Transform = star.LocationTransform;

        Root.Children.Add(_positionGroup);

        // Diffuse material allows the planet to respond to scene lighting.
        _surfaceMaterial = new DiffuseMaterial(new SolidColorBrush(star.StarColor));

        _gasGiantSurface = new GeometryModel3D
        {
            Geometry = _unitSphereMesh,
            Material = _surfaceMaterial,
            BackMaterial = _surfaceMaterial,
            Transform = CreateStarSurfaceTransform(
                star.Radius,
                star.RotationQuaternion)
        };

        _positionGroup.Children.Add(_gasGiantSurface);

        _atmosphereEffect = new GasGiantAtmosphereEffect3D(
            _unitSphereMesh,
            star.StarColor,
            star.Radius,
            star.RotationQuaternion,
            settings.EnableAtmosphericTurbulence,
            settings.TurbulenceIntensity);

        _positionGroup.Children.Add(_gasGiantSurface);
        _positionGroup.Children.Add(_atmosphereEffect.Root);

        Update(star);
    }

    public void Update(LocalStar star)
    {
        GasGiantSettings settings =
            star.LocalStarDetailSettings as GasGiantSettings
            ?? new GasGiantSettings();

        Root.Transform = star.LocationTransform;

        _positionGroup.Transform = new TranslateTransform3D(
            star.Center.X,
            star.Center.Y,
            star.Center.Z);

        _surfaceRadius = Math.Max(0.001, star.Radius);

        _baseSurfaceRotation = star.RotationQuaternion;

        _rotationRate = double.IsFinite(star.RotationRate)
            ? Math.Clamp(star.RotationRate, 0.0, 10.0)
            : 0.0;

        _rotationDirection = star.RotationDirection;

        _atmosphereEffect.Update(
            star.Radius,
            star.RotationQuaternion,
            star.StarColor,
            settings.EnableAtmosphericTurbulence,
            settings.TurbulenceIntensity);

        ApplySurfaceTransform();

        UpdateSurfaceTexture(settings.TexturePath, star.StarColor);

        ApplySurfaceTransform();
    }

    private void UpdateSurfaceTexture(
        string? texturePath,
        Color fallbackColor)
    {
        texturePath ??= string.Empty;

        // Avoid reloading the texture on every ordinary property update.
        if (string.Equals(
            texturePath,
            _loadedTexturePath,
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _loadedTexturePath = texturePath;

        Brush surfaceBrush;

        if (!string.IsNullOrWhiteSpace(texturePath) &&
            File.Exists(texturePath))
        {
            try
            {
                BitmapImage bitmap = new();

                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(texturePath, UriKind.Absolute);
                bitmap.EndInit();

                bitmap.Freeze();

                ImageBrush imageBrush = new(bitmap)
                {
                    Stretch = Stretch.Fill
                };

                imageBrush.Freeze();

                surfaceBrush = imageBrush;
            }
            catch (IOException)
            {
                surfaceBrush = CreateFallbackBrush(fallbackColor);
            }
            catch (UnauthorizedAccessException)
            {
                surfaceBrush = CreateFallbackBrush(fallbackColor);
            }
            catch (NotSupportedException)
            {
                surfaceBrush = CreateFallbackBrush(fallbackColor);
            }
            catch (UriFormatException)
            {
                surfaceBrush = CreateFallbackBrush(fallbackColor);
            }
        }
        else
        {
            surfaceBrush = CreateFallbackBrush(fallbackColor);
        }

        _surfaceMaterial.Brush = surfaceBrush;
    }

    private static Brush CreateFallbackBrush(Color color)
    {
        SolidColorBrush brush = new(color);
        brush.Freeze();

        return brush;
    }

    private static Transform3D CreateStarSurfaceTransform(double radius, Quaternion rotation)
    {
        Transform3DGroup transform = new();

        transform.Children.Add(new ScaleTransform3D(radius, radius, radius));

        transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));

        return transform;
    }

    private void ApplySurfaceTransform()
    {
        Quaternion spinRotation = new(
            new Vector3D(0, 1, 0),
            _surfaceAnimationAngle);

        Quaternion surfaceRotation = Quaternion.Multiply(
            _baseSurfaceRotation,
            spinRotation);

        _gasGiantSurface.Transform = CreateStarSurfaceTransform(
            _surfaceRadius,
            surfaceRotation);

        _atmosphereEffect.SetSurfaceRotation(_surfaceAnimationAngle);
    }

    public void AdvanceAnimation(WorldGlobeAnimationFrame frame)
    {
        if (frame.DeltaSeconds > 0.0 && _rotationRate > 0.0)
        {
            double direction =
                _rotationDirection == WorldRotationDirection.Prograde
                    ? 1.0
                    : -1.0;

            double degreesPerSecond =
                360.0 / SurfaceSecondsPerRevolution * _rotationRate;

            _surfaceAnimationAngle +=
                direction * degreesPerSecond * frame.DeltaSeconds;

            _surfaceAnimationAngle %= 360.0;

            if (_surfaceAnimationAngle < 0.0)
                _surfaceAnimationAngle += 360.0;

            ApplySurfaceTransform();
        }

        // The atmosphere has its own animation, independent of surface speed.
        _atmosphereEffect.AdvanceAnimation(frame);
    }

    public void ResetAnimation()
    {
        _surfaceAnimationAngle = 0.0;

        ApplySurfaceTransform();

        _atmosphereEffect.ResetAnimation();
    }
}