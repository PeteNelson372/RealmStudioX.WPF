using RealmStudioShapeRenderingLib;
using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.Views.Celestial
{
    internal interface ICelestialBodyRenderer
    {
        CelestialBodyRendererType Type { get; }

        Model3DGroup Root { get; }

        void Update(CelestialBodyNode node, CelestialTextureSource? textureSource);
    }
}