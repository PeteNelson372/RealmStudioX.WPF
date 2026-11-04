using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.Views.Celestial
{
    public interface ICelestialBodyVisualTarget : ICelestialBodyAnimationTarget
    {
        Model3DGroup BodyVisualRoot { get; }
        Color BodyColor { get; }
    }
}
