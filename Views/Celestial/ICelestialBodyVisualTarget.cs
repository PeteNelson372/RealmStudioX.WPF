using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.Views.Celestial
{
    public interface ICelestialBodyVisualTarget : ICelestialBodyAnimationTarget
    {
        Model3DGroup BodyVisualRoot { get; }

        // Receives body-radius scaling, but not body animation transforms.
        Model3DGroup ActivityVisualRoot { get; }

        Color BodyColor { get; }
    }
}
