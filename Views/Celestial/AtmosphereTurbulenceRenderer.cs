using RealmStudioShapeRenderingLib;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.Views.Celestial;

internal sealed class AtmosphereTurbulenceRenderer : ICelestialBodyAnimationRenderer
{
    private static readonly MeshGeometry3D UnitSphereMesh = CreateSphereMesh();

    private readonly AtmosphereTurbulenceEffect3D _effect;

    private ICelestialBodyVisualTarget? _target;

    public CelestialBodyAnimationType AnimationType =>
        CelestialBodyAnimationType.AtmosphereTurbulenceActivity;

    public AtmosphereTurbulenceRenderer()
    {
        _effect = new AtmosphereTurbulenceEffect3D(
            UnitSphereMesh,
            Colors.White,
            radius: 1.0,
            rotation: Quaternion.Identity,
            enabled: false,
            intensity: 0.0);
    }

    public void Update(
        CelestialBodyAnimation animation,
        ICelestialBodyAnimationTarget target)
    {
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(target);

        if (target is not ICelestialBodyVisualTarget visualTarget)
        {
            throw new InvalidOperationException(
                "AtmosphereTurbulenceActivity requires a celestial body visual target.");
        }

        GasGiantAnimationSettings settings =
            animation.Settings as GasGiantAnimationSettings
            ?? throw new InvalidOperationException(
                "AtmosphereTurbulenceActivity requires GasGiantAnimationSettings.");

        // Detach from the previous target if necessary.
        if (_target is not null &&
            !ReferenceEquals(_target, visualTarget))
        {
            _target.ActivityVisualRoot.Children.Remove(_effect.Root);
        }

        _target = visualTarget;

        bool enabled =
            animation.Enabled &&
            settings.EnableAtmosphericTurbulence;

        // Only keep the effect in the visual tree when enabled.
        if (enabled)
        {
            if (!_target.ActivityVisualRoot.Children.Contains(_effect.Root))
            {
                _target.ActivityVisualRoot.Children.Add(_effect.Root);
            }
        }
        else
        {
            _target.ActivityVisualRoot.Children.Remove(_effect.Root);
        }

        _effect.Update(
            radius: 1.0,
            rotation: Quaternion.Identity,
            baseColor: visualTarget.BodyColor,
            enabled: enabled,
            intensity: settings.TurbulenceIntensity);
    }

    public void AdvanceAnimation(CelestialAnimationFrame frame)
    {
        _effect.AdvanceAnimation(frame);
    }

    public void ResetAnimation()
    {
        // Remove the effect geometry as well as resetting its animation.
        if (_target is not null)
        {
            _target.ActivityVisualRoot.Children.Remove(_effect.Root);
            _target = null;
        }

        _effect.ResetAnimation();
    }

    private static MeshGeometry3D CreateSphereMesh(
        double radius = 1.0,
        int thetaDiv = 128,
        int phiDiv = 64)
    {
        MeshGeometry3D mesh = new();

        for (int latitude = 0; latitude <= phiDiv; latitude++)
        {
            double v = (double)latitude / phiDiv;
            double phi = Math.PI * v;

            for (int longitude = 0; longitude <= thetaDiv; longitude++)
            {
                double u = (double)longitude / thetaDiv;
                double theta = 2.0 * Math.PI * u;

                double x = -radius * Math.Cos(theta) * Math.Sin(phi);
                double y = radius * Math.Cos(phi);
                double z = radius * Math.Sin(theta) * Math.Sin(phi);

                Point3D position = new(x, y, z);

                mesh.Positions.Add(position);
                mesh.Normals.Add(new Vector3D(x, y, z));
                mesh.TextureCoordinates.Add(new System.Windows.Point(u, v));
            }
        }

        for (int latitude = 0; latitude < phiDiv; latitude++)
        {
            for (int longitude = 0; longitude < thetaDiv; longitude++)
            {
                int current = latitude * (thetaDiv + 1) + longitude;
                int next = current + thetaDiv + 1;

                mesh.TriangleIndices.Add(current);
                mesh.TriangleIndices.Add(next);
                mesh.TriangleIndices.Add(current + 1);

                mesh.TriangleIndices.Add(current + 1);
                mesh.TriangleIndices.Add(next);
                mesh.TriangleIndices.Add(next + 1);
            }
        }

        mesh.Freeze();

        return mesh;
    }
}