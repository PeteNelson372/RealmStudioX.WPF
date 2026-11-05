
using RealmStudioShapeRenderingLib;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Celestial;

internal sealed class CoronaCelestialBodyRenderer :
    LocalStarCelestialBodyRenderer
{
    private static readonly MeshGeometry3D SphereMesh =
        CreateSphereMesh();

    private static readonly Material BlackMaterial =
        CreateBlackMaterial();

    private readonly GeometryModel3D _centralSphere = new();

    public override CelestialBodyRendererType Type =>
        CelestialBodyRendererType.Corona;

    public CoronaCelestialBodyRenderer()
    {
        _centralSphere.Geometry = SphereMesh;
        _centralSphere.Material = BlackMaterial;
        _centralSphere.BackMaterial = BlackMaterial;

        BodyVisualRoot.Children.Add(_centralSphere);
    }

    protected override void UpdateBodyVisuals(
        CelestialBody body,
        ImageSource? texture)
    {
        // The central sphere remains opaque black.
        // Solar activity is supplied by SolarActivityAnimationRenderer.
    }

    private static Material CreateBlackMaterial()
    {
        SolidColorBrush brush = new(Colors.Black)
        {
            Opacity = 1.0
        };

        brush.Freeze();

        Material material = new DiffuseMaterial(brush);

        if (material.CanFreeze)
            material.Freeze();

        return material;
    }

    private static MeshGeometry3D CreateSphereMesh()
    {
        MeshGeometry3D mesh = new();

        const int longitudeSegments = 128;
        const int latitudeSegments = 64;

        Point3DCollection positions = [];
        Vector3DCollection normals = [];
        PointCollection textureCoordinates = [];
        Int32Collection indices = [];

        int rowLength = longitudeSegments + 1;

        for (int latitude = 0;
             latitude <= latitudeSegments;
             latitude++)
        {
            double v = (double)latitude / latitudeSegments;
            double phi = Math.PI / 2.0 - Math.PI * v;

            double cosPhi = Math.Cos(phi);
            double y = Math.Sin(phi);

            for (int longitude = 0;
                 longitude <= longitudeSegments;
                 longitude++)
            {
                double u = (double)longitude / longitudeSegments;
                double theta = 2.0 * Math.PI * u;

                double x = cosPhi * Math.Cos(theta);
                double z = cosPhi * Math.Sin(theta);

                positions.Add(new Point3D(x, y, z));
                normals.Add(new Vector3D(x, y, z));
                textureCoordinates.Add(new Point(u, v));
            }
        }

        for (int latitude = 0;
             latitude < latitudeSegments;
             latitude++)
        {
            for (int longitude = 0;
                 longitude < longitudeSegments;
                 longitude++)
            {
                int a = latitude * rowLength + longitude;
                int b = a + 1;
                int c = a + rowLength;
                int d = c + 1;

                indices.Add(a);
                indices.Add(b);
                indices.Add(c);

                indices.Add(b);
                indices.Add(d);
                indices.Add(c);
            }
        }

        mesh.Positions = positions;
        mesh.Normals = normals;
        mesh.TextureCoordinates = textureCoordinates;
        mesh.TriangleIndices = indices;

        if (mesh.CanFreeze)
            mesh.Freeze();

        return mesh;
    }
}
