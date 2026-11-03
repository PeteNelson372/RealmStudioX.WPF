using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.Views.Celestial;

public interface ICelestialBodyAnimationTarget
{
    Transform3DGroup AnimationTransform { get; }
}