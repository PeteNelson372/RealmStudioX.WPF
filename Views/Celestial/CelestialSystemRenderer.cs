using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.WorldGlobe;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.Views.Celestial;

internal sealed class CelestialSystemRenderer : IWorldGlobeAnimatedObject
{
    private readonly Func<CelestialBody, ImageSource?> _textureResolver;

    private readonly Func<CelestialBodyAttachment, BitmapSource?>
        _attachmentTextureResolver;

    private readonly List<IWorldGlobeAnimatedObject> _animatedRenderers = [];

    private readonly List<IAttachmentContainer> _attachmentContainers = [];

    public CelestialSystemRenderer(
        Func<CelestialBody, ImageSource?> textureResolver,
        Func<CelestialBodyAttachment, BitmapSource?>? attachmentTextureResolver = null)
    {
        _textureResolver = textureResolver
            ?? throw new ArgumentNullException(nameof(textureResolver));

        _attachmentTextureResolver = attachmentTextureResolver ?? (_ => null);
    }

    public Model3DGroup BuildScene(CelestialSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);

        // Clear previously registered renderers in case the scene is rebuilt.
        _animatedRenderers.Clear();
        _attachmentContainers.Clear();

        Model3DGroup root = new();

        foreach (CelestialBodyNode bodyNode in system.Bodies)
        {
            root.Children.Add(BuildBodyNode(bodyNode, isRoot: true));
        }

        return root;
    }

    private Model3DGroup BuildBodyNode(CelestialBodyNode node, bool isRoot)
    {
        CelestialBody body = node.Body;

        ICelestialBodyRenderer renderer = CreateBodyRenderer(body);

        renderer.Update(body, _textureResolver(body));

        // Register the body renderer for animation.
        // Attachment renderers are advanced by their owning body renderer.
        if (renderer is IWorldGlobeAnimatedObject animatedRenderer)
        {
            _animatedRenderers.Add(animatedRenderer);
        }

        // Register and initialize any attachment container.
        if (renderer is IAttachmentContainer attachmentContainer)
        {
            _attachmentContainers.Add(attachmentContainer);

            attachmentContainer.RefreshAttachments(_attachmentTextureResolver);
        }

        Model3DGroup nodeRoot = new();

        if (!isRoot)
        {
            Transform3DGroup orbitalTransform = new();

            orbitalTransform.Children.Add(new RotateTransform3D(
                new AxisAngleRotation3D(
                    new Vector3D(0, 0, 1),
                    node.OrbitalInclination)));

            orbitalTransform.Children.Add(new RotateTransform3D(
                new AxisAngleRotation3D(
                    new Vector3D(0, 1, 0),
                    node.OrbitalAngle)));

            orbitalTransform.Children.Add(
                new TranslateTransform3D(
                    node.OrbitalDistance,
                    0,
                    0));

            nodeRoot.Transform = orbitalTransform;
        }

        nodeRoot.Children.Add(renderer.Root);

        foreach (CelestialBodyNode child in node.Children)
        {
            nodeRoot.Children.Add(
                BuildBodyNode(child, isRoot: false));
        }

        return nodeRoot;
    }

    public void RefreshAttachments()
    {
        foreach (IAttachmentContainer attachmentContainer in _attachmentContainers)
        {
            attachmentContainer.RefreshAttachments(
                _attachmentTextureResolver);
        }
    }

    public void AdvanceAnimation(WorldGlobeAnimationFrame frame)
    {
        foreach (IWorldGlobeAnimatedObject renderer in _animatedRenderers)
        {
            renderer.AdvanceAnimation(frame);
        }
    }

    public void ResetAnimation()
    {
        foreach (IWorldGlobeAnimatedObject renderer in _animatedRenderers)
        {
            renderer.ResetAnimation();
        }
    }

    private static ICelestialBodyRenderer CreateBodyRenderer(CelestialBody body)
    {
        return body.RendererType switch
        {
            CelestialBodyRendererType.Planet =>
                new PlanetCelestialBodyRenderer(),

            _ => throw new NotSupportedException(
                $"Renderer type '{body.RendererType}' is not supported.")
        };
    }
}