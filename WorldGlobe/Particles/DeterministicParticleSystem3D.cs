using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.WorldGlobe.Particles;

internal sealed class DeterministicParticleSystem3D
{
    private static readonly MeshGeometry3D ParticleMesh = CreateParticleMesh();

    private ParticleEmitterSettings _settings;
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

    /// <summary>Updates emission parameters without rebuilding the visual pool.</summary>
    public void UpdateSettings(ParticleEmitterSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.MaxParticles != _visuals.Length)
        {
            throw new ArgumentException(
                "MaxParticles cannot change after the particle system has been created.",
                nameof(settings));
        }

        // Avoid replacing equivalent settings on every common-property update.
        if (_settings.EmissionRate == settings.EmissionRate &&
            _settings.LifetimeSeconds == settings.LifetimeSeconds &&
            _settings.Seed == settings.Seed &&
            _settings.EmissionJitterFraction == settings.EmissionJitterFraction &&
            _settings.LifetimeVariation == settings.LifetimeVariation)
        {
            return;
        }

        _settings = settings;
    }

    /// <summary>
    /// Evaluates particles at an absolute animation timestamp. The result is
    /// independent of the order of prior calls to AdvanceTo.
    /// </summary>
    public void AdvanceTo(double timeSeconds)
    {
        if (!double.IsFinite(timeSeconds) || timeSeconds < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeSeconds),
                "Particle time must be finite and non-negative.");
        }

        if (_settings.EmissionRate <= 0.0)
        {
            HideAll();
            return;
        }

        double interval = 1.0 / _settings.EmissionRate;
        double latestIndexValue = Math.Floor(timeSeconds * _settings.EmissionRate);

        if (latestIndexValue > long.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeSeconds), "The particle emission index is too large.");
        }

        long latestIndex = (long)latestIndexValue;
        double maximumLifetime = _settings.LifetimeSeconds *
            (1.0 + _settings.LifetimeVariation);

        double earliestIndexValue =
            Math.Floor((timeSeconds - maximumLifetime) / interval) - 1.0;

        long earliestIndex = earliestIndexValue <= 0.0
            ? 0
            : (long)Math.Min(earliestIndexValue, long.MaxValue);

        int visualIndex = 0;

        // Prefer the most recently emitted particles when the fixed pool is full.
        for (long index = latestIndex;
             index >= earliestIndex && visualIndex < _visuals.Length;
             index--)
        {
            double emissionTime = index * interval
                + ParticleRandom.Sample(_settings.Seed, index, 0)
                * _settings.EmissionJitterFraction * interval;

            if (emissionTime > timeSeconds)
                continue;

            double lifetimeRandom = ParticleRandom.Sample(_settings.Seed, index, 1);
            double lifetime = _settings.LifetimeSeconds *
                (1.0 + (2.0 * lifetimeRandom - 1.0) * _settings.LifetimeVariation);

            double age = timeSeconds - emissionTime;
            if (age < 0.0 || age >= lifetime)
                continue;

            if (!_behavior.TryEvaluate(
                index, emissionTime, age, lifetime, _settings.Seed,
                out ParticleState state))
            {
                continue;
            }

            if (!_visuals[visualIndex].SetState(state))
                continue;

            visualIndex++;
        }

        for (int i = visualIndex; i < _visuals.Length; i++)
            _visuals[i].Hide();
    }

    public void Reset() => HideAll();

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
            _brush = new SolidColorBrush(Colors.White) { Opacity = 0.0 };

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
            if (!double.IsFinite(state.Position.X) ||
                !double.IsFinite(state.Position.Y) ||
                !double.IsFinite(state.Position.Z) ||
                !double.IsFinite(state.Size) || state.Size <= 0.0 ||
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

            _brush.Color = state.Color;
            _brush.Opacity = opacity;

            Matrix3D matrix = Matrix3D.Identity;
            matrix.M11 = state.Size;
            matrix.M22 = state.Size;
            matrix.M33 = state.Size;
            matrix.OffsetX = state.Position.X;
            matrix.OffsetY = state.Position.Y;
            matrix.OffsetZ = state.Position.Z;

            _transform.Matrix = matrix;
            return true;
        }

        public void Hide() => _brush.Opacity = 0.0;
    }

    private static MeshGeometry3D CreateParticleMesh()
    {
        const int longitudeSegments = 8;
        const int latitudeSegments = 6;

        var mesh = new MeshGeometry3D();
        int stride = longitudeSegments + 1;

        for (int latitude = 0; latitude <= latitudeSegments; latitude++)
        {
            double phi = Math.PI * latitude / latitudeSegments;
            double sinPhi = Math.Sin(phi);
            double cosPhi = Math.Cos(phi);

            for (int longitude = 0; longitude <= longitudeSegments; longitude++)
            {
                double theta = 2.0 * Math.PI * longitude / longitudeSegments;
                double x = sinPhi * Math.Cos(theta);
                double y = cosPhi;
                double z = sinPhi * Math.Sin(theta);

                mesh.Positions.Add(new Point3D(x, y, z));
                mesh.Normals.Add(new Vector3D(x, y, z));
                mesh.TextureCoordinates.Add(new System.Windows.Point(
                    longitude / (double)longitudeSegments,
                    latitude / (double)latitudeSegments));
            }
        }

        for (int latitude = 0; latitude < latitudeSegments; latitude++)
        {
            for (int longitude = 0; longitude < longitudeSegments; longitude++)
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
