using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Celestial
{
    internal sealed class NebulaCelestialBodyRenderer :
        ICelestialBodyRenderer,
        ICameraFacingCelestialObject,
        ICelestialAnimatedObject
    {
        private const int MaximumLayerCount = 7;
        private const int TextureSize = 256;

        private readonly Model3DGroup _root = new();
        private readonly Model3DGroup _billboardGroup = new();
        private readonly Model3DGroup _cloudScaleGroup = new();

        private readonly QuaternionRotation3D _billboardRotation =
            new(Quaternion.Identity);

        private readonly ScaleTransform3D _cloudScale =
            new(1, 1, 1);

        private readonly PointLight _light = new();

        private readonly List<CloudLayer> _layers = new(MaximumLayerCount);

        private Color _lastColor = Colors.Transparent;

        private readonly Func<CelestialTextureSource?, ImageSource?> _textureResolver;

        private double _width = 18.0;
        private double _height = 12.0;
        private double _depth = 5.0;
        private double _opacity = 0.9;
        private double _brightness = 1.0;
        private double _detailIntensity = 1.0;
        private double _rotationSpeed = 1.2;
        private double _driftSpeed = 0.25;

        private int _layerCount = 5;

        public CelestialBodyRendererType Type =>
            CelestialBodyRendererType.Nebula;

        public Model3DGroup Root => _root;

        public NebulaCelestialBodyRenderer(
            Func<CelestialTextureSource?, ImageSource?> textureResolver)
        {
            _textureResolver = textureResolver
                ?? throw new ArgumentNullException(nameof(textureResolver));
            _root.Children.Add(_billboardGroup);

            _billboardGroup.Transform =
                new RotateTransform3D(_billboardRotation);

            _billboardGroup.Children.Add(_cloudScaleGroup);
            _cloudScaleGroup.Transform = _cloudScale;

            BuildLayers(Colors.White);

            _light.Position = new Point3D(0, 0, 0);
            _light.Color = Colors.White;
            _light.ConstantAttenuation = 1.0;
            _light.LinearAttenuation = 0.0;

            // Keep lighting independent of billboard orientation and cloud scale.
            _root.Children.Add(_light);
        }

        public void Update(CelestialBodyNode node, CelestialTextureSource? textureSource)
        {
            ArgumentNullException.ThrowIfNull(node);
            ArgumentNullException.ThrowIfNull(node.Body);
            ArgumentNullException.ThrowIfNull(node.Placement);

            CelestialBody body = node.Body;
            ImageSource? texture = _textureResolver(textureSource);
            _ = texture; // Current nebula layers are generated procedurally.

            NebulaAnimationSettings settings = GetNebulaSettings(body);

            double size = Math.Max(0.001,
                double.IsFinite(node.Placement.Size)
                    ? node.Placement.Size
                    : 1.0);

            _cloudScale.ScaleX = size;
            _cloudScale.ScaleY = size;
            _cloudScale.ScaleZ = size;

            bool textureChanged = _lastColor != body.BaseColor;

            bool detailChanged =
                Math.Abs(_detailIntensity -
                    Clamp(settings.DetailIntensity, 1.0, 0.0, 2.0)) > 0.0001 ||
                Math.Abs(_brightness -
                    Clamp(settings.Brightness, 1.0, 0.0, 3.0)) > 0.0001;

            ApplySettings(settings);

            if (textureChanged || detailChanged)
                RefreshTextures(body.BaseColor);

            _lastColor = body.BaseColor;

            CelestialPointLightSettings lightSettings = body.PointLightSettings;

            if (lightSettings.Enabled)
            {
                double intensity = Math.Clamp(
                    lightSettings.Intensity, 0.0, 1.0);

                double range = Math.Max(
                    0.001, lightSettings.Range);

                _light.Color = ScaleColor(body.BaseColor, intensity);

                _light.ConstantAttenuation = 0.0;
                _light.LinearAttenuation = 0.0;
                _light.QuadraticAttenuation = 1.0 / (range * range);

                if (!_root.Children.Contains(_light))
                    _root.Children.Add(_light);
            }
            else
            {
                _root.Children.Remove(_light);
            }
        }

        private static NebulaAnimationSettings GetNebulaSettings(
            CelestialBody body)
        {
            CelestialBodyAnimation? animation =
                body.Animations.FirstOrDefault(
                    item => item.AnimationType ==
                            CelestialBodyAnimationType.NebulaActivity);

            return animation?.Settings as NebulaAnimationSettings
                ?? new NebulaAnimationSettings();
        }

        public void UpdateCamera(Camera? camera)
        {
            if (camera is not ProjectionCamera projectionCamera)
                return;

            // The Nebula is a Local Star at the origin.
            // Its billboard therefore faces from the origin toward the camera.
            Vector3D directionToCamera =
                projectionCamera.Position - new Point3D(0, 0, 0);

            if (directionToCamera.LengthSquared < 0.000001)
                return;

            directionToCamera.Normalize();

            Vector3D localNormal = new(0, 0, 1);

            double dot = Vector3D.DotProduct(
                localNormal,
                directionToCamera);

            dot = Math.Clamp(dot, -1.0, 1.0);

            Vector3D axis = Vector3D.CrossProduct(
                localNormal,
                directionToCamera);

            if (axis.LengthSquared < 0.000001)
            {
                _billboardRotation.Quaternion =
                    dot >= 0.0
                        ? Quaternion.Identity
                        : new Quaternion(new Vector3D(0, 1, 0), 180.0);

                return;
            }

            axis.Normalize();

            double angle = Math.Acos(dot) * 180.0 / Math.PI;

            _billboardRotation.Quaternion =
                new Quaternion(axis, angle);
        }

        private void BuildLayers(Color baseColor)
        {
            double[] widthFactors =
                [1.00, 0.82, 0.72, 0.58, 1.08, 0.48, 0.66];

            double[] heightFactors =
                [0.76, 0.90, 0.64, 0.82, 0.58, 0.56, 0.70];

            double[] tiltX = [-11, 17, 5, -24, 29, 9, -16];
            double[] tiltY = [8, -13, 22, 6, -19, 31, -4];
            double[] tiltZ = [-12, 21, 4, 37, -28, 52, -44];

            double[] offsetsX =
                [-0.06, 0.08, -0.13, 0.16, 0.02, -0.20, 0.12];

            double[] offsetsY =
                [0.03, -0.12, 0.14, 0.01, 0.17, -0.04, -0.16];

            Color[] accents =
            [
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
                AxisAngleRotation3D rotationX =
                    new(new Vector3D(1, 0, 0), tiltX[i]);

                AxisAngleRotation3D rotationY =
                    new(new Vector3D(0, 1, 0), tiltY[i]);

                AxisAngleRotation3D rotationZ =
                    new(new Vector3D(0, 0, 1), tiltZ[i]);

                TranslateTransform3D drift =
                    new(offsetsX[i], offsetsY[i],
                        (i - (MaximumLayerCount - 1) / 2.0) * 0.08);

                Transform3DGroup transform = new();

                transform.Children.Add(new RotateTransform3D(rotationX));
                transform.Children.Add(new RotateTransform3D(rotationY));
                transform.Children.Add(new RotateTransform3D(rotationZ));
                transform.Children.Add(drift);

                ImageBrush brush = new(
                    CreateCloudTexture(
                        Blend(baseColor, accents[i], 0.42),
                        i + 101,
                        1.0,
                        1.0))
                {
                    Stretch = Stretch.Fill,
                    Opacity = 1.0,
                    TileMode = TileMode.None
                };

                EmissiveMaterial material = new(brush);

                GeometryModel3D model = new()
                {
                    Geometry = CreatePlaneMesh(
                        18.0 * widthFactors[i],
                        12.0 * heightFactors[i]),

                    Material = material,
                    BackMaterial = material,
                    Transform = transform
                };

                Model3DGroup layerGroup = new();
                layerGroup.Children.Add(model);

                _cloudScaleGroup.Children.Add(layerGroup);

                _layers.Add(new CloudLayer(
                    layerGroup,
                    model,
                    brush,
                    rotationZ,
                    drift,
                    widthFactors[i],
                    heightFactors[i],
                    i,
                    tiltZ[i]));
            }
        }

        private void ApplySettings(NebulaAnimationSettings settings)
        {
            _layerCount = (int)Clamp(
                settings.LayerCount, 5, 1, MaximumLayerCount);

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

                layer.Brush.Opacity =
                    _opacity * (i == 0 ? 1.0 : 0.82);

                layer.Brush.Opacity *=
                    _brightness <= 0.0
                        ? 0.0
                        : Math.Min(1.0, 0.72 + _brightness * 0.28);

                layer.Model.Geometry = CreatePlaneMesh(
                    _width * layer.WidthFactor,
                    _height * layer.HeightFactor);

                layer.Drift.OffsetZ =
                    _depth <= 0.0001
                        ? 0.0
                        : ((i / (double)Math.Max(
                            1, MaximumLayerCount - 1)) - 0.5) * _depth;

                SetChildEnabled(
                    _cloudScaleGroup,
                    layer.Group,
                    i < _layerCount);
            }
        }

        private void RefreshTextures(Color baseColor)
        {
            Color[] accents =
            [
                Color.FromRgb(130, 95, 255),
                Color.FromRgb(35, 180, 255),
                Color.FromRgb(255, 85, 174),
                Color.FromRgb(130, 65, 220),
                Color.FromRgb(255, 130, 90),
                Color.FromRgb(70, 130, 255),
                Color.FromRgb(255, 75, 135)
            ];

            for (int i = 0; i < _layers.Count; i++)
            {
                _layers[i].Brush.ImageSource = CreateCloudTexture(
                    Blend(baseColor, accents[i], 0.42),
                    i + 101,
                    _brightness,
                    _detailIntensity);
            }
        }

        public void AdvanceAnimation(CelestialAnimationFrame frame)
        {
            if (frame.DeltaSeconds <= 0.0)
                return;

            double driftPhase = frame.TimeSeconds * _driftSpeed;

            for (int i = 0; i < _layers.Count; i++)
            {
                CloudLayer layer = _layers[i];

                double direction = i % 2 == 0 ? 1.0 : -1.0;

                layer.RotationZ.Angle =
                    layer.BaseRotationDegrees +
                    direction * _rotationSpeed * frame.TimeSeconds;

                if (_driftSpeed <= 0.0)
                {
                    layer.Drift.OffsetX = layer.BaseOffsetX;
                    layer.Drift.OffsetY = layer.BaseOffsetY;
                    continue;
                }

                double phaseOffset = layer.Index * 1.37;

                double xDisplacement =
                    (Math.Sin(phaseOffset + driftPhase) -
                     Math.Sin(phaseOffset)) * 0.12;

                double yDisplacement =
                    (Math.Cos(phaseOffset + driftPhase * 0.78) -
                     Math.Cos(phaseOffset)) * 0.08;

                layer.Drift.OffsetX =
                    layer.BaseOffsetX + xDisplacement;

                layer.Drift.OffsetY =
                    layer.BaseOffsetY + yDisplacement;
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

        private static BitmapSource CreateCloudTexture(
            Color tint,
            int seed,
            double brightness,
            double detailIntensity)
        {
            int stride = TextureSize * 4;
            byte[] pixels = new byte[TextureSize * TextureSize * 4];

            double detail = Clamp(detailIntensity, 1.0, 0.0, 2.0);
            double threshold = 0.48 - detail * 0.10;
            double intensity = Clamp(brightness, 1.0, 0.0, 3.0);

            for (int y = 0; y < TextureSize; y++)
            {
                double ny =
                    (y / (double)(TextureSize - 1)) * 2.0 - 1.0;

                for (int x = 0; x < TextureSize; x++)
                {
                    double nx =
                        (x / (double)(TextureSize - 1)) * 2.0 - 1.0;

                    double warpX =
                        (ValueNoise(nx * 2.4 + 19.0,
                            ny * 2.4 - 7.0, seed + 17) - 0.5) * 0.26;

                    double warpY =
                        (ValueNoise(nx * 2.4 - 11.0,
                            ny * 2.4 + 5.0, seed + 31) - 0.5) * 0.26;

                    double noise =
                        0.52 * FractalNoise(
                            (nx + warpX) * 3.1,
                            (ny + warpY) * 3.1,
                            seed)
                        + 0.32 * FractalNoise(
                            (nx + warpX) * 7.0,
                            (ny + warpY) * 7.0,
                            seed + 101)
                        + 0.16 * ValueNoise(
                            (nx + warpX) * 15.0,
                            (ny + warpY) * 15.0,
                            seed + 211);

                    double distance =
                        Math.Sqrt(nx * nx * 0.82 + ny * ny * 1.15);

                    double envelope =
                        1.0 - SmoothStep(0.64, 1.24, distance);

                    double cloud =
                        SmoothStep(
                            threshold - 0.12,
                            threshold + 0.14,
                            noise);

                    double wisps =
                        0.72 + 0.28 *
                        ValueNoise(nx * 11.0, ny * 11.0, seed + 509);

                    double edgeDistance = Math.Sqrt(nx * nx + ny * ny);

                    double edgeFade =
                        1.0 - SmoothStep(0.60, 1.0, edgeDistance);

                    double alpha = Math.Clamp(
                        envelope * cloud * wisps * edgeFade * 0.88,
                        0.0,
                        0.92);

                    double light = Math.Clamp(
                        (0.42 + noise * 1.22) * intensity,
                        0.0,
                        2.5);

                    light *=
                        0.82 + 0.18 *
                        (1.0 - Math.Clamp(distance, 0.0, 1.0));

                    int index = y * stride + x * 4;

                    pixels[index] = ToByte(tint.B * light);
                    pixels[index + 1] = ToByte(tint.G * light);
                    pixels[index + 2] = ToByte(tint.R * light);
                    pixels[index + 3] = ToByte(alpha * 255.0);
                }
            }

            WriteableBitmap bitmap = new(
                TextureSize,
                TextureSize,
                96,
                96,
                PixelFormats.Bgra32,
                null);

            bitmap.WritePixels(
                new Int32Rect(0, 0, TextureSize, TextureSize),
                pixels,
                stride,
                0);

            bitmap.Freeze();

            return bitmap;
        }

        private static MeshGeometry3D CreatePlaneMesh(
            double width,
            double height)
        {
            double halfWidth = width / 2.0;
            double halfHeight = height / 2.0;

            MeshGeometry3D mesh = new();

            mesh.Positions.Add(new Point3D(-halfWidth, -halfHeight, 0));
            mesh.Positions.Add(new Point3D(halfWidth, -halfHeight, 0));
            mesh.Positions.Add(new Point3D(halfWidth, halfHeight, 0));
            mesh.Positions.Add(new Point3D(-halfWidth, halfHeight, 0));

            mesh.TextureCoordinates.Add(new Point(0, 1));
            mesh.TextureCoordinates.Add(new Point(1, 1));
            mesh.TextureCoordinates.Add(new Point(1, 0));
            mesh.TextureCoordinates.Add(new Point(0, 0));

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(3);

            for (int i = 0; i < 4; i++)
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
            int x0 = (int)Math.Floor(x);
            int y0 = (int)Math.Floor(y);

            double tx = x - x0;
            double ty = y - y0;

            tx = tx * tx * (3.0 - 2.0 * tx);
            ty = ty * ty * (3.0 - 2.0 * ty);

            double a = Hash01(x0, y0, seed);
            double b = Hash01(x0 + 1, y0, seed);
            double c = Hash01(x0, y0 + 1, seed);
            double d = Hash01(x0 + 1, y0 + 1, seed);

            return Lerp(
                Lerp(a, b, tx),
                Lerp(c, d, tx),
                ty);
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

        private static Color ScaleColor(Color color, double scale) =>
            Color.FromRgb(
                ToByte(color.R * scale),
                ToByte(color.G * scale),
                ToByte(color.B * scale));

        private static byte ToByte(double value) =>
            (byte)Math.Clamp((int)Math.Round(value), 0, 255);

        private static double Lerp(double a, double b, double t) =>
            a + (b - a) * t;

        private static double SmoothStep(
            double edge0,
            double edge1,
            double x)
        {
            double t = Math.Clamp(
                (x - edge0) / (edge1 - edge0),
                0.0,
                1.0);

            return t * t * (3.0 - 2.0 * t);
        }

        private static double Clamp(
            double value,
            double fallback,
            double minimum,
            double maximum) =>
            double.IsFinite(value)
                ? Math.Clamp(value, minimum, maximum)
                : fallback;

        private static void SetChildEnabled(
            Model3DGroup parent,
            Model3D child,
            bool enabled)
        {
            bool present = parent.Children.Contains(child);

            if (enabled && !present)
                parent.Children.Add(child);
            else if (!enabled && present)
                parent.Children.Remove(child);
        }

        // this class is used to store the state of each cloud layer in the nebula renderer
        private sealed class CloudLayer(
            Model3DGroup group,
            GeometryModel3D model,
            ImageBrush brush,
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
}