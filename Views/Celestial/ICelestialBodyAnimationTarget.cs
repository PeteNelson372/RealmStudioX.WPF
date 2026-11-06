using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.Views.Celestial;

public interface ICelestialBodyAnimationTarget
{
    public Transform3DGroup AnimationTransform { get; }
}