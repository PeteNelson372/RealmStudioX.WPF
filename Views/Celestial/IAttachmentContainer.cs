using System.Windows.Media.Imaging;

namespace RealmStudioX.WPF.Views.Celestial
{
    internal interface IAttachmentContainer
    {
        void RefreshAttachments(
            Func<CelestialBodyAttachment, BitmapSource?> textureResolver);
    }
}
