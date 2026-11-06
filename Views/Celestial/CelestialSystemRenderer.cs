
using RealmStudioShapeRenderingLib;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.Views.Celestial;

internal sealed class CelestialSystemRenderer : ICelestialAnimatedObject
{
    private readonly Func<CelestialBody, ImageSource?> _textureResolver;

    private readonly Func<CelestialBodyAttachment, BitmapSource?>
        _attachmentTextureResolver;

    private readonly List<ICelestialAnimatedObject> _animatedRenderers = [];

    private readonly List<IAttachmentContainer> _attachmentContainers = [];

    private readonly List<ICameraFacingCelestialObject> _cameraFacingRenderers = [];

    private readonly Dictionary<string, BodyRendererEntry> _bodyRenderers = [];

    private sealed class BodyRendererEntry(
        Model3DGroup nodeRoot,
        TranslateTransform3D centerTransform,
        Model3DGroup visualRoot,
        ICelestialBodyRenderer renderer)
    {
        public Model3DGroup NodeRoot { get; } = nodeRoot;

        public TranslateTransform3D CenterTransform { get; } = centerTransform;

        public Model3DGroup VisualRoot { get; } = visualRoot;

        public ICelestialBodyRenderer Renderer { get; set; } = renderer;
    }

    public CelestialSystemRenderer(
        Func<CelestialBody, ImageSource?> textureResolver,
        Func<CelestialBodyAttachment, BitmapSource?>? attachmentTextureResolver = null)
    {
        _textureResolver = textureResolver
            ?? throw new ArgumentNullException(nameof(textureResolver));

        _attachmentTextureResolver =
            attachmentTextureResolver ?? (_ => null);
    }

    #region Scene Construction

    public Model3DGroup BuildScene(CelestialSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);

        ClearRendererRegistrations();

        Model3DGroup root = new();

        foreach (CelestialBodyNode bodyNode in system.Bodies)
        {
            root.Children.Add(BuildBodyNode(bodyNode));
        }

        return root;
    }

    private void ClearRendererRegistrations()
    {
        foreach (ICelestialAnimatedObject renderer in _animatedRenderers)
        {
            renderer.ResetAnimation();
        }

        _animatedRenderers.Clear();
        _attachmentContainers.Clear();
        _cameraFacingRenderers.Clear();
        _bodyRenderers.Clear();
    }

    private Model3DGroup BuildBodyNode(CelestialBodyNode node)
    {
        CelestialBody body = node.Body;

        ICelestialBodyRenderer renderer = CreateBodyRenderer(body);

        renderer.Update(body, _textureResolver(body));

        Model3DGroup nodeRoot = new();

        TranslateTransform3D centerTransform = new(
            node.Center.X,
            node.Center.Y,
            node.Center.Z);

        Transform3DGroup nodeTransform = new();

        if (node.LocalTransform is not null)
        {
            nodeTransform.Children.Add(node.LocalTransform);
        }

        nodeTransform.Children.Add(centerTransform);

        nodeRoot.Transform = nodeTransform;

        // Keep the body's own visuals in a separate group so a visual
        // transform (such as the World Globe's manual rotation) does not
        // affect child celestial bodies in the node hierarchy.
        Model3DGroup visualRoot = new();

        visualRoot.Children.Add(renderer.Root);

        nodeRoot.Children.Add(visualRoot);

        BodyRendererEntry entry = new(
            nodeRoot,
            centerTransform,
            visualRoot,
            renderer);

        _bodyRenderers.Add(body.Id, entry);

        RegisterRenderer(renderer);

        foreach (CelestialBodyNode child in node.Children)
        {
            nodeRoot.Children.Add(BuildBodyNode(child));
        }

        return nodeRoot;
    }

    #endregion

    #region Body Visual Management

    public void SetBodyVisualTransform(
        CelestialBody body,
        Transform3D transform)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(transform);

        if (!_bodyRenderers.TryGetValue(
                body.Id,
                out BodyRendererEntry? entry))
        {
            throw new InvalidOperationException(
                $"No renderer exists for celestial body '{body.Id}'.");
        }

        entry.VisualRoot.Transform = transform;
    }

    public void AddBodyVisual(
        CelestialBody body,
        Model3D visual)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(visual);

        if (!_bodyRenderers.TryGetValue(
                body.Id,
                out BodyRendererEntry? entry))
        {
            throw new InvalidOperationException(
                $"No renderer exists for celestial body '{body.Id}'.");
        }

        if (!entry.VisualRoot.Children.Contains(visual))
        {
            entry.VisualRoot.Children.Add(visual);
        }
    }

    public void RemoveBodyVisual(
        CelestialBody body,
        Model3D visual)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(visual);

        if (!_bodyRenderers.TryGetValue(
                body.Id,
                out BodyRendererEntry? entry))
        {
            return;
        }

        entry.VisualRoot.Children.Remove(visual);
    }

    #endregion

    #region Renderer Registration

    private void RegisterRenderer(ICelestialBodyRenderer renderer)
    {
        if (renderer is ICelestialAnimatedObject animatedRenderer)
        {
            _animatedRenderers.Add(animatedRenderer);
        }

        if (renderer is ICameraFacingCelestialObject cameraFacing)
        {
            _cameraFacingRenderers.Add(cameraFacing);
        }

        if (renderer is IAttachmentContainer attachmentContainer)
        {
            _attachmentContainers.Add(attachmentContainer);

            attachmentContainer.RefreshAttachments(
                _attachmentTextureResolver);
        }
    }

    private void UnregisterRenderer(ICelestialBodyRenderer renderer)
    {
        if (renderer is ICelestialAnimatedObject animatedRenderer)
        {
            animatedRenderer.ResetAnimation();

            _animatedRenderers.Remove(animatedRenderer);
        }

        if (renderer is ICameraFacingCelestialObject cameraFacing)
        {
            _cameraFacingRenderers.Remove(cameraFacing);
        }

        if (renderer is IAttachmentContainer attachmentContainer)
        {
            _attachmentContainers.Remove(attachmentContainer);
        }
    }

    #endregion

    #region Body Updates

    public void UpdateBody(
        CelestialBody body,
        Point3D center)
    {
        ArgumentNullException.ThrowIfNull(body);

        if (!_bodyRenderers.TryGetValue(
                body.Id,
                out BodyRendererEntry? entry))
        {
            throw new InvalidOperationException(
                $"No renderer exists for celestial body '{body.Id}'.");
        }

        UpdateBodyPosition(body, center);

        if (entry.Renderer.Type != body.RendererType)
        {
            ReplaceBodyRenderer(body, entry);
        }
        else
        {
            entry.Renderer.Update(
                body,
                _textureResolver(body));

            RefreshAttachments(entry.Renderer);
        }
    }

    private void ReplaceBodyRenderer(
        CelestialBody body,
        BodyRendererEntry entry)
    {
        UnregisterRenderer(entry.Renderer);

        ICelestialBodyRenderer replacement =
            CreateBodyRenderer(body);

        replacement.Update(
            body,
            _textureResolver(body));

        entry.VisualRoot.Children.Remove(entry.Renderer.Root);

        entry.VisualRoot.Children.Add(replacement.Root);

        entry.Renderer = replacement;

        RegisterRenderer(replacement);
    }

    private void RefreshAttachments(ICelestialBodyRenderer renderer)
    {
        if (renderer is IAttachmentContainer attachmentContainer)
        {
            attachmentContainer.RefreshAttachments(
                _attachmentTextureResolver);
        }
    }

    public void UpdateBodyPosition(
        CelestialBody body,
        Point3D center)
    {
        ArgumentNullException.ThrowIfNull(body);

        if (!_bodyRenderers.TryGetValue(
                body.Id,
                out BodyRendererEntry? entry))
        {
            throw new InvalidOperationException(
                $"No renderer exists for celestial body '{body.Id}'.");
        }

        entry.CenterTransform.OffsetX = center.X;
        entry.CenterTransform.OffsetY = center.Y;
        entry.CenterTransform.OffsetZ = center.Z;
    }

    #endregion

    #region Attachment Management

    public void RefreshAttachments()
    {
        foreach (IAttachmentContainer attachmentContainer in _attachmentContainers)
        {
            attachmentContainer.RefreshAttachments(
                _attachmentTextureResolver);
        }
    }

    #endregion

    #region Animation

    public void AdvanceAnimation(CelestialAnimationFrame frame)
    {
        foreach (ICelestialAnimatedObject renderer in _animatedRenderers)
        {
            renderer.AdvanceAnimation(frame);
        }
    }

    public void ResetAnimation()
    {
        foreach (ICelestialAnimatedObject renderer in _animatedRenderers)
        {
            renderer.ResetAnimation();
        }
    }

    #endregion

    #region Camera

    public void UpdateCamera(Camera? camera)
    {
        foreach (ICameraFacingCelestialObject renderer in _cameraFacingRenderers)
        {
            renderer.UpdateCamera(camera);
        }
    }

    #endregion

    #region Renderer Factory

    private static ICelestialBodyRenderer CreateBodyRenderer(
        CelestialBody body)
    {
        return body.RendererType switch
        {
            CelestialBodyRendererType.Planet =>
                new PlanetCelestialBodyRenderer(),

            CelestialBodyRendererType.Sun =>
                new SunCelestialBodyRenderer(),

            CelestialBodyRendererType.Nebula =>
                new NebulaCelestialBodyRenderer(),

            CelestialBodyRendererType.Corona =>
                new CoronaCelestialBodyRenderer(),

            CelestialBodyRendererType.BlackHole =>
                new BlackHoleCelestialBodyRenderer(),

            _ => throw new NotSupportedException(
                $"Renderer type '{body.RendererType}' is not supported.")
        };
    }

    #endregion
}
