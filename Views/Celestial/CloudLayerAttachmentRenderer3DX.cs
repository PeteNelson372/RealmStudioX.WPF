
using HelixToolkit.Maths;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using MeshGeometry3D = HelixToolkit.SharpDX.MeshGeometry3D;

namespace RealmStudioX.WPF.Views.Celestial;

/// <summary>
/// Renders and animates a cloud layer as a native HelixToolkit 3DX visual.
/// </summary>
internal sealed class CloudLayerAttachmentRenderer3DX :
    ICelestialAnimatedObject,
    ICelestialBodyAnimationTarget
{
    private const double CloudLayerRadiusScale = 1.003;

    private readonly GroupModel3D _root = new();
    private readonly MeshGeometryModel3D _surfaceModel = new();

    private readonly ScaleTransform3D _scaleTransform =
        new(1.0, 1.0, 1.0);

    private readonly Transform3DGroup _animationTransform = new();

    private readonly Dictionary<string, ICelestialBodyAnimationRenderer>
        _animationRenderers = [];

    private readonly Func<BitmapSource, TextureModel> _textureModelFactory;

    private BitmapSource? _currentTexture;
    private CelestialBodyAttachment? _attachment;

    public GroupModel3D Root => _root;

    public Transform3DGroup AnimationTransform => _animationTransform;

    public bool IsEnabled { get; private set; }

    public CloudLayerAttachmentRenderer3DX(
        MeshGeometry3D sphereGeometry,
        Func<BitmapSource, TextureModel> textureModelFactory)
    {
        ArgumentNullException.ThrowIfNull(sphereGeometry);

        _textureModelFactory = textureModelFactory
            ?? throw new ArgumentNullException(nameof(textureModelFactory));

        Transform3DGroup transform = new();
        transform.Children.Add(_scaleTransform);
        transform.Children.Add(_animationTransform);

        _root.Transform = transform;

        _surfaceModel.Geometry = sphereGeometry;
        _surfaceModel.IsTransparent = true;

        _root.Children.Add(_surfaceModel);
    }

    public void Update(
        CelestialBodyAttachment attachment,
        BitmapSource? texture,
        double hostRadius)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        _attachment = attachment;

        double radius =
            Math.Max(0.001, hostRadius) * CloudLayerRadiusScale;

        _scaleTransform.ScaleX = radius;
        _scaleTransform.ScaleY = radius;
        _scaleTransform.ScaleZ = radius;

        // Recreate the material only when the actual texture changes.
        if (!ReferenceEquals(_currentTexture, texture))
        {
            _currentTexture = texture;

            if (texture is not null)
            {
                _surfaceModel.Material = new PhongMaterial
                {
                    DiffuseColor = new Color4(1, 1, 1, 1),
                    AmbientColor = new Color4(1, 1, 1, 1),
                    SpecularColor = new Color4(0, 0, 0, 1),
                    SpecularShininess = 1,
                    DiffuseMap = _textureModelFactory(texture)
                };
            }
        }

        IsEnabled = attachment.Enabled && texture is not null;

        RefreshAnimationRenderers();
    }

    private void RefreshAnimationRenderers()
    {
        if (_attachment is null)
            return;

        HashSet<string> activeIds =
        [
            .. _attachment.Animations.Select(animation => animation.Id)
        ];

        foreach (string id in _animationRenderers.Keys.ToArray())
        {
            if (!activeIds.Contains(id))
            {
                _animationRenderers[id].ResetAnimation();
                _animationRenderers.Remove(id);
            }
        }

        foreach (CelestialBodyAnimation animation in _attachment.Animations)
        {
            if (!_animationRenderers.TryGetValue(
                    animation.Id,
                    out ICelestialBodyAnimationRenderer? renderer))
            {
                renderer = AnimationRendererFactory.Create(animation);
                _animationRenderers.Add(animation.Id, renderer);
            }

            renderer.Update(animation, this);
        }
    }

    public void AdvanceAnimation(CelestialAnimationFrame frame)
    {
        if (!IsEnabled || frame.DeltaSeconds <= 0.0)
            return;

        foreach (ICelestialBodyAnimationRenderer renderer
                 in _animationRenderers.Values)
        {
            renderer.AdvanceAnimation(frame);
        }
    }

    public void ResetAnimation()
    {
        foreach (ICelestialBodyAnimationRenderer renderer
                 in _animationRenderers.Values)
        {
            renderer.ResetAnimation();
        }
    }
}
