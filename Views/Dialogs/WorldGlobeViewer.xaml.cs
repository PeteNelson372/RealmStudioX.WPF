using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Panels.WorldGlobeViewerPanels;
using SkiaSharp;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using UserControl = System.Windows.Controls.UserControl;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for WorldGlobeViewer.xaml
    /// </summary>
    public partial class WorldGlobeViewer :  ModelessDialog
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public WorldGlobeViewModel ViewModel { get; private set; }

        private readonly Dictionary<string, UserControl> _toolPanels = new()
        {
            ["Starfield"] = new StarfieldTools(),
            ["World"] = new WorldTools(),
            ["Surface"] = new SurfaceTools(),
            ["Atmosphere"] = new AtmosphereTools(),
            ["Lighting"] = new LightingTools(),
            ["LocalStar"] = new LocalStarTools(),
            ["Rings"] = new PlanetaryRingsTools(),
            ["Moons"] = new PlanetaryMoonsTools(),
            ["Effects"] = new EffectsTools(),            
        };

        public static readonly int[] SupportedFrameRates =
            [
                15, 24, 25, 30, 50, 60
            ];

        private const int GlobeTextureEdgeInset = 3;

        private readonly AxisAngleRotation3D _globeYaw = new(new Vector3D(0, 1, 0), 0);

        private readonly AxisAngleRotation3D _globePitch = new(new Vector3D(1, 0, 0), 0);

        private bool _isRotatingGlobe;
        private Point _lastGlobeDragPoint;

        private const double GlobeRotationSensitivity = 0.5;

        private readonly AxisAngleRotation3D _globeAutoRotation = new(new Vector3D(0, 1, 0), 0);

        private const double SecondsPerRevolution = 60.0;

        private bool _isGlobeRotationAnimating;

        private static readonly QuaternionRotation3D _globeSpinRotation = new(Quaternion.Identity);

        private readonly Model3DGroup _globeSpinGroup = new()
        {
            Transform = new RotateTransform3D(_globeSpinRotation)
        };

        private Model3DGroup? _globeBody;
        public Model3DGroup? GlobeBody => _globeBody;
        private GeometryModel3D? _globeSurfaceModel;

        private readonly AxisAngleRotation3D _globeTiltRotation = new(new Vector3D(1, 0, 0), 0);

        private readonly Model3DGroup _globeTiltGroup = new();

        private readonly Transform3DGroup _globeTransform = new();
        public Transform3DGroup GlobeTransform => _globeTransform;

        private readonly Transform3DGroup _globeOrientationTransform = new();

        private MainWindowViewModel _mainViewModel;

        public WorldGlobeViewer(MainWindowViewModel mainViewModel)
        {
            InitializeComponent();

            _mainViewModel = mainViewModel;

            ViewModel = new WorldGlobeViewModel(mainViewModel, this);

            DataContext = ViewModel;

            WorldGlobeViewerTitleBar.DataContext = ViewModel;

            WorldGlobeViewerMenu.DataContext = ViewModel;

            Loaded += (s, e) =>
            {
                WorldGlobeViewerTitleBar.MinimizeClicked += (s, e) => MinimizeHandler();
                WorldGlobeViewerTitleBar.MaximizeClicked += (s, e) => MaximizeHandler();
                WorldGlobeViewerTitleBar.ExitClicked += (s, e) => ExitHandler();

                WorldGlobeViewerMenu.ExitClicked += (s, e) => ExitHandler();
                GlobeTransform.Children.Clear();

                GlobeTransform.Children.Add(new RotateTransform3D(_globeYaw));

                GlobeTransform.Children.Add(new RotateTransform3D(_globePitch));

                // Use HelixToolkit for left-drag panning,
                // but not for right-drag camera rotation.
                ModelViewer.SetCameraRotationEnabled(false);

                ModelViewer.PreviewMouseDown += ModelViewer_PreviewMouseDown;
                ModelViewer.PreviewMouseMove += ModelViewer_PreviewMouseMove;
                ModelViewer.PreviewMouseUp += ModelViewer_PreviewMouseUp;
                ModelViewer.LostMouseCapture += ModelViewer_LostMouseCapture;

                CreateGlobe();

                FrameRateCombo.ItemsSource = SupportedFrameRates;

                if (EnableRotationCheckBox.IsChecked == true)
                {
                    StartGlobeRotation();
                }
            };
        }

        private void ShowHideAnimationTimeline(object sender, RoutedEventArgs e)
        {
            AnimationTimelineExpander.IsExpanded = !AnimationTimelineExpander.IsExpanded;
        }

        private void SceneTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is not TreeViewItem selectedItem)
                return;

            string? selectedCategory = selectedItem.Tag?.ToString();

            if (!string.IsNullOrEmpty(selectedCategory))
            {
                ShowToolPanel(selectedCategory);
            }
        }

        private void ShowToolPanel(string? tab)
        {
            if (string.IsNullOrEmpty(tab) || !_toolPanels.ContainsKey(tab))
            {
                ToolsPanelHost.Content = null;
                return;
            }

            if (_toolPanels[tab] is StarfieldTools st)
            {
                st.DataContext = ViewModel;
            }


            ToolsPanelHost.Content = _toolPanels[tab];
        }

        private void ModelViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right || GlobeBody is null)
            {
                return;
            }

            _isRotatingGlobe = true;
            _lastGlobeDragPoint = e.GetPosition(ModelViewer);

            Mouse.Capture(ModelViewer, CaptureMode.SubTree);

            // Prevent HelixToolkit from interpreting this as
            // a camera-rotation gesture.
            e.Handled = true;
        }

        private void ModelViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isRotatingGlobe ||
                e.RightButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point currentPosition = e.GetPosition(ModelViewer);

            double deltaX = currentPosition.X - _lastGlobeDragPoint.X;
            double deltaY = currentPosition.Y - _lastGlobeDragPoint.Y;

            _globeYaw.Angle += deltaX * GlobeRotationSensitivity;
            _globePitch.Angle += deltaY * GlobeRotationSensitivity;

            double actualTilt = GetCurrentAxialTilt();

            ViewModel.UpdateCurrentTiltAngle(actualTilt);

            _lastGlobeDragPoint = currentPosition;

            e.Handled = true;
        }

        private void ModelViewer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right ||
                !_isRotatingGlobe)
            {
                return;
            }

            _isRotatingGlobe = false;
            Mouse.Capture(null);

            e.Handled = true;
        }

        private void ModelViewer_LostMouseCapture(object sender, MouseEventArgs e)
        {
            _isRotatingGlobe = false;
        }

        private void EnableRotation_Checked(object sender, RoutedEventArgs e)
        {
            StartGlobeRotation();
        }

        private void EnableRotation_Unchecked(object sender, RoutedEventArgs e)
        {
            StopGlobeRotation();
        }

        public void CreateGlobe()
        {
            SKBitmap? globeTexture = CreateWorldGlobeTexture();

            if (globeTexture != null)
            {
                InitializeWorldGlobe(globeTexture);
            }
        }

        public void InitializeWorldGlobe(SKBitmap worldTexture)
        {
            ArgumentNullException.ThrowIfNull(worldTexture);

            if (worldTexture.ToImageSource() is not BitmapSource bitmapSource)
            {
                return;
            }

            ImageBrush textureBrush = new(bitmapSource)
            {
                Stretch = Stretch.Fill
            };

            textureBrush.Freeze();

            DiffuseMaterial material = new(textureBrush);
            material.Freeze();

            if (_globeSurfaceModel is null)
            {
                _globeSurfaceModel = new GeometryModel3D
                {
                    Geometry = CreateSphereMesh(),
                    Material = material,
                    BackMaterial = material
                };

                // Attach the surface to the independently rotating group.
                _globeSpinGroup.Children.Add(_globeSurfaceModel);

                ModelViewer.ShowGrid(false);
                ModelViewer.ShowBoundingBox(false);
                ModelViewer.ShowWireframe(false);
                ModelViewer.ShowViewCube(false);
                ModelViewer.ShowCoordinateSystem(false);
            }
            else
            {
                // Replace the texture without rebuilding the sphere.
                _globeSurfaceModel.Material = material;
                _globeSurfaceModel.BackMaterial = material;
            }

            if (_globeBody is null)
            {
                // The parent controls manual orientation.
                _globeBody = new Model3DGroup
                {
                    Transform = GlobeTransform
                };

                // The child controls automatic rotation.
                _globeBody.Children.Add(_globeSpinGroup);

                _globeTiltGroup.Transform = new RotateTransform3D(_globeTiltRotation);

                _globeTiltGroup.Children.Clear();
                _globeTiltGroup.Children.Add(_globeSpinGroup);

                _globeBody.Children.Clear();
                _globeBody.Children.Add(_globeTiltGroup);

                ModelViewer.SetModel(_globeBody);
            }
        }

        private static MeshGeometry3D CreateSphereMesh(double radius = 1.0, int longitudeSegments = 128, int latitudeSegments = 64)
        {
            MeshGeometry3D mesh = new();

            Point3DCollection positions = new();
            Vector3DCollection normals = new();
            PointCollection textureCoordinates = new();
            Int32Collection triangleIndices = new();

            int rowLength = longitudeSegments + 1;

            // Create vertices, normals, and texture coordinates.
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

                    positions.Add(new Point3D(
                        radius * x,
                        radius * y,
                        radius * z));

                    normals.Add(new Vector3D(x, y, z));

                    // Duplicate the longitude seam so the texture wraps cleanly.
                    textureCoordinates.Add(new Point(u, v));
                }
            }

            // Create triangles with outward-facing winding.
            for (int latitude = 0; latitude < latitudeSegments; latitude++)
            {
                for (int longitude = 0; longitude < longitudeSegments; longitude++)
                {
                    int a = latitude * rowLength + longitude;
                    int b = a + 1;
                    int c = a + rowLength;
                    int d = c + 1;

                    triangleIndices.Add(a);
                    triangleIndices.Add(b);
                    triangleIndices.Add(c);

                    triangleIndices.Add(b);
                    triangleIndices.Add(d);
                    triangleIndices.Add(c);
                }
            }

            mesh.Positions = positions;
            mesh.Normals = normals;
            mesh.TextureCoordinates = textureCoordinates;
            mesh.TriangleIndices = triangleIndices;

            if (mesh.CanFreeze)
            {
                mesh.Freeze();
            }

            return mesh;
        }

        private SKBitmap? CreateWorldGlobeTexture()
        {
            if (_mainViewModel.Editor.Scene == null || _mainViewModel.Editor.Scene.Map == null)
            {
                return null;
            }

            RealmStudioMap map = _mainViewModel.Editor.Scene.Map;

            int width = map.MapWidth;
            int height = map.MapHeight;

            if (width <= GlobeTextureEdgeInset * 2 ||
                height <= GlobeTextureEdgeInset * 2)
            {
                throw new InvalidOperationException("The map is too small to create a globe texture.");
            }

            // Render the map without editor overlays.
            using var renderedMap = new SKBitmap(new SKImageInfo(width, height));

            using (var renderCanvas = new SKCanvas(renderedMap))
            {
                renderCanvas.Clear(SKColors.Transparent);

                _mainViewModel.Editor.Scene.RenderFor3DGlobeView(renderCanvas);
            }

            // Create the final texture at the original map dimensions.
            var globeTexture = new SKBitmap(new SKImageInfo(width, height));

            try
            {
                using var textureCanvas = new SKCanvas(globeTexture);

                textureCanvas.Clear(SKColors.Transparent);

                // Exclude the outer three pixels on each edge.
                SKRect sourceRect = new(
                    GlobeTextureEdgeInset,
                    GlobeTextureEdgeInset,
                    width - GlobeTextureEdgeInset,
                    height - GlobeTextureEdgeInset);

                // Expand the cropped image back to the original dimensions.
                SKRect destinationRect = new(0, 0, width, height);

                textureCanvas.DrawBitmap(
                    renderedMap,
                    sourceRect,
                    destinationRect,
                    SKSamplingOptions.Default);

                return globeTexture;
            }
            catch
            {
                globeTexture.Dispose();
                throw;
            }
        }

        private double _globeSpinAngle;
        private TimeSpan? _lastRenderingTime;

        private void StartGlobeRotation()
        {
            if (GlobeBody is null || _isGlobeRotationAnimating)
            {
                return;
            }

            _lastRenderingTime = null;
            CompositionTarget.Rendering += GlobeRendering;
            _isGlobeRotationAnimating = true;
        }

        private void StopGlobeRotation()
        {
            if (!_isGlobeRotationAnimating)
                return;

            CompositionTarget.Rendering -= GlobeRendering;

            _lastRenderingTime = null;
            _isGlobeRotationAnimating = false;
        }

        private void GlobeRendering(object? sender, EventArgs e)
        {
            if (e is not RenderingEventArgs renderingArgs)
                return;

            TimeSpan currentTime = renderingArgs.RenderingTime;

            if (_lastRenderingTime is null)
            {
                _lastRenderingTime = currentTime;
                return;
            }

            double elapsedSeconds =
                (currentTime - _lastRenderingTime.Value).TotalSeconds;

            _lastRenderingTime = currentTime;

            if (elapsedSeconds <= 0)
            {
                return;
            }

            // Avoid a large jump if rendering was temporarily interrupted.
            elapsedSeconds = Math.Min(elapsedSeconds, 0.1);

            double direction = ViewModel.RotationDirection == WorldRotationDirection.Prograde ? 1.0 : -1.0;

            double rotationRate = Math.Clamp(ViewModel.RotationRate, 0.1f, 10.0f);

            double degreesPerSecond = (360.0 / SecondsPerRevolution) * rotationRate;

            _globeSpinAngle += direction * degreesPerSecond * elapsedSeconds;

            // Keep the angle bounded during extended sessions.
            _globeSpinAngle %= 360.0;

            _globeSpinRotation.Quaternion = new Quaternion(new Vector3D(0, 1, 0), _globeSpinAngle);
        }

        public void UpdateGlobeTilt(double tiltAngle)
        {
            if (!double.IsFinite(tiltAngle))
            {
                return;
            }

            _globeTiltRotation.Angle = Math.Clamp(tiltAngle, 0.0, 90.0);
        }

        public double GetCurrentAxialTilt()
        {
            // The globe's north axis in its local coordinate system.
            Vector3D northAxis = new(0, 1, 0);

            // Apply the transforms from the globe surface outward.
            northAxis = _globeSpinGroup.Transform.Transform(northAxis);
            northAxis = _globeTiltGroup.Transform.Transform(northAxis);
            northAxis = GlobeTransform.Transform(northAxis);

            northAxis.Normalize();

            Vector3D worldUp = new(0, 1, 0);

            // Treat the axis as an undirected line, so the reported
            // inclination remains between 0 and 90 degrees.
            double cosine = Math.Abs(
                Vector3D.DotProduct(northAxis, worldUp));

            cosine = Math.Clamp(cosine, 0.0, 1.0);

            return Math.Acos(cosine) * 180.0 / Math.PI;
        }

        public void ResetGlobeView()
        {
            // Reset manual globe orientation.
            _globeYaw.Angle = 0;
            _globePitch.Angle = 0;

            // Reset the configured axial tilt.
            ViewModel.WorldTiltAngle = 0;
            _globeTiltRotation.Angle = 0;

            // Reset the automatic spin.
            _globeSpinAngle = 0;
            _globeSpinRotation.Quaternion = Quaternion.Identity;

            // Restart the rendering time baseline so the next frame
            // doesn't apply an unexpectedly large time delta.
            _lastRenderingTime = null;

            // Update the displayed tilt readout.
            ViewModel.UpdateCurrentTiltAngle(0);

            // Reset camera position and orientation.
            ModelViewer.ResetCamera();
        }

        private void ExitHandler()
        {
            Close();
        }

        private void MaximizeHandler()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void MinimizeHandler()
        {
            WindowState = WindowState.Minimized;
        }

    }
}
