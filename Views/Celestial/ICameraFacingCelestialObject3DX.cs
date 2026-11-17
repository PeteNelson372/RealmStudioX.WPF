using HelixToolkit.Wpf.SharpDX;

namespace RealmStudioX.WPF.Views.Celestial
{
    /// <summary>
    /// Optional contract for 3DX visuals whose orientation depends on the camera.
    /// </summary>
    internal interface ICameraFacingCelestialObject3DX
    {
        void UpdateCamera(Camera? camera);
    }
}
