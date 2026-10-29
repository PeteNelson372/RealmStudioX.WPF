using RealmStudioShapeRenderingLib;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.WorldGlobe;

/// <summary>
/// A lightweight layered approximation of a nebula. Each cloud is a translucent,
/// emissive plane with a different procedural texture, orientation and depth.
/// This deliberately avoids a full volume renderer while adding parallax and
/// irregular structure beyond a single textured sphere or billboard.
/// </summary>
internal sealed class NebulaLocalStarSceneObject : ILocalStarSceneObject, IWorldGlobeAnimatedObject
{
    private const int MaximumLayerCount = 7;
    private const int TextureSize = 256;

    private readonly Model3DGroup _root = new();
    private readonly Model3DGroup _positionGroup = new();
    private readonly Model3DGroup _orientationGroup = new();
    private readonly Model3DGroup _cloudScaleGroup = new();
    private readonly TranslateTransform3D _translation = new();
    private readonly QuaternionRotation3D _orientationRotation = new(Quaternion.Identity);
    private readonly ScaleTransform3D _cloudScale = new(1, 1, 1);
    private readonly PointLight _light = new();
    private readonly List<CloudLayer> _layers = new(MaximumLayerCount);

    private Color _lastColor;
    private double _width = 18.0;
    private double _height = 12.0;
    private double _depth = 5.0;
    private double _opacity = 0.9;
    private double _brightness = 1.0;
    private double _detailIntensity = 1.0;
    private double _rotationSpeed = 1.2;
    private double _driftSpeed = 0.25;
    private int _layerCount = 5;

    public LocalStarType Type => LocalStarType.Nebula;

    public Model3DGroup Root => _root;

    public NebulaLocalStarSceneObject(LocalStar star, MeshGeometry3D unitSphereMesh)
    {
        ArgumentNullException.ThrowIfNull(star);
        // The unit sphere parameter is part of the common scene-object construction
        // pattern; a nebula is represented by planes and does not use the sphere.
        ArgumentNullException.ThrowIfNull(unitSphereMesh);

        _root.Transform = star.LocationTransform;
        _positionGroup.Transform = _translation;
        _orientationGroup.Transform = new RotateTransform3D(_orientationRotation);
        _cloudScaleGroup.Transform = _cloudScale;

        _root.Children.Add(_positionGroup);
        _positionGroup.Children.Add(_orientationGroup);
        _orientationGroup.Children.Add(_cloudScaleGroup);

        BuildLayers(star.StarColor);

        _light.Position = new Point3D(0, 0, 0);
        _light.Color = Colors.White;
        _light.ConstantAttenuation = 1.0;
        _light.LinearAttenuation = 0.0;
        _orientationGroup.Children.Add(_light);

        Update(star);
    }

    public void Update(LocalStar star)
    {
        ArgumentNullException.ThrowIfNull(star);

        NebulaSettings settings = GetSettings(star);

        _root.Transform = star.LocationTransform;
        _translation.OffsetX = star.Center.X;
        _translation.OffsetY = star.Center.Y;
        _translation.OffsetZ = star.Center.Z;
        _orientationRotation.Quaternion = star.RotationQuaternion;

        double radius = Math.Max(0.001, star.Radius);
        _cloudScale.ScaleX = radius;
        _cloudScale.ScaleY = radius;
        _cloudScale.ScaleZ = radius;

        bool textureChanged = _lastColor != star.StarColor;
        bool detailChanged = Math.Abs(_detailIntensity - Clamp(settings.DetailIntensity, 1.0, 0.0, 2.0)) > 0.0001
            || Math.Abs(_brightness - Clamp(settings.Brightness, 1.0, 0.0, 3.0)) > 0.0001;

        ApplySettings(settings);

        if (textureChanged || detailChanged)
            RefreshTextures(star.StarColor);

        _lastColor = star.StarColor;

        _light.Color = ScaleColor(star.StarColor, Clamp(star.LightIntensity, 0.1, 0.0, 5.0));
        double distance = (star.Center - new Point3D(0, 0, 0)).Length;
        double effectiveRange = Math.Max(Math.Max(0.1, star.LightRange), distance + 2.0);
        _light.QuadraticAttenuation = 1.0 / (effectiveRange * effectiveRange);
    }

    private void BuildLayers(Color baseColor)
    {
        // Different deterministic seeds, proportions, tilts and offsets make the
        // layers read as overlapping cloud structures rather than one flat image.
        double[] widthFactors = [1.00, 0.82, 0.72, 0.58, 1.08, 0.48, 0.66];
        double[] heightFactors = [0.76, 0.90, 0.64, 0.82, 0.58, 0.56, 0.70];
        double[] tiltX = [-11, 17, 5, -24, 29, 9, -16];
        double[] tiltY = [8, -13, 22, 6, -19, 31, -4];
        double[] tiltZ = [-12, 21, 4, 37, -28, 52, -44];
        double[] offsetsX = [-0.06, 0.08, -0.13, 0.16, 0.02, -0.20, 0.12];
        double[] offsetsY = [0.03, -0.12, 0.14, 0.01, 0.17, -0.04, -0.16];
        Color[] accents = [
            Color.FromRgb(130, 95, 255),
            Color.FromRgb(35, 180, 255),
            Color.FromRgb(255, 85, 174),
            Color.FromRgb(130, 65, 220),
            Color.FromRgb(255, 130, 90),
            Color.FromRgb(70, 130, 255),
            Color.FromRgb(255, 75, 135)
        ];

        for (int i = 0; i < MaximumLayerCount; i++)
        {
            var rotationX = new AxisAngleRotation3D(new Vector3D(1, 0, 0), tiltX[i]);
            var rotationY = new AxisAngleRotation3D(new Vector3D(0, 1, 0), tiltY[i]);
            var rotationZ = new AxisAngleRotation3D(new Vector3D(0, 0, 1), tiltZ[i]);
            var drift = new TranslateTransform3D(offsetsX[i], offsetsY[i], (i - (MaximumLayerCount - 1) / 2.0) * 0.08);
            var transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(rotationX));
            transform.Children.Add(new RotateTransform3D(rotationY));
            transform.Children.Add(new RotateTransform3D(rotationZ));
            transform.Children.Add(drift);

            var brush = new ImageBrush(CreateCloudTexture(Blend(baseColor, accents[i], 0.42), i + 101, 1.0, 1.0))
            {
                Stretch = Stretch.Fill,
                Opacity = 1.0,
                TileMode = TileMode.None
            };
            // Keep the brush mutable: ApplySettings updates opacity at runtime.
            var material = new EmissiveMaterial(brush);
            var mesh = CreatePlaneMesh(18.0 * widthFactors[i], 12.0 * heightFactors[i]);
            var model = new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material,
                Transform = transform
            };

            var layerGroup = new Model3DGroup();
            layerGroup.Children.Add(model);
            _cloudScaleGroup.Children.Add(layerGroup);
            _layers.Add(new CloudLayer(layerGroup, model, brush, transform, rotationZ, drift, widthFactors[i], heightFactors[i], i, tiltZ[i]));
        }
    }

    private void ApplySettings(NebulaSettings settings)
    {
        _layerCount = (int)Clamp(settings.LayerCount, 5, 1, MaximumLayerCount);
        _width = Clamp(settings.Width, 18.0, 1.0, 60.0);
        _height = Clamp(settings.Height, 12.0, 1.0, 60.0);
        _depth = Clamp(settings.Depth, 5.0, 0.0, 20.0);
        _opacity = Clamp(settings.Opacity, 0.9, 0.0, 1.0);
        _brightness = Clamp(settings.Brightness, 1.0, 0.0, 3.0);
        _detailIntensity = Clamp(settings.DetailIntensity, 1.0, 0.0, 2.0);
        _rotationSpeed = Clamp(settings.RotationSpeed, 1.2, -2.0, 2.0);
        _driftSpeed = Clamp(settings.DriftSpeed, 0.25, 0.0, 0.5);

        for (int i = 0; i < _layers.Count; i++)
        {
            CloudLayer layer = _layers[i];
            layer.Brush.Opacity = _opacity * (i == 0 ? 1.0 : 0.82);
            layer.Brush.Opacity *= _brightness <= 0.0 ? 0.0 : Math.Min(1.0, 0.72 + _brightness * 0.28);
            layer.Model.Geometry = CreatePlaneMesh(_width * layer.WidthFactor, _height * layer.HeightFactor);
            layer.Drift.OffsetZ = _depth <= 0.0001
                ? 0.0
                : ((i / (double)Math.Max(1, MaximumLayerCount - 1)) - 0.5) * _depth;
            SetChildEnabled(_cloudScaleGroup, layer.Group, i < _layerCount);
        }
    }

    private void RefreshTextures(Color baseColor)
    {
        Color[] accents = [
            Color.FromRgb(130, 95, 255), Color.FromRgb(35, 180, 255),
            Color.FromRgb(255, 85, 174), Color.FromRgb(130, 65, 220),
            Color.FromRgb(255, 130, 90), Color.FromRgb(70, 130, 255),
            Color.FromRgb(255, 75, 135)
        ];

        for (int i = 0; i < _layers.Count; i++)
        {
            _layers[i].Brush.ImageSource = CreateCloudTexture(
                Blend(baseColor, accents[i], 0.42), i + 101, _brightness, _detailIntensity);
        }
    }

    public void AdvanceAnimation(WorldGlobeAnimationFrame frame)
    {
        // Animation enablement is controlled globally by WorldGlobeSceneViewModel.
        // RotationSpeed is expressed in degrees per second.
        // DriftSpeed controls the angular rate of a bounded, looping drift cycle
        // in radians per second, rather than acting as a tiny displacement value.
        double driftPhase = frame.TimeSeconds * _driftSpeed;

        for (int i = 0; i < _layers.Count; i++)
        {
            CloudLayer layer = _layers[i];
            double direction = (i % 2 == 0) ? 1.0 : -1.0;
            layer.RotationZ.Angle =
                layer.BaseRotationDegrees + direction * _rotationSpeed * frame.TimeSeconds;

            if (_driftSpeed <= 0.0)
            {
                layer.Drift.OffsetX = layer.BaseOffsetX;
                layer.Drift.OffsetY = layer.BaseOffsetY;
                continue;
            }

            // Subtract each layer's starting phase so changing to this motion
            // formula does not cause an initial positional jump at time zero.
            double phaseOffset = layer.Index * 1.37;
            double xDisplacement =
                (Math.Sin(phaseOffset + driftPhase) - Math.Sin(phaseOffset)) * 0.12;
            double yDisplacement =
                (Math.Cos(phaseOffset + driftPhase * 0.78) - Math.Cos(phaseOffset)) * 0.08;

            layer.Drift.OffsetX = layer.BaseOffsetX + xDisplacement;
            layer.Drift.OffsetY = layer.BaseOffsetY + yDisplacement;
        }
    }

    public void ResetAnimation()
    {
        foreach (CloudLayer layer in _layers)
        {
            layer.RotationZ.Angle = layer.BaseRotationDegrees;
            layer.Drift.OffsetX = layer.BaseOffsetX;
            layer.Drift.OffsetY = layer.BaseOffsetY;
        }
    }

    private static BitmapSource CreateCloudTexture(Color tint, int seed, double brightness, double detailIntensity)
    {
        int stride = TextureSize * 4;
        byte[] pixels = new byte[TextureSize * TextureSize * 4];
        double detail = Clamp(detailIntensity, 1.0, 0.0, 2.0);
        double threshold = 0.48 - detail * 0.10;
        double intensity = Clamp(brightness, 1.0, 0.0, 3.0);

        for (int y = 0; y < TextureSize; y++)
        {
            double ny = (y / (double)(TextureSize - 1)) * 2.0 - 1.0;
            for (int x = 0; x < TextureSize; x++)
            {
                double nx = (x / (double)(TextureSize - 1)) * 2.0 - 1.0;
                double warpX = (ValueNoise(nx * 2.4 + 19.0, ny * 2.4 - 7.0, seed + 17) - 0.5) * 0.26;
                double warpY = (ValueNoise(nx * 2.4 - 11.0, ny * 2.4 + 5.0, seed + 31) - 0.5) * 0.26;
                double noise = 0.52 * FractalNoise((nx + warpX) * 3.1, (ny + warpY) * 3.1, seed)
                    + 0.32 * FractalNoise((nx + warpX) * 7.0, (ny + warpY) * 7.0, seed + 101)
                    + 0.16 * ValueNoise((nx + warpX) * 15.0, (ny + warpY) * 15.0, seed + 211);

                double distance = Math.Sqrt(nx * nx * 0.82 + ny * ny * 1.15);
                double envelope = 1.0 - SmoothStep(0.64, 1.24, distance);
                double cloud = SmoothStep(threshold - 0.12, threshold + 0.14, noise);
                double wisps = 0.72 + 0.28 * ValueNoise(nx * 11.0, ny * 11.0, seed + 509);

                // Fade alpha to exactly zero at every texture edge. Because the
                // square texture is mapped onto a rectangular plane, this radial
                // falloff reads as an elliptical soft edge in the rendered scene.
                double edgeDistance = Math.Sqrt(nx * nx + ny * ny);
                double edgeFade = 1.0 - SmoothStep(0.60, 1.0, edgeDistance);
                double alpha = Math.Clamp(envelope * cloud * wisps * edgeFade * 0.88, 0.0, 0.92);
                double light = Math.Clamp((0.42 + noise * 1.22) * intensity, 0.0, 2.5);
                // A restrained central glow helps the overlapping images read as luminous gas.
                light *= 0.82 + 0.18 * (1.0 - Math.Clamp(distance, 0.0, 1.0));

                int index = y * stride + x * 4;
                pixels[index] = ToByte(tint.B * light);
                pixels[index + 1] = ToByte(tint.G * light);
                pixels[index + 2] = ToByte(tint.R * light);
                pixels[index + 3] = ToByte(alpha * 255.0);
            }
        }

        var bitmap = new WriteableBitmap(TextureSize, TextureSize, 96, 96, PixelFormats.Bgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, TextureSize, TextureSize), pixels, stride, 0);
        bitmap.Freeze();
        return bitmap;
    }

    private static MeshGeometry3D CreatePlaneMesh(double width, double height)
    {
        double halfWidth = width / 2.0;
        double halfHeight = height / 2.0;
        var mesh = new MeshGeometry3D();
        mesh.Positions.Add(new Point3D(-halfWidth, -halfHeight, 0));
        mesh.Positions.Add(new Point3D(halfWidth, -halfHeight, 0));
        mesh.Positions.Add(new Point3D(halfWidth, halfHeight, 0));
        mesh.Positions.Add(new Point3D(-halfWidth, halfHeight, 0));
        mesh.TextureCoordinates.Add(new Point(0, 1));
        mesh.TextureCoordinates.Add(new Point(1, 1));
        mesh.TextureCoordinates.Add(new Point(1, 0));
        mesh.TextureCoordinates.Add(new Point(0, 0));
        mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(1); mesh.TriangleIndices.Add(2);
        mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(2); mesh.TriangleIndices.Add(3);
        mesh.Normals.Add(new Vector3D(0, 0, 1));
        mesh.Normals.Add(new Vector3D(0, 0, 1));
        mesh.Normals.Add(new Vector3D(0, 0, 1));
        mesh.Normals.Add(new Vector3D(0, 0, 1));
        mesh.Freeze();
        return mesh;
    }

    private static double FractalNoise(double x, double y, int seed)
    {
        double sum = 0.0;
        double weightSum = 0.0;
        double amplitude = 0.5;
        for (int octave = 0; octave < 4; octave++)
        {
            sum += ValueNoise(x, y, seed + octave * 71) * amplitude;
            weightSum += amplitude;
            x *= 2.03;
            y *= 2.03;
            amplitude *= 0.5;
        }
        return sum / weightSum;
    }

    private static double ValueNoise(double x, double y, int seed)
    {
        int x0 = (int)Math.Floor(x); int y0 = (int)Math.Floor(y);
        double tx = x - x0; double ty = y - y0;
        tx = tx * tx * (3.0 - 2.0 * tx);
        ty = ty * ty * (3.0 - 2.0 * ty);
        double a = Hash01(x0, y0, seed);
        double b = Hash01(x0 + 1, y0, seed);
        double c = Hash01(x0, y0 + 1, seed);
        double d = Hash01(x0 + 1, y0 + 1, seed);
        return Lerp(Lerp(a, b, tx), Lerp(c, d, tx), ty);
    }

    private static double Hash01(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)seed;
            h ^= (uint)x * 374761393u;
            h = (h << 13) | (h >> 19);
            h ^= (uint)y * 668265263u;
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0x00FFFFFFu) / 16777215.0;
        }
    }

    private static Color Blend(Color a, Color b, double amount)
    {
        amount = Math.Clamp(amount, 0.0, 1.0);
        return Color.FromRgb(
            ToByte(a.R * (1.0 - amount) + b.R * amount),
            ToByte(a.G * (1.0 - amount) + b.G * amount),
            ToByte(a.B * (1.0 - amount) + b.B * amount));
    }

    private static Color ScaleColor(Color color, double scale) => Color.FromRgb(
        ToByte(color.R * scale), ToByte(color.G * scale), ToByte(color.B * scale));

    private static byte ToByte(double value) => (byte)Math.Clamp((int)Math.Round(value), 0, 255);
    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
    private static double SmoothStep(double edge0, double edge1, double x)
    {
        double t = Math.Clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
        return t * t * (3.0 - 2.0 * t);
    }

    private static double Clamp(double value, double fallback, double minimum, double maximum) =>
        double.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : fallback;

    private static void SetChildEnabled(Model3DGroup parent, Model3D child, bool enabled)
    {
        bool present = parent.Children.Contains(child);
        if (enabled && !present) parent.Children.Add(child);
        else if (!enabled && present) parent.Children.Remove(child);
    }

    private static NebulaSettings GetSettings(LocalStar star) =>
        star.LocalStarDetailSettings as NebulaSettings ?? new NebulaSettings();

    private sealed class CloudLayer(
        Model3DGroup group,
        GeometryModel3D model,
        ImageBrush brush,
        Transform3DGroup transform,
        AxisAngleRotation3D rotationZ,
        TranslateTransform3D drift,
        double widthFactor,
        double heightFactor,
        int index,
        double baseRotationDegrees)
    {
        public Model3DGroup Group { get; } = group;
        public GeometryModel3D Model { get; } = model;
        public ImageBrush Brush { get; } = brush;
        public Transform3DGroup Transform { get; } = transform;
        public AxisAngleRotation3D RotationZ { get; } = rotationZ;
        public TranslateTransform3D Drift { get; } = drift;
        public double WidthFactor { get; } = widthFactor;
        public double HeightFactor { get; } = heightFactor;
        public int Index { get; } = index;
        public double BaseRotationDegrees { get; } = baseRotationDegrees;
        public double BaseOffsetX { get; } = drift.OffsetX;
        public double BaseOffsetY { get; } = drift.OffsetY;
    }
}
