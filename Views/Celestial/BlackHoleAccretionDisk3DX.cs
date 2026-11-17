
using HelixToolkit;
using HelixToolkit.Maths;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using System.IO;
using System.Numerics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaColor = System.Windows.Media.Color;
using MeshGeometry3D = HelixToolkit.SharpDX.MeshGeometry3D;

namespace RealmStudioX.WPF.Views.Celestial;

/// <summary>
/// Procedural accretion disk for the 3DX black-hole renderer.
/// The texture is generated from the host star color and uploaded
/// through a Helix TextureModel.
/// </summary>
internal sealed class BlackHoleAccretionDisk3DX
{
    private const int DiskAngularSegments = 384;
    private const int DiskRadialSegments = 64;

    private const double DiskOpacity = 0.58;

    private const double InnerRadius = 1.075;
    private const double OuterRadius = 3.25;

    private readonly MeshGeometryModel3D _model = new();

    private MediaColor _lastColor;
    private bool _hasColor;

    public MeshGeometryModel3D Model => _model;

    public BlackHoleAccretionDisk3DX()
    {
        _model.Geometry = CreateAccretionDiskMesh(
            InnerRadius,
            OuterRadius);

        _model.IsTransparent = true;
        _model.CullMode = SharpDX.Direct3D11.CullMode.None;
    }

    public void Update(MediaColor starColor)
    {
        if (_hasColor && _lastColor == starColor)
            return;

        // This is the actual procedural texture generation.
        BitmapSource bitmap = CreateAccretionDiskTexture(starColor);

        // Convert the generated bitmap into a SharpDX texture.
        TextureModel texture = CreateTextureModel(bitmap);

        _model.Material = new DiffuseMaterial
        {
            DiffuseColor = new Color4(1, 1, 1, 1),
            DiffuseMap = texture,
            EnableUnLit = true
        };

        _lastColor = starColor;
        _hasColor = true;
    }

    private static TextureModel CreateTextureModel(BitmapSource bitmap)
    {
        // Helix needs the stream to remain available until the texture
        // has been uploaded. The TextureModel is configured to dispose it.
        MemoryStream stream = new();

        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(stream);

        stream.Position = 0;

        return new TextureModel(stream, true);
    }

    private static MeshGeometry3D CreateAccretionDiskMesh(
        double innerRadius,
        double outerRadius)
    {
        MeshGeometry3D mesh = new()
        {
            Positions = [],
            Normals = [],
            TextureCoordinates = [],
            Indices = []
        };

        int stride = DiskAngularSegments + 1;

        for (int radialIndex = 0;
             radialIndex <= DiskRadialSegments;
             radialIndex++)
        {
            double radialFraction =
                radialIndex / (double)DiskRadialSegments;

            double radius = innerRadius +
                (outerRadius - innerRadius) * radialFraction;

            for (int angularIndex = 0;
                 angularIndex <= DiskAngularSegments;
                 angularIndex++)
            {
                double angularFraction =
                    angularIndex / (double)DiskAngularSegments;

                double angle = 2.0 * Math.PI * angularFraction;

                double heightNoise = SamplePeriodicNoise(
                    radialFraction, angularFraction, 9, 17);

                double fineHeightNoise = SamplePeriodicNoise(
                    radialFraction, angularFraction, 25, 41);

                double y =
                    (heightNoise - 0.5) * 0.045
                    + (fineHeightNoise - 0.5) * 0.018
                    + Math.Sin(
                        angle * 3.0 + radialFraction * 8.0) * 0.006;

                mesh.Positions.Add(new Vector3(
                    (float)(radius * Math.Cos(angle)),
                    (float)y,
                    (float)(radius * Math.Sin(angle))));

                mesh.TextureCoordinates.Add(new Vector2(
                    (float)radialFraction,
                    (float)angularFraction));

                mesh.Normals.Add(Vector3.UnitY);
            }
        }

        for (int radialIndex = 0;
             radialIndex < DiskRadialSegments;
             radialIndex++)
        {
            for (int angularIndex = 0;
                 angularIndex < DiskAngularSegments;
                 angularIndex++)
            {
                int innerCurrent =
                    radialIndex * stride + angularIndex;

                int innerNext = innerCurrent + 1;
                int outerCurrent = innerCurrent + stride;
                int outerNext = outerCurrent + 1;

                mesh.Indices.Add(innerCurrent);
                mesh.Indices.Add(innerNext);
                mesh.Indices.Add(outerCurrent);

                mesh.Indices.Add(innerNext);
                mesh.Indices.Add(outerNext);
                mesh.Indices.Add(outerCurrent);
            }
        }

        return mesh;
    }

    private static BitmapSource CreateAccretionDiskTexture(
        MediaColor starColor)
    {
        const int width = 256;
        const int height = 1024;

        int stride = width * 4;
        byte[] pixels = new byte[stride * height];

        var stops = CreateDiskColorStops(starColor);

        const double winding = -1.0;

        for (int y = 0; y < height; y++)
        {
            double angularFraction = y / (double)(height - 1);
            double angle = angularFraction * Math.PI * 2.0;

            for (int x = 0; x < width; x++)
            {
                double radialFraction = x / (double)(width - 1);

                MediaColor baseColor = SampleDiskColor(
                    stops, radialFraction);

                double broadNoise = SamplePeriodicNoise(
                    radialFraction, angularFraction, 7, 13);

                double mediumNoise = SamplePeriodicNoise(
                    radialFraction, angularFraction, 19, 31);

                double fineNoise = SamplePeriodicNoise(
                    radialFraction, angularFraction, 43, 67);

                double phaseDistortion =
                    (broadNoise - 0.5) * 2.8
                    + (mediumNoise - 0.5) * 1.2;

                double streak1 = Math.Sin(
                    angle * 13.0
                    + winding * radialFraction * 27.0
                    + phaseDistortion);

                double streak2 = Math.Sin(
                    angle * 29.0
                    + winding * radialFraction * 51.0
                    + phaseDistortion * 1.7);

                double streak3 = Math.Sin(
                    angle * 57.0
                    + winding * radialFraction * 93.0
                    + (fineNoise - 0.5) * 2.2);

                double knotPhase =
                    angle * 5.0
                    + winding * radialFraction * 18.0
                    + (mediumNoise - 0.5) * 2.0;

                double brightKnots = Math.Pow(
                    Math.Max(0.0, Math.Sin(knotPhase)), 7.0);

                double turbulence =
                    0.77
                    + 0.13 * streak1
                    + 0.09 * streak2
                    + 0.055 * streak3
                    + 0.12 * brightKnots
                    + 0.42 * (broadNoise - 0.5)
                    + 0.34 * (mediumNoise - 0.5)
                    + 0.24 * (fineNoise - 0.5);

                double brightness = Math.Clamp(
                    turbulence, 0.25, 1.35);

                double density =
                    0.48 * broadNoise
                    + 0.34 * mediumNoise
                    + 0.18 * fineNoise;

                double opacityVariation = 0.25
                    + 0.75 * Math.Pow(density, 1.25);

                double outerFade = Math.Clamp(
                    (1.0 - radialFraction) / 0.16,
                    0.0,
                    1.0);

                byte alpha = (byte)Math.Clamp(
                    Math.Round(
                        255.0 * DiskOpacity *
                        outerFade * opacityVariation),
                    0.0, 255.0);

                int offset = y * stride + x * 4;

                pixels[offset] =
                    ScaleByte(baseColor.B, brightness);

                pixels[offset + 1] =
                    ScaleByte(baseColor.G, brightness);

                pixels[offset + 2] =
                    ScaleByte(baseColor.R, brightness);

                pixels[offset + 3] = alpha;
            }
        }

        BitmapSource bitmap = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            stride);

        if (bitmap.CanFreeze)
            bitmap.Freeze();

        return bitmap;
    }

    private static (double Position, MediaColor Color)[]
        CreateDiskColorStops(MediaColor starColor)
    {
        return
        [
            (0.00, MediaColor.FromRgb(255, 255, 244)),
            (0.10, MediaColor.FromRgb(255, 247, 200)),
            (0.25, MediaColor.FromRgb(255, 220, 128)),
            (0.43, MediaColor.FromRgb(255, 168, 70)),
            (0.61, MediaColor.FromRgb(255, 112, 42)),
            (0.77, BlendColors(
                MediaColor.FromRgb(218, 49, 28), starColor, 0.20)),
            (0.90, MediaColor.FromRgb(155, 35, 25)),
            (1.00, MediaColor.FromRgb(100, 20, 15))
        ];
    }

    private static MediaColor SampleDiskColor(
        (double Position, MediaColor Color)[] stops,
        double position)
    {
        if (position <= stops[0].Position)
            return stops[0].Color;

        for (int i = 1; i < stops.Length; i++)
        {
            if (position > stops[i].Position)
                continue;

            var previous = stops[i - 1];
            var next = stops[i];

            double span = next.Position - previous.Position;

            double t = span <= 0.0
                ? 0.0
                : (position - previous.Position) / span;

            return BlendColors(previous.Color, next.Color, t);
        }

        return stops[^1].Color;
    }

    private static double SamplePeriodicNoise(
        double radialFraction,
        double angularFraction,
        int radialCells,
        int angularCells)
    {
        double x = radialFraction * radialCells;

        double wrappedAngle =
            angularFraction - Math.Floor(angularFraction);

        double y = wrappedAngle * angularCells;

        int x0 = (int)Math.Floor(x);
        int x1 = x0 + 1;

        int y0 = (int)Math.Floor(y);
        int y1 = (y0 + 1) % angularCells;

        double tx = SmoothStep(x - x0);
        double ty = SmoothStep(y - Math.Floor(y));

        double a = HashNoise(x0, y0);
        double b = HashNoise(x1, y0);
        double c = HashNoise(x0, y1);
        double d = HashNoise(x1, y1);

        return Lerp(
            Lerp(a, b, tx),
            Lerp(c, d, tx),
            ty);
    }

    private static double HashNoise(int x, int y)
    {
        uint n = unchecked(
            (uint)x * 374761393u +
            (uint)y * 668265263u);

        n = unchecked(
            (n ^ (n >> 13)) * 1274126177u);

        n ^= n >> 16;

        return (n & 0x00FFFFFF) / 16777215.0;
    }

    private static double SmoothStep(double value)
    {
        value = Math.Clamp(value, 0.0, 1.0);
        return value * value * (3.0 - 2.0 * value);
    }

    private static double Lerp(
        double start,
        double end,
        double t)
    {
        return start + (end - start) * Math.Clamp(t, 0.0, 1.0);
    }

    private static MediaColor BlendColors(
        MediaColor start,
        MediaColor end,
        double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);

        return MediaColor.FromRgb(
            LerpByte(start.R, end.R, t),
            LerpByte(start.G, end.G, t),
            LerpByte(start.B, end.B, t));
    }

    private static byte LerpByte(
        byte start,
        byte end,
        double t)
    {
        return (byte)Math.Clamp(
            Math.Round(start + (end - start) * t),
            0.0,
            255.0);
    }

    private static byte ScaleByte(byte value, double scale)
    {
        return (byte)Math.Clamp(
            Math.Round(value * scale),
            0.0,
            255.0);
    }
}
