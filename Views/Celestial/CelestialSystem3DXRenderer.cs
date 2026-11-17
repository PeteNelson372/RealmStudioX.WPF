using HelixToolkit.Wpf.SharpDX;
using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Camera = HelixToolkit.Wpf.SharpDX.Camera;

namespace RealmStudioX.WPF.Views.Celestial;

/// <summary>
/// Creates and manages the HelixToolkit.Wpf.SharpDX scene representation
/// of a <see cref="CelestialSystem"/>.
///
/// This class deliberately mirrors the responsibilities of
/// <see cref="CelestialSystemRenderer"/> without depending on WPF Model3D
/// visuals. Individual 3DX body renderers are supplied through the factory.
/// </summary>
internal sealed class CelestialSystem3DXRenderer : ICelestialAnimatedObject
{
    private readonly Func<CelestialBody, ICelestialBodyRenderer3DX>_bodyRendererFactory;

    private readonly Func<CelestialBodyAttachment, BitmapSource?> _attachmentTextureResolver;

    private readonly List<ICelestialAnimatedObject> _animatedRenderers = [];

    private readonly List<IAttachmentContainer> _attachmentContainers = [];

    private readonly List<ICameraFacingCelestialObject3DX> _cameraFacingRenderers = [];

    private readonly Dictionary<string, BodyRendererEntry> _bodyRenderers = [];

    private sealed class BodyRendererEntry(
        GroupModel3D nodeRoot,
        TranslateTransform3D centerTransform,
        GroupModel3D visualRoot,
        ICelestialBodyRenderer3DX renderer)
    {
        public GroupModel3D NodeRoot { get; } = nodeRoot;

        public TranslateTransform3D CenterTransform { get; } = centerTransform;

        public GroupModel3D VisualRoot { get; } = visualRoot;

        public ICelestialBodyRenderer3DX Renderer { get; set; } = renderer;
    }

    public CelestialSystem3DXRenderer(
        Func<CelestialBody, ICelestialBodyRenderer3DX> bodyRendererFactory,
        Func<CelestialBodyAttachment, BitmapSource?>? attachmentTextureResolver = null)
    {
        _bodyRendererFactory = bodyRendererFactory
            ?? throw new ArgumentNullException(nameof(bodyRendererFactory));

        _attachmentTextureResolver = attachmentTextureResolver ?? (_ => null);
    }

    #region Scene Construction

    /// <summary>
    /// Builds a 3DX scene graph for the supplied celestial system.
    /// The returned root can be added directly to a Viewport3DX Items collection.
    /// </summary>
    public GroupModel3D BuildScene(CelestialSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);

        ClearRendererRegistrations();

        GroupModel3D root = new();

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

    private GroupModel3D BuildBodyNode(CelestialBodyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(node.Body);
        ArgumentNullException.ThrowIfNull(node.Placement);

        CelestialBody body = node.Body;

        ICelestialBodyRenderer3DX renderer = _bodyRendererFactory(body)
            ?? throw new InvalidOperationException(
                $"The 3DX renderer factory returned null for body '{body.Id}'.");

        renderer.Update(node, body.TextureSource);

        GroupModel3D nodeRoot = new();

        TranslateTransform3D centerTransform = new(
            node.Placement.Center.X,
            node.Placement.Center.Y,
            node.Placement.Center.Z);

        Transform3DGroup nodeTransform = new();

        if (node.LocalTransform is not null)
        {
            nodeTransform.Children.Add(node.LocalTransform);
        }

        nodeTransform.Children.Add(centerTransform);
        nodeRoot.Transform = nodeTransform;

        // Keep the body's visuals separate from its child nodes. A visual
        // transform (for example, manual globe rotation) must not rotate
        // child celestial bodies along with the parent's surface.
        GroupModel3D visualRoot = new();
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

    /// <summary>
    /// Applies a transform to a body's visual group without applying it to
    /// that body's child nodes.
    /// </summary>
    public void SetBodyVisualTransform(
        CelestialBody body,
        Transform3D transform)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(transform);

        if (!_bodyRenderers.TryGetValue(body.Id, out BodyRendererEntry? entry))
        {
            throw new InvalidOperationException(
                $"No 3DX renderer exists for celestial body '{body.Id}'.");
        }

        entry.VisualRoot.Transform = transform;
    }

    public void AddBodyVisual(CelestialBody body, Element3D visual)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(visual);

        if (!_bodyRenderers.TryGetValue(body.Id, out BodyRendererEntry? entry))
        {
            throw new InvalidOperationException(
                $"No 3DX renderer exists for celestial body '{body.Id}'.");
        }

        if (!entry.VisualRoot.Children.Contains(visual))
        {
            entry.VisualRoot.Children.Add(visual);
        }
    }

    public void RemoveBodyVisual(CelestialBody body, Element3D visual)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(visual);

        if (!_bodyRenderers.TryGetValue(body.Id, out BodyRendererEntry? entry))
        {
            return;
        }

        entry.VisualRoot.Children.Remove(visual);
    }

    #endregion

    #region Renderer Registration

    private void RegisterRenderer(ICelestialBodyRenderer3DX renderer)
    {
        if (renderer is ICelestialAnimatedObject animatedRenderer)
        {
            _animatedRenderers.Add(animatedRenderer);
        }

        if (renderer is ICameraFacingCelestialObject3DX cameraFacing)
        {
            _cameraFacingRenderers.Add(cameraFacing);
        }

        if (renderer is IAttachmentContainer attachmentContainer)
        {
            _attachmentContainers.Add(attachmentContainer);
            attachmentContainer.RefreshAttachments(_attachmentTextureResolver);
        }
    }

    private void UnregisterRenderer(ICelestialBodyRenderer3DX renderer)
    {
        if (renderer is ICelestialAnimatedObject animatedRenderer)
        {
            animatedRenderer.ResetAnimation();
            _animatedRenderers.Remove(animatedRenderer);
        }

        if (renderer is ICameraFacingCelestialObject3DX cameraFacing)
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

    public void UpdateBody(CelestialBodyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(node.Body);
        ArgumentNullException.ThrowIfNull(node.Placement);

        CelestialBody body = node.Body;

        if (!_bodyRenderers.TryGetValue(body.Id, out BodyRendererEntry? entry))
        {
            throw new InvalidOperationException(
                $"No 3DX renderer exists for celestial body '{body.Id}'.");
        }

        UpdateBodyPosition(node);

        if (entry.Renderer.Type != body.RendererType)
        {
            ReplaceBodyRenderer(node, entry);
        }
        else
        {
            entry.Renderer.Update(node, body.TextureSource);
            RefreshAttachments(entry.Renderer);
        }
    }

    public void UpdateBodyPlacement(CelestialBodyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(node.Body);
        ArgumentNullException.ThrowIfNull(node.Placement);

        if (!_bodyRenderers.TryGetValue(
                node.Body.Id,
                out BodyRendererEntry? entry))
        {
            throw new InvalidOperationException(
                $"No 3DX renderer exists for celestial body '{node.Body.Id}'.");
        }

        UpdateBodyPosition(node);

        entry.Renderer.UpdatePlacement(node);
    }

    private void ReplaceBodyRenderer(
        CelestialBodyNode node,
        BodyRendererEntry entry)
    {
        UnregisterRenderer(entry.Renderer);

        ICelestialBodyRenderer3DX replacement = _bodyRendererFactory(node.Body)
            ?? throw new InvalidOperationException(
                $"The 3DX renderer factory returned null for body '{node.Body.Id}'.");

        replacement.Update(node, node.Body.TextureSource);

        entry.VisualRoot.Children.Remove(entry.Renderer.Root);
        entry.VisualRoot.Children.Add(replacement.Root);

        entry.Renderer = replacement;
        RegisterRenderer(replacement);
    }

    private void RefreshAttachments(ICelestialBodyRenderer3DX renderer)
    {
        if (renderer is IAttachmentContainer attachmentContainer)
        {
            attachmentContainer.RefreshAttachments(_attachmentTextureResolver);
        }
    }

    public void UpdateBodyPosition(CelestialBodyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(node.Body);
        ArgumentNullException.ThrowIfNull(node.Placement);

        if (!_bodyRenderers.TryGetValue(
                node.Body.Id,
                out BodyRendererEntry? entry))
        {
            throw new InvalidOperationException(
                $"No 3DX renderer exists for celestial body '{node.Body.Id}'.");
        }

        Point3D center = node.Placement.Center;

        entry.CenterTransform.OffsetX = center.X;
        entry.CenterTransform.OffsetY = center.Y;
        entry.CenterTransform.OffsetZ = center.Z;
    }

    public void UpdateBodyVisualSettings(CelestialBodyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(node.Body);
        ArgumentNullException.ThrowIfNull(node.Placement);

        CelestialBody body = node.Body;

        if (!_bodyRenderers.TryGetValue(
                body.Id,
                out BodyRendererEntry? entry))
        {
            throw new InvalidOperationException(
                $"No 3DX renderer exists for celestial body '{body.Id}'.");
        }

        UpdateBodyPosition(node);

        // A renderer-type change still requires a replacement.
        if (entry.Renderer.Type != body.RendererType)
        {
            ReplaceBodyRenderer(node, entry);
            return;
        }

        if (entry.Renderer is ICelestialBodyVisualSettingsUpdater3DX updater)
        {
            updater.UpdateVisualSettings(node);
        }
        else
        {
            // Fallback for renderers that have not yet implemented
            // a targeted visual-settings update.
            entry.Renderer.Update(node, body.TextureSource);
        }
    }

    #endregion

    #region Attachment Management

    public void RefreshAttachments()
    {
        foreach (IAttachmentContainer attachmentContainer in _attachmentContainers)
        {
            attachmentContainer.RefreshAttachments(_attachmentTextureResolver);
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
        foreach (ICameraFacingCelestialObject3DX renderer in _cameraFacingRenderers)
        {
            renderer.UpdateCamera(camera);
        }
    }

    #endregion
}
