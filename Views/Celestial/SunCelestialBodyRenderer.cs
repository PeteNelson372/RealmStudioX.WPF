using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.EditorUtilities;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Celestial
{
    internal sealed class SunCelestialBodyRenderer : LocalStarCelestialBodyRenderer
    {
        private static readonly Lazy<BitmapSource> SunTexture =
            new(() => WorldGlobeAssets.LoadBitmap("sol-texture.jpg"));

        private static readonly MeshGeometry3D SphereMesh = CreateSphereMesh();

        private readonly GeometryModel3D _sunSurface = new();
        private readonly GeometryModel3D _corona = new();

        private readonly EmissiveMaterial _sunMaterial;
        private readonly EmissiveMaterial _coronaMaterial;

        private readonly ImageBrush _coronaBrush;

        private readonly Vector3D _coronaRotationAxis =
            CreateRandomRotationAxis();

        private readonly Vector3D _coronaWobbleAxisA =
            CreateRandomRotationAxis();

        private readonly Vector3D _coronaWobbleAxisB =
            CreateRandomRotationAxis();

        private readonly double _coronaRotationDirection =
            Random.Shared.Next(2) == 0 ? -1.0 : 1.0;

        private readonly double _coronaWobbleAmplitudeA =
            3.0 + Random.Shared.NextDouble() * 4.0;

        private readonly double _coronaWobbleAmplitudeB =
            2.0 + Random.Shared.NextDouble() * 3.0;

        private readonly double _coronaWobbleFrequencyA =
            0.35 + Random.Shared.NextDouble() * 0.45;

        private readonly double _coronaWobbleFrequencyB =
            0.65 + Random.Shared.NextDouble() * 0.65;

        private readonly double _coronaWobblePhaseA =
            Random.Shared.NextDouble() * Math.PI * 2.0;

        private readonly double _coronaWobblePhaseB =
            Random.Shared.NextDouble() * Math.PI * 2.0;

        private double _coronaElapsedTime;
        private double _coronaAnimationAngle;
        private double _coronaRotationRate = 12.0;

        private const double CoronaRadiusScale = 1.02;

        private bool _enableCorona = true;

        private Quaternion _baseCoronaRotation = Quaternion.Identity;

        public override CelestialBodyRendererType Type => CelestialBodyRendererType.Sun;

        public SunCelestialBodyRenderer(
            Func<CelestialTextureSource?, ImageSource?> textureResolver)
            : base(textureResolver)
        {
            ImageBrush sunBrush = new(SunTexture.Value)
            {
                Stretch = Stretch.Fill,
                Opacity = 1.0
            };

            _sunMaterial = new EmissiveMaterial(sunBrush);

            _coronaBrush = new ImageBrush(SunTexture.Value)
            {
                Stretch = Stretch.Fill,
                Opacity = 0.18
            };

            _coronaMaterial = new EmissiveMaterial(_coronaBrush);

            _corona.Geometry = SphereMesh;
            _corona.Material = _coronaMaterial;

            Material backMaterial = new DiffuseMaterial(_coronaBrush);

            _sunSurface.Geometry = SphereMesh;
            _sunSurface.Material = _sunMaterial;
            _sunSurface.BackMaterial = backMaterial;

            _corona.Geometry = SphereMesh;
            _corona.Material = _coronaMaterial;
            _corona.BackMaterial = _coronaMaterial;

            // The corona is slightly larger than the Sun's surface.
            // Its independent rotation creates the roiling appearance.
            BodyVisualRoot.Children.Add(_corona);
            BodyVisualRoot.Children.Add(_sunSurface);
        }

        protected override void UpdateBodyVisuals(CelestialBody body, ImageSource? texture)
        {
            SunAnimationSettings settings = GetSunSettings(body);

            ImageSource sunTexture = SunTexture.Value;

            ImageBrush sunBrush = new(sunTexture)
            {
                Stretch = Stretch.Fill,
                Opacity = 1.0
            };

            _sunMaterial.Brush = sunBrush;
            _sunMaterial.Color = body.BaseColor;

            _coronaMaterial.Color = body.BaseColor;
            _coronaBrush.ImageSource = sunTexture;

            _enableCorona = settings.EnableCorona;

            _coronaBrush.Opacity = _enableCorona
                ? Clamp(settings.CoronaOpacity, 0.18, 0.05, 0.30)
                : 0.0;

            _coronaRotationRate = Clamp(
                settings.CoronaRotationSpeed,
                12.0,
                0.0,
                25.0);

            _corona.Transform = CreateStarSurfaceTransform(
                CoronaRadiusScale,
                _baseCoronaRotation);
        }

        private static SunAnimationSettings GetSunSettings(
            CelestialBody body)
        {
            CelestialBodyAnimation? animation =
                body.Animations.FirstOrDefault(
                    item => item.AnimationType ==
                            CelestialBodyAnimationType.SolarActivity);

            return animation?.Settings as SunAnimationSettings
                ?? new SunAnimationSettings();
        }

        public override void AdvanceAnimation(CelestialAnimationFrame frame)
        {
            base.AdvanceAnimation(frame);

            if (frame.DeltaSeconds <= 0.0 || !_enableCorona)
                return;

            _coronaElapsedTime += frame.DeltaSeconds;

            _coronaAnimationAngle +=
                _coronaRotationDirection *
                _coronaRotationRate *
                frame.DeltaSeconds;

            _coronaAnimationAngle %= 360.0;

            if (_coronaAnimationAngle < 0.0)
                _coronaAnimationAngle += 360.0;

            ApplyCoronaTransform();
        }

        public override void ResetAnimation()
        {
            base.ResetAnimation();

            _coronaElapsedTime = 0.0;
            _coronaAnimationAngle = 0.0;

            ApplyCoronaTransform();
        }

        private void ApplyCoronaTransform()
        {
            Quaternion primaryRotation = new(
                _coronaRotationAxis,
                _coronaAnimationAngle);

            double wobbleAngleA =
                Math.Sin(
                    _coronaElapsedTime *
                    _coronaWobbleFrequencyA *
                    Math.PI * 2.0 +
                    _coronaWobblePhaseA) *
                _coronaWobbleAmplitudeA;

            double wobbleAngleB =
                Math.Sin(
                    _coronaElapsedTime *
                    _coronaWobbleFrequencyB *
                    Math.PI * 2.0 +
                    _coronaWobblePhaseB) *
                _coronaWobbleAmplitudeB;

            Quaternion wobbleRotationA = new(
                _coronaWobbleAxisA,
                wobbleAngleA);

            Quaternion wobbleRotationB = new(
                _coronaWobbleAxisB,
                wobbleAngleB);

            Quaternion coronaRotation = Quaternion.Multiply(
                _baseCoronaRotation,
                Quaternion.Multiply(
                    primaryRotation,
                    Quaternion.Multiply(
                        wobbleRotationA,
                        wobbleRotationB)));

            _corona.Transform = CreateStarSurfaceTransform(
                CoronaRadiusScale,
                coronaRotation);
        }

        private static Transform3D CreateStarSurfaceTransform(
            double radius,
            Quaternion rotation)
        {
            Transform3DGroup transform = new();

            transform.Children.Add(
                new ScaleTransform3D(radius, radius, radius));

            transform.Children.Add(
                new RotateTransform3D(
                    new QuaternionRotation3D(rotation)));

            return transform;
        }

        private static Vector3D CreateRandomRotationAxis()
        {
            Vector3D axis;

            do
            {
                axis = new Vector3D(
                    Random.Shared.NextDouble() * 2.0 - 1.0,
                    Random.Shared.NextDouble() * 2.0 - 1.0,
                    Random.Shared.NextDouble() * 2.0 - 1.0);
            }
            while (axis.LengthSquared < 0.0001);

            axis.Normalize();

            return axis;
        }

        private static MeshGeometry3D CreateSphereMesh()
        {
            MeshGeometry3D mesh = new();

            const int longitudeSegments = 128;
            const int latitudeSegments = 64;

            Point3DCollection positions = [];
            Vector3DCollection normals = [];
            PointCollection textureCoordinates = [];
            Int32Collection indices = [];

            int rowLength = longitudeSegments + 1;

            for (int latitude = 0; latitude <= latitudeSegments; latitude++)
            {
                double v = (double)latitude / latitudeSegments;
                double phi = Math.PI / 2.0 - Math.PI * v;

                double cosPhi = Math.Cos(phi);
                double y = Math.Sin(phi);

                for (int longitude = 0; longitude <= longitudeSegments; longitude++)
                {
                    double u = (double)longitude / longitudeSegments;
                    double theta = 2.0 * Math.PI * u;

                    double x = cosPhi * Math.Cos(theta);
                    double z = cosPhi * Math.Sin(theta);

                    positions.Add(new Point3D(x, y, z));
                    normals.Add(new Vector3D(x, y, z));
                    textureCoordinates.Add(new Point(u, v));
                }
            }

            for (int latitude = 0; latitude < latitudeSegments; latitude++)
            {
                for (int longitude = 0; longitude < longitudeSegments; longitude++)
                {
                    int a = latitude * rowLength + longitude;
                    int b = a + 1;
                    int c = a + rowLength;
                    int d = c + 1;

                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(c);

                    indices.Add(b);
                    indices.Add(d);
                    indices.Add(c);
                }
            }

            mesh.Positions = positions;
            mesh.Normals = normals;
            mesh.TextureCoordinates = textureCoordinates;
            mesh.TriangleIndices = indices;

            if (mesh.CanFreeze)
                mesh.Freeze();

            return mesh;
        }

        private static double Clamp(
            double value,
            double fallback,
            double minimum,
            double maximum) =>
            double.IsFinite(value)
                ? Math.Clamp(value, minimum, maximum)
                : fallback;
    }
}