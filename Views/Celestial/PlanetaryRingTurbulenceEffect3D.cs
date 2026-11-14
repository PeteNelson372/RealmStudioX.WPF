using RealmStudioX.Core;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RealmStudioX.WPF.Views.Celestial
{
    internal sealed class PlanetaryRingTurbulenceEffect3D
    {
        private const int TextureWidth = 512;
        private const int TextureHeight = 256;

        private const double MinimumUpdateRate = 1.0;
        private const double MaximumUpdateRate = 120.0;

        private double _updateRate = 30.0;

        private double TextureUpdateInterval => 1.0 / _updateRate;

        private readonly WriteableBitmap _texture;
        private readonly byte[] _pixels;

        private byte[] _sourcePixels = [];

        private int _sourceWidth;
        private int _sourceHeight;

        private double _intensity = 0.2;
        private bool _enabled;

        private double _lastTextureUpdateTime = double.NegativeInfinity;
        private bool _textureModified = true;

        public BitmapSource Texture => _texture;

        public PlanetaryRingTurbulenceEffect3D(
            BitmapSource sourceTexture,
            bool enabled,
            double intensity,
            double updateRate = 30.0)
        {
            _texture = new WriteableBitmap(
                TextureWidth,
                TextureHeight,
                96,
                96,
                PixelFormats.Pbgra32,
                null);

            _pixels = new byte[TextureWidth * TextureHeight * 4];

            SetSourceTexture(sourceTexture);

            Update(enabled, intensity, updateRate);

            UpdateTexture(0.0);
        }

        public void SetSourceTexture(BitmapSource sourceTexture)
        {
            FormatConvertedBitmap converted = new(
                sourceTexture,
                PixelFormats.Pbgra32,
                null,
                0);

            _sourceWidth = converted.PixelWidth;
            _sourceHeight = converted.PixelHeight;

            _sourcePixels = new byte[
                _sourceWidth * _sourceHeight * 4];

            converted.CopyPixels(
                _sourcePixels,
                _sourceWidth * 4,
                0);

            _textureModified = true;

            // Immediately regenerate the output bitmap using the new source.
            // Otherwise, the previous source texture remains visible until
            // the next animation update.
            UpdateTexture(
                double.IsFinite(_lastTextureUpdateTime)
                    ? _lastTextureUpdateTime
                    : 0.0);
        }

        public void Update(bool enabled, double intensity, double updateRate = 30.0)
        {
            _enabled = enabled;

            _intensity = double.IsFinite(intensity)
                ? Math.Clamp(intensity, 0.0, 1.0)
                : 0.2;

            _updateRate = double.IsFinite(updateRate)
                ? Math.Clamp(updateRate, MinimumUpdateRate, MaximumUpdateRate)
                : 30.0;

            _textureModified = true;
        }

        public void AdvanceAnimation(CelestialAnimationFrame frame)
        {
            if (!_enabled || _intensity <= 0.0)
                return;

            if (!_textureModified &&
                frame.TimeSeconds - _lastTextureUpdateTime < TextureUpdateInterval)
            {
                return;
            }

            UpdateTexture(frame.TimeSeconds);
        }

        public void ResetAnimation()
        {
            _lastTextureUpdateTime = double.NegativeInfinity;
            _textureModified = true;

            UpdateTexture(0.0);
        }

        private void UpdateTexture(double timeSeconds)
        {
            double intensity = Math.Sqrt(_intensity);

            for (int y = 0; y < TextureHeight; y++)
            {
                double v = (double)y / TextureHeight;
                double angle = v * Math.PI * 2.0;

                for (int x = 0; x < TextureWidth; x++)
                {
                    double u = (double)x / TextureWidth;

                    // Broad radial displacement.
                    double radialWaveA = Math.Sin(
                        angle * 5.0 +
                        u * 13.0 +
                        timeSeconds * 0.45);

                    double radialWaveB = Math.Sin(
                        angle * 11.0 -
                        u * 27.0 -
                        timeSeconds * 0.28);

                    // Smaller-scale circumferential displacement.
                    double angularWave = Math.Sin(
                        u * 32.0 +
                        angle * 7.0 +
                        timeSeconds * 0.35);

                    double distortedU =
                        u +
                        (radialWaveA * 0.004 +
                         radialWaveB * 0.0015) * intensity;

                    double distortedV =
                        v +
                        angularWave * 0.0015 * intensity;

                    // Sample the original texture at the displaced coordinates.
                    SampleTexture(
                        distortedU,
                        distortedV,
                        out double blue,
                        out double green,
                        out double red,
                        out double alpha);

                    // Slowly evolving local brightness variation.
                    double brightnessWaveA = Math.Sin(
                        angle * 8.0 +
                        u * 18.0 +
                        timeSeconds * 0.22);

                    double brightnessWaveB = Math.Sin(
                        angle * 19.0 -
                        u * 31.0 -
                        timeSeconds * 0.16);

                    double brightness =
                        1.0 +
                        (brightnessWaveA * 0.07 +
                         brightnessWaveB * 0.035) * intensity;

                    // Subtle local opacity variation.
                    double opacityVariation =
                        1.0 +
                        (Math.Sin(
                            angle * 13.0 +
                            u * 23.0 +
                            timeSeconds * 0.3) * 0.08) * intensity;

                    alpha *= opacityVariation;

                    WritePixel(
                        x,
                        y,
                        blue * brightness,
                        green * brightness,
                        red * brightness,
                        alpha);
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

        private void SampleTexture(
            double u,
            double v,
            out double blue,
            out double green,
            out double red,
            out double alpha)
        {
            // Wrap around both texture dimensions.
            u -= Math.Floor(u);
            v -= Math.Floor(v);

            int x = (int)(u * _sourceWidth);
            int y = (int)(v * _sourceHeight);

            x = Math.Clamp(x, 0, _sourceWidth - 1);
            y = Math.Clamp(y, 0, _sourceHeight - 1);

            int index = (y * _sourceWidth + x) * 4;

            // Source pixels are premultiplied BGRA.
            alpha = _sourcePixels[index + 3] / 255.0;

            if (alpha > 0.0001)
            {
                // Unpremultiply before applying brightness.
                blue = _sourcePixels[index] / 255.0 / alpha;
                green = _sourcePixels[index + 1] / 255.0 / alpha;
                red = _sourcePixels[index + 2] / 255.0 / alpha;
            }
            else
            {
                blue = green = red = 0.0;
            }
        }

        private void WritePixel(
            int x,
            int y,
            double blue,
            double green,
            double red,
            double alpha)
        {
            int index = (y * TextureWidth + x) * 4;

            alpha = Math.Clamp(alpha, 0.0, 1.0);

            // Write premultiplied BGRA values.
            _pixels[index] = ToByte(blue * alpha);
            _pixels[index + 1] = ToByte(green * alpha);
            _pixels[index + 2] = ToByte(red * alpha);
            _pixels[index + 3] = ToByte(alpha * 255.0);
        }

        private static byte ToByte(double value)
        {
            return (byte)Math.Clamp(
                Math.Round(value * 255.0),
                0.0,
                255.0);
        }
    }
}
