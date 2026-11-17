
using HelixToolkit.Wpf.SharpDX;
using RealmStudioShapeRenderingLib;

namespace RealmStudioX.WPF.Views.Celestial;

/// <summary>
/// Contract implemented by native HelixToolkit.Wpf.SharpDX
/// celestial body renderers.
/// </summary>
internal interface ICelestialBodyRenderer3DX
{
    CelestialBodyRendererType Type { get; }

    Element3D Root { get; }

    void Update(CelestialBodyNode node, CelestialTextureSource? textureSource);

    void UpdatePlacement(CelestialBodyNode node);
}
