using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;   

namespace RealmStudioX.WPF.Views.Celestial;

public interface ICelestialBodyAnimationTarget
{
    public Transform3DGroup AnimationTransform { get; }
}