using RealmStudioShapeRenderingLib;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Celestial;

internal sealed class BlackHoleCelestialBodyRenderer :
    LocalStarCelestialBodyRenderer
{
    private const int DiskAngularSegments = 384;
    private const int DiskRadialSegments = 64;
    private const double DiskOpacity = 0.58;

    private readonly GeometryModel3D _eventHorizon = new();
    private readonly GeometryModel3D _photonRing = new();
    private readonly GeometryModel3D _accretionDisk = new();

    private readonly LinearGradientBrush _photonRingBrush;
    private readonly ImageBrush _accretionDiskBrush;

    private Color _lastDiskColor;
    private WorldRotationDirection _lastDiskDirection;

    public override CelestialBodyRendererType Type =>
        CelestialBodyRendererType.BlackHole;

    public BlackHoleCelestialBodyRenderer()
    {
        Material blackMaterial = CreateBlackMaterial();

        _eventHorizon.Geometry = CreateSphereMesh();
        _eventHorizon.Material = blackMaterial;
        _eventHorizon.BackMaterial = blackMaterial;

        _photonRingBrush = CreatePhotonRingBrush(Colors.Yellow);

        Material photonRingMaterial =
            CreateEmissiveMaterial(_photonRingBrush);

        _photonRing.Geometry = CreateAnnulusMesh(0.98, 1.075);
        _photonRing.Material = photonRingMaterial;
        _photonRing.BackMaterial = photonRingMaterial;

        _accretionDiskBrush = new ImageBrush
        {
            Stretch = Stretch.Fill,
            TileMode = TileMode.None,
            Opacity = 1.0
        };

        Material diskMaterial =
            CreateEmissiveMaterial(_accretionDiskBrush);

        _accretionDisk.Geometry =
            CreateAccretionDiskMesh(1.075, 3.25);

        _accretionDisk.Material = diskMaterial;
        _accretionDisk.BackMaterial = diskMaterial;

        // The order matters: the photon ring and disk sit around
        // the event horizon, which remains opaque black.
        BodyVisualRoot.Children.Add(_photonRing);
        BodyVisualRoot.Children.Add(_accretionDisk);
        BodyVisualRoot.Children.Add(_eventHorizon);
    }

    protected override void UpdateBodyVisuals(
        CelestialBody body,
        ImageSource? texture)
    {
        _photonRingBrush.GradientStops[3].Color =
            Color.FromArgb(
                190,
                body.BaseColor.R,
                body.BaseColor.G,
                body.BaseColor.B);

        if (_lastDiskColor != body.BaseColor)
        {
            _accretionDiskBrush.ImageSource =
                CreateAccretionDiskTexture(body.BaseColor);

            _lastDiskColor = body.BaseColor;
        }
    }

    private static Material CreateBlackMaterial()
    {
        SolidColorBrush brush =
            new(Colors.Black);

        MaterialGroup material = new();
        material.Children.Add(new DiffuseMaterial(brush));
        material.Children.Add(new EmissiveMaterial(brush));

        return material;
    }

    private static Material CreateEmissiveMaterial(Brush brush)
    {
        MaterialGroup material = new();
        material.Children.Add(new DiffuseMaterial(brush));
        material.Children.Add(new EmissiveMaterial(brush));

        return material;
    }

    private static LinearGradientBrush CreatePhotonRingBrush(
        Color starColor)
    {
        LinearGradientBrush brush = new()
        {
            StartPoint = new Point(0, 0.5),
            EndPoint = new Point(1, 0.5),
            MappingMode = BrushMappingMode.RelativeToBoundingBox
        };

        brush.GradientStops.Add(
            new GradientStop(
                Color.FromArgb(0, 255, 255, 255), 0.00));

        brush.GradientStops.Add(
            new GradientStop(Colors.White, 0.25));

        brush.GradientStops.Add(
            new GradientStop(
                Color.FromRgb(255, 252, 235), 0.48));

        brush.GradientStops.Add(
            new GradientStop(
                Color.FromArgb(
                    190,
                    starColor.R,
                    starColor.G,
                    starColor.B),
                0.75));

        brush.GradientStops.Add(
            new GradientStop(
                Color.FromArgb(0, 255, 255, 255), 1.00));

        return brush;
    }

    private static MeshGeometry3D CreateSphereMesh()
    {
        const int latitudeSegments = 48;
        const int longitudeSegments = 64;

        MeshGeometry3D mesh = new();
        int stride = longitudeSegments + 1;

        for (int latitude = 0;
             latitude <= latitudeSegments;
             latitude++)
        {
            double phi = Math.PI * latitude / latitudeSegments;
            double sinPhi = Math.Sin(phi);
            double cosPhi = Math.Cos(phi);

            for (int longitude = 0;
                 longitude <= longitudeSegments;
                 longitude++)
            {
                double theta =
                    2.0 * Math.PI * longitude / longitudeSegments;

                double x = sinPhi * Math.Cos(theta);
                double y = cosPhi;
                double z = sinPhi * Math.Sin(theta);

                mesh.Positions.Add(new Point3D(x, y, z));
                mesh.Normals.Add(new Vector3D(x, y, z));

                mesh.TextureCoordinates.Add(
                    new Point(
                        longitude / (double)longitudeSegments,
                        latitude / (double)latitudeSegments));
            }
        }

        for (int latitude = 0;
             latitude < latitudeSegments;
             latitude++)
        {
            for (int longitude = 0;
                 longitude < longitudeSegments;
                 longitude++)
            {
                int current = latitude * stride + longitude;
                int next = current + stride;

                mesh.TriangleIndices.Add(current);
                mesh.TriangleIndices.Add(next);
                mesh.TriangleIndices.Add(current + 1);

                mesh.TriangleIndices.Add(current + 1);
                mesh.TriangleIndices.Add(next);
                mesh.TriangleIndices.Add(next + 1);
            }
        }

        return mesh;
    }

    private static MeshGeometry3D CreateAnnulusMesh(
        double innerRadius,
        double outerRadius)
    {
        MeshGeometry3D mesh = new();

        for (int i = 0; i <= DiskAngularSegments; i++)
        {
            double angle =
                2.0 * Math.PI * i / DiskAngularSegments;

            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            double v = (double)i / DiskAngularSegments;

            mesh.Positions.Add(new Point3D(
                innerRadius * cos, 0, innerRadius * sin));

            mesh.TextureCoordinates.Add(new Point(0, v));
            mesh.Normals.Add(new Vector3D(0, 1, 0));

            mesh.Positions.Add(new Point3D(
                outerRadius * cos, 0, outerRadius * sin));

            mesh.TextureCoordinates.Add(new Point(1, v));
            mesh.Normals.Add(new Vector3D(0, 1, 0));
        }

        for (int i = 0; i < DiskAngularSegments; i++)
        {
            int innerCurrent = i * 2;
            int outerCurrent = innerCurrent + 1;
            int innerNext = innerCurrent + 2;
            int outerNext = innerCurrent + 3;

            mesh.TriangleIndices.Add(innerCurrent);
            mesh.TriangleIndices.Add(innerNext);
            mesh.TriangleIndices.Add(outerCurrent);

            mesh.TriangleIndices.Add(innerNext);
            mesh.TriangleIndices.Add(outerNext);
            mesh.TriangleIndices.Add(outerCurrent);
        }

        return mesh;
    }

    private static MeshGeometry3D CreateAccretionDiskMesh(
        double innerRadius,
        double outerRadius)
    {
        MeshGeometry3D mesh = new();
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

                double heightNoise = SamplePeriodicNoise(
                    radialFraction, angularFraction, 9, 17);

                double fineHeightNoise = SamplePeriodicNoise(
                    radialFraction, angularFraction, 25, 41);

                double angle =
                    2.0 * Math.PI * angularFraction;

                double y =
                    (heightNoise - 0.5) * 0.045
                    + (fineHeightNoise - 0.5) * 0.018
                    + Math.Sin(
                        angle * 3.0 + radialFraction * 8.0) * 0.006;

                mesh.Positions.Add(new Point3D(
                    radius * Math.Cos(angle),
                    y,
                    radius * Math.Sin(angle)));

                mesh.TextureCoordinates.Add(
                    new Point(radialFraction, angularFraction));

                mesh.Normals.Add(new Vector3D(0, 1, 0));
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

                mesh.TriangleIndices.Add(innerCurrent);
                mesh.TriangleIndices.Add(innerNext);
                mesh.TriangleIndices.Add(outerCurrent);

                mesh.TriangleIndices.Add(innerNext);
                mesh.TriangleIndices.Add(outerNext);
                mesh.TriangleIndices.Add(outerCurrent);
            }
        }

        return mesh;
    }

    private static BitmapSource CreateAccretionDiskTexture(Color starColor)
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

                Color baseColor = SampleDiskColor(
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

                pixels[offset] = ScaleByte(baseColor.B, brightness);
                pixels[offset + 1] = ScaleByte(baseColor.G, brightness);
                pixels[offset + 2] = ScaleByte(baseColor.R, brightness);
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

    private static (double Position, Color Color)[]
        CreateDiskColorStops(Color starColor)
    {
        return
        [
            (0.00, Color.FromRgb(255, 255, 244)),
            (0.10, Color.FromRgb(255, 247, 200)),
            (0.25, Color.FromRgb(255, 220, 128)),
            (0.43, Color.FromRgb(255, 168, 70)),
            (0.61, Color.FromRgb(255, 112, 42)),
            (0.77, BlendColors(
                Color.FromRgb(218, 49, 28), starColor, 0.20)),
            (0.90, Color.FromRgb(155, 35, 25)),
            (1.00, Color.FromRgb(100, 20, 15))
        ];
    }

    private static Color SampleDiskColor(
        (double Position, Color Color)[] stops,
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

    private static double Lerp(double start, double end, double t)
    {
        return start + (end - start) * Math.Clamp(t, 0.0, 1.0);
    }

    private static Color BlendColors(
        Color start,
        Color end,
        double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);

        return Color.FromRgb(
            LerpByte(start.R, end.R, t),
            LerpByte(start.G, end.G, t),
            LerpByte(start.B, end.B, t));
    }

    private static byte LerpByte(byte start, byte end, double t)
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