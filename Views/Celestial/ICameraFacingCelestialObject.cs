using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.Views.Celestial;

/// <summary>
/// Identifies a scene object whose presentation must be aligned to the current
/// viewport camera. The viewer should call UpdateCamera when the camera changes
/// (and during its render tick if the camera can move continuously).
/// </summary>
internal interface ICameraFacingCelestialObject
{
    void UpdateCamera(Camera? camera);
}