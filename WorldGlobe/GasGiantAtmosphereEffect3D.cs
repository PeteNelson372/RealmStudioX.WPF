using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.WorldGlobe;

internal sealed class GasGiantAtmosphereEffect3D : IWorldGlobeAnimatedObject
{
    private const int TextureWidth = 512;
    private const int TextureHeight = 256;

    // Limit procedural bitmap updates to approximately 12 per second.
    private const double TextureUpdateInterval = 1.0 / 12.0;

    private const double AtmosphereRadiusScale = 1.012;

    private readonly WriteableBitmap _texture;

    private readonly byte[] _pixels;

    private readonly ImageBrush _atmosphereBrush;

    private readonly DiffuseMaterial _atmosphereMaterial;

    private readonly GeometryModel3D _atmosphereSurface;

    private readonly double _textureUpdateInterval = TextureUpdateInterval;

    private Color _baseColor;

    private double _intensity = 0.35;

    private bool _enabled = true;

    private double _radius = 0.15;

    private Quaternion _baseRotation = Quaternion.Identity;

    private double _surfaceRotationAngle;

    private double _lastTextureUpdateTime = double.NegativeInfinity;

    private bool _textureModified = true;

    public Model3DGroup Root { get; } = new();

    public GasGiantAtmosphereEffect3D(
        MeshGeometry3D unitSphereMesh,
        Color baseColor,
        double radius,
        Quaternion rotation,
        bool enabled,
        double intensity)
    {
        _baseColor = baseColor;

        _texture = new WriteableBitmap(
            TextureWidth,
            TextureHeight,
            96,
            96,
            PixelFormats.Pbgra32,
            null);

        _pixels = new byte[TextureWidth * TextureHeight * 4];

        _atmosphereBrush = new ImageBrush(_texture)
        {
            Stretch = Stretch.Fill
        };

        _atmosphereMaterial = new DiffuseMaterial(_atmosphereBrush);

        _atmosphereSurface = new GeometryModel3D
        {
            Geometry = unitSphereMesh,
            Material = _atmosphereMaterial,
            BackMaterial = _atmosphereMaterial
        };

        Root.Children.Add(_atmosphereSurface);

        Update(radius, rotation, baseColor, enabled, intensity);

        UpdateTexture(0.0);
    }

    public void Update(
        double radius,
        Quaternion rotation,
        Color baseColor,
        bool enabled,
        double intensity)
    {
        _radius = Math.Max(0.001, radius);
        _baseRotation = rotation;
        _baseColor = baseColor;
        _enabled = enabled;

        _intensity = double.IsFinite(intensity)
            ? Math.Clamp(intensity, 0.0, 1.0)
            : 0.35;

        _atmosphereBrush.Opacity = _enabled
            ? _intensity
            : 0.0;

        ApplyTransform();

        _atmosphereBrush.Opacity = _enabled ? 1.0 : 0.0;

        _textureModified = true;

        ApplyTransform();
    }

    public void SetSurfaceRotation(double angle)
    {
        _surfaceRotationAngle = angle;

        ApplyTransform();
    }

    private void ApplyTransform()
    {
        Quaternion spinRotation = new(
            new Vector3D(0, 1, 0),
            _surfaceRotationAngle);

        Quaternion atmosphereRotation = Quaternion.Multiply(
            _baseRotation,
            spinRotation);

        Transform3DGroup transform = new();

        transform.Children.Add(
            new ScaleTransform3D(
                _radius * AtmosphereRadiusScale,
                _radius * AtmosphereRadiusScale,
                _radius * AtmosphereRadiusScale));

        transform.Children.Add(
            new RotateTransform3D(
                new QuaternionRotation3D(atmosphereRotation)));

        _atmosphereSurface.Transform = transform;
    }

    public void AdvanceAnimation(WorldGlobeAnimationFrame frame)
    {
        if (!_enabled || _intensity <= 0.0)
            return;

        if (!_textureModified &&
            frame.TimeSeconds - _lastTextureUpdateTime < _textureUpdateInterval)
        {
            return;
        }

        UpdateTexture(frame.TimeSeconds);
    }

    public void ResetAnimation()
    {
        _lastTextureUpdateTime = double.NegativeInfinity;

        UpdateTexture(0.0);
    }

    private void UpdateTexture(double timeSeconds)
    {
        if (!_enabled || _intensity <= 0.0)
        {
            ClearTexture();
            _lastTextureUpdateTime = timeSeconds;
            return;
        }

        for (int y = 0; y < TextureHeight; y++)
        {
            double v = (double)y / TextureHeight;

            // Latitude ranges from +90 degrees to -90 degrees.
            double latitude = (0.5 - v) * Math.PI;

            double latitudeBand =
                Math.Sin(latitude * 22.0);

            for (int x = 0; x < TextureWidth; x++)
            {
                double u = (double)x / TextureWidth;

                double longitude = u * Math.PI * 2.0;

                // Broad atmospheric bands moving at different rates.
                double bandOffset =
                    timeSeconds * 0.12 * Math.Sin(latitude * 4.0);

                // Add traveling waves to the band boundaries.
                double waveA = Math.Sin(
                    longitude * 3.0 +
                    latitude * 7.0 +
                    timeSeconds * 0.8);

                double waveB = Math.Sin(
                    longitude * 7.0 -
                    latitude * 11.0 -
                    timeSeconds * 0.55);

                double distortion =
                    (waveA * 0.075 + waveB * 0.045) *
                    Math.Sqrt(_intensity);

                double bandValue = Math.Sin(
                    latitude * 22.0 +
                    distortion * 22.0 +
                    bandOffset);

                // Give the bands softer, irregular boundaries.
                double bandStrength =
                    0.5 + 0.5 * Math.Sin(bandValue * 2.4);

                // A subtle fine-grained atmospheric pattern.
                double detail =
                    Math.Sin(longitude * 31.0 + latitude * 18.0 + timeSeconds * 1.7) *
                    Math.Sin(longitude * 17.0 - latitude * 29.0 - timeSeconds * 1.1);

                double vortexStrength = GetVortexStrength(
                    longitude,
                    latitude,
                    timeSeconds);

                double turbulence =
                    Math.Clamp(
                        bandStrength * 0.55 +
                        detail * 0.12 +
                        vortexStrength * 0.7,
                        0.0,
                        1.0);

                // Keep the poles relatively quiet.
                double polarFade = Math.Clamp(
                    1.0 - Math.Pow(Math.Abs(latitude) / (Math.PI * 0.5), 5.0),
                    0.0,
                    1.0);

                // Use a nonlinear intensity curve so moderate settings
                // produce a clearly visible effect.
                double intensity = Math.Sqrt(_intensity);

                // Increase the atmospheric contribution.
                double alpha =
                    (0.22 + turbulence * 0.48) *
                    polarFade *
                    intensity;

                alpha = Math.Clamp(alpha, 0.0, 0.65);

                // Alternate slightly lighter and darker atmospheric bands.
                // Stronger variation between atmospheric bands.
                double shade = 0.48 + turbulence * 0.95;

                // Make localized storms more visually distinctive.
                shade += vortexStrength * 0.45;

                shade = Math.Clamp(shade, 0.25, 1.65);

                // Give storms a little more visual contrast.
                shade += vortexStrength * 0.25;

                // Slowly evolving color variation across the atmospheric bands.
                double colorWaveA = Math.Sin(
                    longitude * 2.7 +
                    latitude * 5.1 +
                    timeSeconds * 0.18);

                double colorWaveB = Math.Sin(
                    latitude * 17.0 -
                    longitude * 3.2 +
                    timeSeconds * 0.11);

                // Keep the overall color shifts restrained.
                double colorVariation =
                    colorWaveA * 0.035 +
                    colorWaveB * 0.025;

                // Add a little additional variation following the band structure.
                double bandColorVariation =
                    Math.Sin(
                        latitude * 22.0 +
                        distortion * 22.0 +
                        bandOffset + 0.8) * 0.035;

                // Vortices receive a subtle warmer coloration.
                double vortexWarmth = vortexStrength * 0.08;

                double redMultiplier =
                    1.0 + colorVariation + bandColorVariation + vortexWarmth;

                double greenMultiplier =
                    1.0 - colorVariation * 0.25 + bandColorVariation * 0.2;

                double blueMultiplier =
                    1.0 - colorVariation - bandColorVariation * 0.4
                    - vortexWarmth * 0.8;

                WritePixel(
                    x,
                    y,
                    shade,
                    alpha,
                    redMultiplier,
                    greenMultiplier,
                    blueMultiplier);
            }
        }

        _texture.WritePixels(
            new Int32Rect(0, 0, TextureWidth, TextureHeight),
            _pixels,
            TextureWidth * 4,
            0);

        _lastTextureUpdateTime = timeSeconds;
        _textureModified = false;
    }

    private double GetVortexStrength(
        double longitude,
        double latitude,
        double timeSeconds)
    {
        // Three persistent storm systems at different latitudes.
        double vortexA = Vortex(
            longitude,
            latitude,
            -1.1 + timeSeconds * 0.035,
            0.22,
            0.24,
            0.12);

        double vortexB = Vortex(
            longitude,
            latitude,
            1.8 - timeSeconds * 0.025,
            -0.38,
            0.32,
            0.10);

        double vortexC = Vortex(
            longitude,
            latitude,
            3.6 + timeSeconds * 0.018,
            0.52,
            0.18,
            0.07);

        return Math.Clamp(
            vortexA + vortexB + vortexC,
            0.0,
            1.0);
    }

    private static double Vortex(
        double longitude,
        double latitude,
        double centerLongitude,
        double centerLatitude,
        double longitudeRadius,
        double latitudeRadius)
    {
        double longitudeDifference =
            longitude - centerLongitude;

        // Wrap longitude so storms cross the texture seam naturally.
        longitudeDifference =
            Math.Atan2(
                Math.Sin(longitudeDifference),
                Math.Cos(longitudeDifference));

        double dx = longitudeDifference / longitudeRadius;
        double dy = (latitude - centerLatitude) / latitudeRadius;

        double distanceSquared = dx * dx + dy * dy;

        return Math.Exp(-distanceSquared * 1.8);
    }

    private void WritePixel(
        int x,
        int y,
        double shade,
        double alpha,
        double redMultiplier,
        double greenMultiplier,
        double blueMultiplier)
    {
        int index = (y * TextureWidth + x) * 4;

        alpha = Math.Clamp(alpha, 0.0, 1.0);

        byte a = (byte)Math.Round(alpha * 255.0);

        // Premultiply RGB values because the bitmap uses Pbgra32.
        byte r = (byte)Math.Clamp(
            Math.Round(_baseColor.R * shade * redMultiplier * alpha),
            0.0,
            255.0);

        byte g = (byte)Math.Clamp(
            Math.Round(_baseColor.G * shade * greenMultiplier * alpha),
            0.0,
            255.0);

        byte b = (byte)Math.Clamp(
            Math.Round(_baseColor.B * shade * blueMultiplier * alpha),
            0.0,
            255.0);

        _pixels[index] = b;
        _pixels[index + 1] = g;
        _pixels[index + 2] = r;
        _pixels[index + 3] = a;
    }

    private void ClearTexture()
    {
        Array.Clear(_pixels);

        _texture.WritePixels(
            new Int32Rect(0, 0, TextureWidth, TextureHeight),
            _pixels,
            TextureWidth * 4,
            0);
    }
}