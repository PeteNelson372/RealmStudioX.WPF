using System.Windows.Media;
using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.WorldGlobe.Particles;

internal sealed class DeterministicParticleSystem3D
{
    private static readonly MeshGeometry3D ParticleMesh =
        CreateParticleMesh();

    private readonly ParticleEmitterSettings _settings;
    private readonly IDeterministicParticleBehavior _behavior;
    private readonly ParticleVisual[] _visuals;

    public Model3DGroup Root { get; } = new();

    public DeterministicParticleSystem3D(
        ParticleEmitterSettings settings,
        IDeterministicParticleBehavior behavior)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(behavior);

        _settings = settings;
        _behavior = behavior;

        _visuals = new ParticleVisual[settings.MaxParticles];

        for (int i = 0; i < _visuals.Length; i++)
        {
            _visuals[i] = new ParticleVisual(ParticleMesh);
            Root.Children.Add(_visuals[i].Model);
        }
    }

    /// <summary>
    /// Evaluates the complete particle system at an absolute animation
    /// time. The result does not depend on the order of previous calls.
    /// </summary>
    public void AdvanceTo(double timeSeconds)
    {
        if (!double.IsFinite(timeSeconds) || timeSeconds < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeSeconds));
        }

        if (_settings.EmissionRate <= 0.0)
        {
            HideAll();
            return;
        }

        double interval = 1.0 / _settings.EmissionRate;
        double latestIndexValue =
            Math.Floor(timeSeconds * _settings.EmissionRate);

        if (latestIndexValue > long.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeSeconds),
                "The particle emission index is too large.");
        }

        long latestIndex = (long)latestIndexValue;

        double maximumLifetime =
            _settings.LifetimeSeconds *
            (1.0 + _settings.LifetimeVariation);

        double earliestIndexValue =
            Math.Floor(
                (timeSeconds - maximumLifetime) / interval) - 1.0;

        long earliestIndex = earliestIndexValue <= 0.0
            ? 0
            : (long)Math.Min(earliestIndexValue, long.MaxValue);

        int visualIndex = 0;

        // Search newest particles first. If the configured pool is
        // exhausted, the most recently emitted living particles win.
        for (long index = latestIndex;
             index >= earliestIndex && visualIndex < _visuals.Length;
             index--)
        {
            double emissionTime =
                index * interval
                + ParticleRandom.Sample(_settings.Seed, index, 0)
                * _settings.EmissionJitterFraction
                * interval;

            // Positive emission jitter can put this emission in
            // the future even though its nominal interval has begun.
            if (emissionTime > timeSeconds)
                continue;

            double lifetimeRandom = ParticleRandom.Sample(
                _settings.Seed, index, 1);

            double lifetime = _settings.LifetimeSeconds *
                (1.0 + (2.0 * lifetimeRandom - 1.0)
                    * _settings.LifetimeVariation);

            double age = timeSeconds - emissionTime;

            if (age < 0.0 || age >= lifetime)
                continue;

            if (!_behavior.TryEvaluate(
                    index,
                    emissionTime,
                    age,
                    lifetime,
                    _settings.Seed,
                    out ParticleState state))
            {
                continue;
            }

            if (!_visuals[visualIndex].SetState(state))
                continue;

            visualIndex++;
        }

        // Return the remaining slots to the invisible pool.
        for (int i = visualIndex; i < _visuals.Length; i++)
            _visuals[i].Hide();
    }

    public void Reset()
    {
        HideAll();
    }

    private void HideAll()
    {
        foreach (ParticleVisual visual in _visuals)
            visual.Hide();
    }

    private sealed class ParticleVisual
    {
        private readonly SolidColorBrush _brush;
        private readonly MatrixTransform3D _transform;

        public GeometryModel3D Model { get; }

        public ParticleVisual(MeshGeometry3D geometry)
        {
            _brush = new SolidColorBrush(Colors.White)
            {
                Opacity = 0.0
            };

            var material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(_brush));
            material.Children.Add(new EmissiveMaterial(_brush));

            _transform = new MatrixTransform3D();

            Model = new GeometryModel3D
            {
                Geometry = geometry,
                Material = material,
                BackMaterial = material,
                Transform = _transform
            };
        }

        public bool SetState(ParticleState state)
        {
            if (!IsFinite(state.Position.X) ||
                !IsFinite(state.Position.Y) ||
                !IsFinite(state.Position.Z) ||
                !double.IsFinite(state.Size) ||
                state.Size <= 0.0 ||
                !double.IsFinite(state.Opacity))
            {
                Hide();
                return false;
            }

            double opacity = Math.Clamp(state.Opacity, 0.0, 1.0);

            if (opacity <= 0.001)
            {
                Hide();
                return false;
            }

            _brush.Color = Color.FromRgb(
                state.Color.R,
                state.Color.G,
                state.Color.B);

            _brush.Opacity = opacity;

            double size = state.Size;

            Matrix3D matrix = Matrix3D.Identity;

            matrix.M11 = size;
            matrix.M22 = size;
            matrix.M33 = size;

            matrix.OffsetX = state.Position.X;
            matrix.OffsetY = state.Position.Y;
            matrix.OffsetZ = state.Position.Z;

            _transform.Matrix = matrix;

            return true;
        }

        public void Hide()
        {
            _brush.Opacity = 0.0;
        }

        private static bool IsFinite(double value) =>
            double.IsFinite(value);
    }

    private static MeshGeometry3D CreateParticleMesh()
    {
        const int longitudeSegments = 8;
        const int latitudeSegments = 6;

        var mesh = new MeshGeometry3D();

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

                mesh.TextureCoordinates.Add(new System.Windows.Point(longitude / (double)longitudeSegments, latitude / (double)latitudeSegments));
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
                int a = latitude * stride + longitude;
                int b = a + stride;
                int c = a + 1;
                int d = b + 1;

                mesh.TriangleIndices.Add(a);
                mesh.TriangleIndices.Add(b);
                mesh.TriangleIndices.Add(c);

                mesh.TriangleIndices.Add(c);
                mesh.TriangleIndices.Add(b);
                mesh.TriangleIndices.Add(d);
            }
        }

        if (mesh.CanFreeze)
            mesh.Freeze();

        return mesh;
    }
}