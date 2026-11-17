
using HelixToolkit;
using HelixToolkit.Maths;
using HelixToolkit.Wpf.SharpDX;
using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using System.Numerics;
using System.Windows.Media.Media3D;
using Camera = HelixToolkit.Wpf.SharpDX.Camera;
using Color = System.Windows.Media.Color;
using MeshGeometry3D = HelixToolkit.SharpDX.MeshGeometry3D;
using ProjectionCamera = HelixToolkit.Wpf.SharpDX.ProjectionCamera;

namespace RealmStudioX.WPF.Views.Celestial;

internal abstract class LocalStarCelestialBodyRenderer3DX :
    ICelestialBodyRenderer3DX,
    ICelestialAnimatedObject,
    ICameraFacingCelestialObject3DX
{
    protected static readonly MeshGeometry3D SphereMesh = CreateSphereMesh();

    private readonly GroupModel3D _root = new();
    private readonly GroupModel3D _tiltGroup = new();
    private readonly GroupModel3D _bodyVisualRoot = new();
    private readonly GroupModel3D _activityVisualRoot = new();
    private readonly GroupModel3D _attachmentRoot = new();

    private readonly ScaleTransform3D _bodyScale = new(1, 1, 1);
    private readonly ScaleTransform3D _activityScale = new(1, 1, 1);

    private readonly AxisAngleRotation3D _axialTilt =
        new(new Vector3D(1, 0, 0), 0);

    private readonly AxisAngleRotation3D _bodyRotation =
        new(new Vector3D(0, 1, 0), 0);

    private readonly Transform3DGroup _bodyTransform = new();

    private CelestialBody? _body;
    private CelestialBodyNode? _node;

    private Camera? _camera;

    private double _rotationAngle;
    private bool _rotationEnabled;
    private BasicRotationSettings? _rotationSettings;

    private readonly GroupModel3D _billboardVisualRoot = new();

    private readonly AxisAngleRotation3D _billboardRotation = new(new Vector3D(0, 1, 0), 0);

    private readonly PointLight3D _pointLight = new();

    protected LocalStarCelestialBodyRenderer3DX()
    {
        _tiltGroup.Transform = new RotateTransform3D(_axialTilt);

        _bodyTransform.Children.Add(_bodyScale);
        _bodyTransform.Children.Add(
            new RotateTransform3D(_bodyRotation));

        _bodyVisualRoot.Transform = _bodyTransform;
        _activityVisualRoot.Transform = _activityScale;

        _billboardVisualRoot.Transform =
            new RotateTransform3D(_billboardRotation);

        _activityVisualRoot.Children.Add(_billboardVisualRoot);

        _tiltGroup.Children.Add(_bodyVisualRoot);
        _tiltGroup.Children.Add(_activityVisualRoot);
        _tiltGroup.Children.Add(_attachmentRoot);

        _root.Children.Add(_tiltGroup);

        // Keep the point light outside the body scale and axial-tilt transforms.
        // The renderer's parent still supplies the body's world position.
        _pointLight.Position = new Point3D(0, 0, 0);
        _pointLight.Attenuation = new Vector3D(1.0, 0.0, 0.02);
        _pointLight.Visibility = System.Windows.Visibility.Collapsed;

        _root.Children.Add(_pointLight);
    }

    public abstract CelestialBodyRendererType Type { get; }

    public Element3D Root => _root;

    protected CelestialBody Body =>
        _body ?? throw new InvalidOperationException(
            "The Local Star renderer has not been initialized.");

    protected CelestialBodyNode Node =>
        _node ?? throw new InvalidOperationException(
            "The Local Star renderer has not been initialized.");

    protected GroupModel3D BodyVisualRoot => _bodyVisualRoot;

    protected GroupModel3D ActivityVisualRoot => _activityVisualRoot;

    protected GroupModel3D AttachmentRoot => _attachmentRoot;

    protected GroupModel3D BillboardVisualRoot => _billboardVisualRoot;

    protected double Size => Node.Placement.Size;

    protected Color StarColor => Body.BaseColor;

    protected virtual bool SupportsPointLight => false;

    protected Camera? CurrentCamera => _camera;

    public virtual void UpdateCamera(Camera? camera)
    {
        _camera = camera;

        UpdateBillboardOrientation();

        OnCameraUpdated(camera);
    }

    protected virtual void OnCameraUpdated(Camera? camera)
    {
    }

    private void UpdateBillboardOrientation()
    {
        if (!TryGetCameraDirectionInActivitySpace(
                out Vector3D cameraDirection))
        {
            _billboardRotation.Angle = 0;
            return;
        }

        // The billboard's unrotated normal points along +Z.
        Vector3D normal = new(0, 0, 1);

        double dot = Vector3D.DotProduct(
            normal,
            cameraDirection);

        dot = Math.Clamp(dot, -1.0, 1.0);

        Vector3D axis = Vector3D.CrossProduct(
            normal,
            cameraDirection);

        if (axis.LengthSquared < 1e-12)
        {
            // The vectors are parallel or antiparallel.
            if (dot < 0)
            {
                // A 180-degree rotation around Y handles
                // the antiparallel case.
                _billboardRotation.Axis = new Vector3D(0, 1, 0);
                _billboardRotation.Angle = 180;
            }
            else
            {
                _billboardRotation.Angle = 0;
            }

            return;
        }

        axis.Normalize();

        _billboardRotation.Axis = axis;

        _billboardRotation.Angle =
            Math.Acos(dot) * 180.0 / Math.PI;
    }

    protected bool TryGetCameraDirectionInActivitySpace(
        out Vector3D direction)
    {
        direction = default;

        if (_camera is not ProjectionCamera projectionCamera ||
            _node is null)
        {
            return false;
        }

        // Direction from the star's center toward the camera.
        direction = projectionCamera.Position - Node.Placement.Center;

        if (direction.LengthSquared < 1e-12)
            return false;

        direction.Normalize();

        // Convert from scene coordinates into the coordinate system
        // before the axial-tilt rotation is applied.
        Matrix3D inverseTilt =
            new RotateTransform3D(_axialTilt).Value;

        inverseTilt.Invert();

        direction = inverseTilt.Transform(direction);

        if (direction.LengthSquared < 1e-12)
            return false;

        direction.Normalize();

        return true;
    }

    public void Update(CelestialBodyNode node, CelestialTextureSource? textureSource)
    {
        UpdatePlacement(node);

        UpdateRotationSettings();

        UpdateBodyVisuals(node.Body, textureSource);
    }

    protected abstract void UpdateBodyVisuals(CelestialBody body, CelestialTextureSource? textureSource);

    public void UpdatePlacement(CelestialBodyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(node.Body);
        ArgumentNullException.ThrowIfNull(node.Placement);

        _node = node;
        _body = node.Body;

        double size = node.Placement.Size;

        _axialTilt.Angle = node.Placement.AxialTilt;

        _bodyScale.ScaleX = size;
        _bodyScale.ScaleY = size;
        _bodyScale.ScaleZ = size;

        _activityScale.ScaleX = size;
        _activityScale.ScaleY = size;
        _activityScale.ScaleZ = size;

        UpdatePointLight();
    }

    private void UpdateRotationSettings()
    {
        CelestialBodyAnimation? animation =
            Body.Animations.FirstOrDefault(
                item => item.AnimationType ==
                        CelestialBodyAnimationType.BasicRotation);

        _rotationEnabled = animation?.Enabled == true;

        _rotationSettings =
            animation?.Settings as BasicRotationSettings;

        if (!_rotationEnabled || _rotationSettings is null)
        {
            _rotationAngle = 0;
            _bodyRotation.Angle = 0;
        }
    }

    private void UpdatePointLight()
    {
        CelestialPointLightSettings settings = Body.PointLightSettings;

        bool enabled = SupportsPointLight && settings.Enabled;

        _pointLight.Visibility = enabled
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;

        if (!enabled)
            return;

        double intensity = double.IsFinite(settings.Intensity)
            ? Math.Clamp(settings.Intensity, 0.0, 1.0)
            : 0.1;

        double configuredRange = double.IsFinite(settings.Range)
            ? Math.Clamp(settings.Range, 0.1, 100.0)
            : 10.0;

        // Preserve the legacy behavior of ensuring the light can reach
        // the World Globe at the origin.
        Vector3D center = new(
            Node.Placement.Center.X,
            Node.Placement.Center.Y,
            Node.Placement.Center.Z);

        double effectiveRange = Math.Max(
            configuredRange,
            center.Length + 2.0);

        Color color = StarColor;

        _pointLight.Color = Color.FromRgb(
            (byte)Math.Round(color.R * intensity),
            (byte)Math.Round(color.G * intensity),
            (byte)Math.Round(color.B * intensity));

        _pointLight.Range = effectiveRange;
    }

    public virtual void AdvanceAnimation(CelestialAnimationFrame frame)
    {
        if (!_rotationEnabled ||
            _rotationSettings is null ||
            frame.DeltaSeconds <= 0)
        {
            return;
        }

        double rate = double.IsFinite(_rotationSettings.RotationRate)
            ? _rotationSettings.RotationRate
            : 1.0;

        double direction =
            _rotationSettings.RotationDirection ==
            WorldRotationDirection.Retrograde
                ? -1.0
                : 1.0;

        const double SecondsPerRevolution = 240.0;

        double degreesPerSecond =
            direction * 360.0 / SecondsPerRevolution * rate;

        _rotationAngle =
            (_rotationAngle + degreesPerSecond * frame.DeltaSeconds) % 360.0;

        _bodyRotation.Angle = _rotationAngle;
    }

    public virtual void ResetAnimation()
    {
        _rotationAngle = 0;
        _bodyRotation.Angle = 0;
    }

    protected static Color4 ToColor4(Color color)
    {
        return new Color4(
            color.ScR,
            color.ScG,
            color.ScB,
            color.ScA);
    }

    private static MeshGeometry3D CreateSphereMesh()
    {
        const int longitudeSegments = 128;
        const int latitudeSegments = 64;

        MeshGeometry3D mesh = new()
        {
            Positions = new Vector3Collection(),
            Normals = new Vector3Collection(),
            TextureCoordinates = new Vector2Collection(),
            Indices = new IntCollection()
        };

        int rowLength = longitudeSegments + 1;

        for (int latitude = 0; latitude <= latitudeSegments; latitude++)
        {
            double v = (double)latitude / latitudeSegments;
            double phi = Math.PI / 2.0 - Math.PI * v;

            double cosPhi = Math.Cos(phi);
            double y = Math.Sin(phi);

            for (int longitude = 0; longitude <= longitudeSegments; longitude++)
            {
                double u = (double)longitude / longitudeSegments;
                double theta = 2.0 * Math.PI * u;

                double x = cosPhi * Math.Cos(theta);
                double z = cosPhi * Math.Sin(theta);

                mesh.Positions.Add(new Vector3(
                    (float)x, (float)y, (float)z));

                mesh.Normals.Add(new Vector3(
                    (float)x, (float)y, (float)z));

                mesh.TextureCoordinates.Add(new Vector2(
                    (float)u, (float)v));
            }
        }

        for (int latitude = 0; latitude < latitudeSegments; latitude++)
        {
            for (int longitude = 0; longitude < longitudeSegments; longitude++)
            {
                int a = latitude * rowLength + longitude;
                int b = a + 1;
                int c = a + rowLength;
                int d = c + 1;

                mesh.Indices.Add(a);
                mesh.Indices.Add(b);
                mesh.Indices.Add(c);

                mesh.Indices.Add(b);
                mesh.Indices.Add(d);
                mesh.Indices.Add(c);
            }
        }

        return mesh;
    }
}
