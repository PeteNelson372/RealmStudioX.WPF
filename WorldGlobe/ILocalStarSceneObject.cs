using RealmStudioShapeRenderingLib;
using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.WorldGlobe
{
    internal interface ILocalStarSceneObject
    {
        LocalStarType Type { get; }

        Model3DGroup Root { get; }

        void Update(LocalStar star);
    }
}
