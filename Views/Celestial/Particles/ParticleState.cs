using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.Views.Celestial.Particles;

internal readonly record struct ParticleState(
    Point3D Position,
    double Size,
    Color Color,
    double Opacity);