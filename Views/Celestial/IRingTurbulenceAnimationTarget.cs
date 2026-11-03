using System.Windows.Media.Imaging;

namespace RealmStudioX.WPF.Views.Celestial;

public interface IRingTurbulenceAnimationTarget
{
    BitmapSource? SourceTexture { get; }

    void SetAnimatedTexture(BitmapSource? texture);
}