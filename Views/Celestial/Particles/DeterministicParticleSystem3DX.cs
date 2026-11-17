
using HelixToolkit.Maths;
using HelixToolkit.Wpf.SharpDX;
using System.Windows;
using System.Windows.Media.Media3D;
using DiffuseMaterial = HelixToolkit.Wpf.SharpDX.DiffuseMaterial;
using MeshGeometry3D = HelixToolkit.SharpDX.MeshGeometry3D;

namespace RealmStudioX.WPF.Views.Celestial.Particles;

internal sealed class DeterministicParticleSystem3DX
{
    private ParticleEmitterSettings _settings;
    private readonly IDeterministicParticleBehavior _behavior;
    private readonly ParticleVisual[] _visuals;

    public GroupModel3D Root { get; } = new();

    public DeterministicParticleSystem3DX(
        MeshGeometry3D particleMesh,
        ParticleEmitterSettings settings,
        IDeterministicParticleBehavior behavior)
    {
        ArgumentNullException.ThrowIfNull(particleMesh);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(behavior);

        _settings = settings;
        _behavior = behavior;

        _visuals = new ParticleVisual[settings.MaxParticles];

        for (int i = 0; i < _visuals.Length; i++)
        {
            ParticleVisual visual = new(particleMesh);
            _visuals[i] = visual;
            Root.Children.Add(visual.Model);
        }

        Root.Visibility = Visibility.Collapsed;
    }

    public void UpdateSettings(ParticleEmitterSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.MaxParticles != _visuals.Length)
        {
            throw new ArgumentException(
                "The particle pool size cannot be changed after initialization.",
                nameof(settings));
        }

        _settings = settings;
    }

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
            Math.Floor((timeSeconds - maximumLifetime) / interval) - 1.0;

        long earliestIndex = earliestIndexValue <= 0.0
            ? 0
            : (long)Math.Min(earliestIndexValue, long.MaxValue);

        int visualIndex = 0;

        // Prefer the most recently emitted particles when the pool is full.
        for (long index = latestIndex;
             index >= earliestIndex && visualIndex < _visuals.Length;
             index--)
        {
            double emissionTime =
                index * interval +
                ParticleRandom.Sample(_settings.Seed, index, 0) *
                _settings.EmissionJitterFraction * interval;

            if (emissionTime > timeSeconds)
                continue;

            double lifetimeRandom =
                ParticleRandom.Sample(_settings.Seed, index, 1);

            double lifetime =
                _settings.LifetimeSeconds *
                (1.0 +
                 (2.0 * lifetimeRandom - 1.0) *
                 _settings.LifetimeVariation);

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
        private readonly DiffuseMaterial _material;
        private readonly ScaleTransform3D _scale = new(1, 1, 1);
        private readonly TranslateTransform3D _translation = new();

        public MeshGeometryModel3D Model { get; }

        public ParticleVisual(MeshGeometry3D geometry)
        {
            Transform3DGroup transform = new();
            transform.Children.Add(_scale);
            transform.Children.Add(_translation);

            _material = new DiffuseMaterial
            {
                DiffuseColor = new Color4(1, 1, 1, 0),
                EnableUnLit = true
            };

            Model = new MeshGeometryModel3D
            {
                Geometry = geometry,
                Material = _material,
                IsTransparent = true,
                CullMode = SharpDX.Direct3D11.CullMode.None,
                Transform = transform,
                Visibility = Visibility.Collapsed
            };
        }

        public bool SetState(ParticleState state)
        {
            if (!double.IsFinite(state.Position.X) ||
                !double.IsFinite(state.Position.Y) ||
                !double.IsFinite(state.Position.Z) ||
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

            _material.DiffuseColor = new Color4(
                state.Color.R / 255.0f,
                state.Color.G / 255.0f,
                state.Color.B / 255.0f,
                (float)opacity);

            _scale.ScaleX = state.Size;
            _scale.ScaleY = state.Size;
            _scale.ScaleZ = state.Size;

            _translation.OffsetX = state.Position.X;
            _translation.OffsetY = state.Position.Y;
            _translation.OffsetZ = state.Position.Z;

            Model.Visibility = Visibility.Visible;
            return true;
        }

        public void Hide()
        {
            Model.Visibility = Visibility.Collapsed;
        }
    }
}
