using RealmStudioShapeRenderingLib;
using System.Windows.Media.Imaging;

namespace RealmStudioX.WPF.Views.Celestial
{
    internal sealed class RingTurbulenceAnimationRenderer
        : ICelestialBodyAnimationRenderer
    {
        private IRingTurbulenceAnimationTarget? _target;
        private PlanetaryRingTurbulenceEffect3D? _effect;

        private BitmapSource? _sourceTexture;

        private bool _enabled;

        public CelestialBodyAnimationType AnimationType =>
            CelestialBodyAnimationType.RingTurbulenceActivity;

        public void Update(
            CelestialBodyAnimation animation,
            ICelestialBodyAnimationTarget target)
        {
            ArgumentNullException.ThrowIfNull(animation);
            ArgumentNullException.ThrowIfNull(target);

            if (target is not IRingTurbulenceAnimationTarget ringTarget)
            {
                throw new ArgumentException(
                    "Ring turbulence requires an IRingTurbulenceAnimationTarget.",
                    nameof(target));
            }

            _target = ringTarget;

            RingTurbulenceSettings settings =
                animation.Settings as RingTurbulenceSettings
                ?? throw new InvalidOperationException(
                    "RingTurbulenceActivity requires RingTurbulenceSettings.");

            _enabled = animation.Enabled && settings.Intensity > 0.0;

            if (_effect is null)
            {
                _sourceTexture = ringTarget.SourceTexture;

                if (_sourceTexture is null)
                {
                    throw new InvalidOperationException(
                        "The ring texture has not been initialized.");
                }

                _effect = new PlanetaryRingTurbulenceEffect3D(
                    _sourceTexture,
                    _enabled,
                    settings.Intensity,
                    settings.UpdateRate);
            }
            else
            {
                if (!ReferenceEquals(_sourceTexture, ringTarget.SourceTexture))
                {
                    _sourceTexture = ringTarget.SourceTexture;

                    if (_sourceTexture is null)
                    {
                        throw new InvalidOperationException(
                            "The ring texture has not been initialized.");
                    }

                    _effect.SetSourceTexture(_sourceTexture);
                }

                _effect.Update(
                    _enabled,
                    settings.Intensity,
                    settings.UpdateRate);
            }

            _target.SetAnimatedTexture(
                _enabled ? _effect.Texture : null);
        }

        public void AdvanceAnimation(CelestialAnimationFrame frame)
        {
            if (!_enabled || _effect is null)
                return;

            _effect.AdvanceAnimation(frame);
        }

        public void ResetAnimation()
        {
            _effect?.ResetAnimation();

            _target?.SetAnimatedTexture(
                _enabled ? _effect?.Texture : null);
        }
    }
}