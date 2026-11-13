using RealmStudioShapeRenderingLib;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.Views.Celestial
{
    internal abstract class LocalStarCelestialBodyRenderer :
        ICelestialBodyRenderer,
        IAttachmentContainer,
        ICelestialBodyVisualTarget,
        ICelestialAnimatedObject,
        ICameraFacingCelestialObject
    {
        private readonly Model3DGroup _root = new();
        private readonly Model3DGroup _bodyVisualRoot = new();
        public Model3DGroup BodyVisualRoot => _bodyVisualRoot;

        public Color BodyColor => Body.BaseColor;

        private readonly Model3DGroup _attachmentRoot = new();

        private readonly ScaleTransform3D _scaleTransform = new(1.0, 1.0, 1.0);

        private readonly Transform3DGroup _animationTransform = new();

        private PointLight? _light;

        private readonly Dictionary<string, ICelestialBodyAnimationRenderer> _animationRenderers = [];

        private readonly Dictionary<string, IAttachmentRenderer> _attachmentRenderers = [];

        private CelestialBody? _body;

        private CelestialBodyNode? _node;

        private readonly Func<CelestialTextureSource?, ImageSource?> _textureResolver;

        private readonly Model3DGroup _activityVisualRoot = new();

        public Model3DGroup ActivityVisualRoot => _activityVisualRoot;

        private readonly ScaleTransform3D _activityScaleTransform = new(1.0, 1.0, 1.0);

        protected LocalStarCelestialBodyRenderer(
            Func<CelestialTextureSource?, ImageSource?> textureResolver)
        {
            _textureResolver = textureResolver
                ?? throw new ArgumentNullException(nameof(textureResolver));
            // The body visual receives the placement size scaling and
            // its animation transforms.
            Transform3DGroup bodyTransform = new();

            bodyTransform.Children.Add(_scaleTransform);
            bodyTransform.Children.Add(_animationTransform);

            _bodyVisualRoot.Transform = bodyTransform;

            // Solar activity receives the same placement-size scaling as the body,
            // but deliberately does not inherit its animation transforms.
            _activityVisualRoot.Transform = _activityScaleTransform;

            _root.Children.Add(_bodyVisualRoot);
            _root.Children.Add(_activityVisualRoot);

            // Attachments are deliberately kept outside the body visual
            // transform. Each attachment receives the host placement size
            // when its own renderer is updated.
            _root.Children.Add(_attachmentRoot);

            // The light remains at the Local Star's origin and is not
            // affected by the body's visual scaling or rotation. It is added
            // to the scene only while the body's Point Light is enabled.
        }

        public abstract CelestialBodyRendererType Type { get; }

        public Model3DGroup Root => _root;

        public Transform3DGroup AnimationTransform => _animationTransform;

        protected CelestialBody Body =>
            _body ?? throw new InvalidOperationException(
                "The Local Star renderer has not been initialized.");

        protected CelestialBodyNode Node =>
            _node ?? throw new InvalidOperationException(
                "The Local Star renderer has not been initialized.");

        protected double Size => Node.Placement.Size;

        protected Color StarColor => Body.BaseColor;

        // Retained for compatibility with derived renderers. Per-body
        // CelestialPointLightSettings now supplies the effective values.
        protected double LightRange { get; set; } = 10.0;

        public void Update(CelestialBodyNode node, CelestialTextureSource? textureSource)
        {
            ArgumentNullException.ThrowIfNull(node);
            ArgumentNullException.ThrowIfNull(node.Body);
            ArgumentNullException.ThrowIfNull(node.Placement);

            _node = node;
            _body = node.Body;

            ImageSource? texture = _textureResolver(textureSource);

            double size = node.Placement.Size;

            _scaleTransform.ScaleX = size;
            _scaleTransform.ScaleY = size;
            _scaleTransform.ScaleZ = size;

            _activityScaleTransform.ScaleX = size;
            _activityScaleTransform.ScaleY = size;
            _activityScaleTransform.ScaleZ = size;

            UpdateLight();

            // The derived renderer owns all geometry and materials
            // associated with the particular Local Star type.
            UpdateBodyVisuals(node.Body, texture);

            RefreshAnimationRenderers();
        }

        /// <summary>
        /// Updates the geometry and materials specific to this Local Star.
        /// The supplied texture is the texture resolved from the body's
        /// TextureSource.
        /// </summary>
        protected abstract void UpdateBodyVisuals(
            CelestialBody body,
            ImageSource? texture);

        private void UpdateLight()
        {
            CelestialPointLightSettings settings =
                Body.PointLightSettings ?? new CelestialPointLightSettings();

            if (!settings.Enabled)
            {
                if (_light is not null)
                {
                    _root.Children.Remove(_light);
                    _light = null;
                }

                return;
            }

            // Do not even instantiate the WPF light until it is enabled.
            _light ??= new PointLight();
            if (!_root.Children.Contains(_light))
                _root.Children.Add(_light);

            double intensity = Math.Clamp(Body.LightIntensity, 0.0, 1.0);
            double range = Math.Max(
                0.001,
                double.IsFinite(settings.Range) ? settings.Range : 10.0);

            byte red = (byte)Math.Clamp(StarColor.R * intensity, 0.0, 255.0);
            byte green = (byte)Math.Clamp(StarColor.G * intensity, 0.0, 255.0);
            byte blue = (byte)Math.Clamp(StarColor.B * intensity, 0.0, 255.0);

            _light.Color = Color.FromRgb(red, green, blue);
            _light.Position = new Point3D(0, 0, 0);

            _light.ConstantAttenuation = 0.0;
            _light.LinearAttenuation = 0.0;
            _light.QuadraticAttenuation = 1.0 / (range * range);
        }

        public void UpdateCamera(Camera? camera)
        {
            foreach (ICameraFacingCelestialObject renderer
                     in _animationRenderers.Values.OfType<ICameraFacingCelestialObject>())
            {
                renderer.UpdateCamera(camera);
            }
        }

        #region Attachments

        public void RefreshAttachments(
            Func<CelestialBodyAttachment, BitmapSource?> textureResolver)
        {
            ArgumentNullException.ThrowIfNull(textureResolver);

            if (_body is null)
                return;

            HashSet<string> currentAttachmentIds = [];

            foreach (CelestialBodyAttachment attachment in _body.Attachments)
            {
                currentAttachmentIds.Add(attachment.Id);

                if (!_attachmentRenderers.TryGetValue(
                        attachment.Id,
                        out IAttachmentRenderer? renderer))
                {
                    renderer =
                        PlanetCelestialBodyRenderer.AttachmentRendererFactory
                            .Create(attachment);

                    _attachmentRenderers.Add(attachment.Id, renderer);
                }
                else if (renderer.AttachmentType != attachment.Type)
                {
                    _attachmentRoot.Children.Remove(renderer.Root);
                    renderer.ResetAnimation();

                    renderer =
                        PlanetCelestialBodyRenderer.AttachmentRendererFactory
                            .Create(attachment);

                    _attachmentRenderers[attachment.Id] = renderer;
                }

                BitmapSource? texture = textureResolver(attachment);

                renderer.Update(attachment, texture, Size);

                bool isInScene =
                    _attachmentRoot.Children.Contains(renderer.Root);

                if (renderer.IsEnabled && !isInScene)
                {
                    _attachmentRoot.Children.Add(renderer.Root);
                }
                else if (!renderer.IsEnabled && isInScene)
                {
                    _attachmentRoot.Children.Remove(renderer.Root);
                }
            }

            // Remove renderers for attachments that no longer exist.
            string[] removedAttachmentIds = _attachmentRenderers.Keys
                .Where(id => !currentAttachmentIds.Contains(id))
                .ToArray();

            foreach (string attachmentId in removedAttachmentIds)
            {
                IAttachmentRenderer renderer =
                    _attachmentRenderers[attachmentId];

                _attachmentRoot.Children.Remove(renderer.Root);

                renderer.ResetAnimation();

                _attachmentRenderers.Remove(attachmentId);
            }
        }

        #endregion

        #region Body animations

        private void RefreshAnimationRenderers()
        {
            if (_body is null)
                return;

            HashSet<string> currentAnimationIds = [];

            foreach (CelestialBodyAnimation animation in _body.Animations)
            {
                currentAnimationIds.Add(animation.Id);

                if (!_animationRenderers.TryGetValue(
                        animation.Id,
                        out ICelestialBodyAnimationRenderer? renderer))
                {
                    renderer = AnimationRendererFactory.Create(animation);

                    _animationRenderers.Add(animation.Id, renderer);
                }
                else if (renderer.AnimationType != animation.AnimationType)
                {
                    renderer.ResetAnimation();

                    renderer = AnimationRendererFactory.Create(animation);

                    _animationRenderers[animation.Id] = renderer;
                }

                renderer.Update(animation, this);
            }

            // Remove renderers for animations that no longer exist.
            string[] removedAnimationIds = _animationRenderers.Keys
                .Where(id => !currentAnimationIds.Contains(id))
                .ToArray();

            foreach (string animationId in removedAnimationIds)
            {
                _animationRenderers[animationId].ResetAnimation();
                _animationRenderers.Remove(animationId);
            }
        }

        #endregion

        #region Animation

        public virtual void AdvanceAnimation(CelestialAnimationFrame frame)
        {
            if (_body is null || frame.DeltaSeconds <= 0.0)
                return;

            foreach (ICelestialBodyAnimationRenderer renderer
                     in _animationRenderers.Values)
            {
                renderer.AdvanceAnimation(frame);
            }

            foreach (IAttachmentRenderer renderer in _attachmentRenderers.Values)
            {
                renderer.AdvanceAnimation(frame);
            }
        }

        public virtual void ResetAnimation()
        {
            foreach (ICelestialBodyAnimationRenderer renderer
                     in _animationRenderers.Values)
            {
                renderer.ResetAnimation();
            }

            foreach (IAttachmentRenderer renderer in _attachmentRenderers.Values)
            {
                renderer.ResetAnimation();
            }
        }

        #endregion
    }
}