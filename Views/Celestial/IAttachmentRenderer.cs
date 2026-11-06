using RealmStudioShapeRenderingLib;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.Views.Celestial;

public interface IAttachmentRenderer : ICelestialAnimatedObject
{
    string AttachmentId { get; }
    CelestialBodyAttachmentType AttachmentType { get; }
    Model3DGroup Root { get; }
    bool IsEnabled { get; }

    void Update(
        CelestialBodyAttachment attachment,
        BitmapSource? texture,
        double hostRadius);
}