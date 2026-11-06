using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Celestial;
using RealmStudioX.WPF.Views.Panels.WorldGlobeViewerPanels;
using SkiaSharp;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using UserControl = System.Windows.Controls.UserControl;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for WorldGlobeViewer.xaml
    /// </summary>
    public partial class WorldGlobeViewer : ModelessDialog, ICelestialAnimatedObject
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public WorldGlobeSceneViewModel ViewModel { get; private set; }

        private readonly Dictionary<string, UserControl> _toolPanels = new()
        {
            ["Starfield"] = new StarfieldTools(),
            ["World"] = new WorldTools(),
            ["Surface"] = new SurfaceTools(),
            ["Atmosphere"] = new AtmosphereTools(),
            ["Lighting"] = new LightingTools(),
            ["LocalStar"] = new LocalStarTools(),
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


        // Retained as a scene-graph grouping node. Surface rotation is
        // now handled by PlanetCelestialBodyRenderer.
        private readonly Model3DGroup _globeSpinGroup = new();

        private Model3DGroup? _globeBody;
        public Model3DGroup? GlobeBody => _globeBody;

        private readonly AxisAngleRotation3D _globeTiltRotation = new(new Vector3D(1, 0, 0), 0);

        private readonly Model3DGroup _globeTiltGroup = new();

        private readonly Transform3DGroup _globeTransform = new();
        public Transform3DGroup GlobeTransform => _globeTransform;

        private readonly Transform3DGroup _worldVisualTransform = new();

        // Cloud texture processing remains in the viewer; the actual cloud
        // geometry and animation belong to the celestial-body attachment.
        private BitmapSource? _cloudSourceTexture;
        private BitmapSource? _tintedCloudTexture;
        private Color? _tintedCloudColor;
        private double? _tintedCloudOpacity;
        private CelestialBodyAttachment? _cloudLayerAttachment;
        private CelestialBodyAttachment? _planetaryRingsAttachment;

        private readonly Dictionary<string, BitmapSource> _textureCache = new(StringComparer.OrdinalIgnoreCase);

        private readonly CelestialAnimationController _animationController = new();

        private bool _isRenderingSubscribed;

        private GeometryModel3D? _atmosphereModel;

        private readonly Model3DGroup _sceneRootModel = new();

        private CelestialSystem? _celestialSystem;
        private CelestialSystemRenderer? _celestialSystemRenderer;

        private CelestialBody? _worldBody;

        private CelestialBodyNode? _worldNode;

        private CelestialBodyNode? _localStarNode;

        private BitmapSource? _worldTextureSource;

        public static readonly Color DefaultSpaceColor = Color.FromRgb(5, 8, 18);

        private MainWindowViewModel _mainViewModel;

        public WorldGlobeViewer(MainWindowViewModel mainViewModel)
        {
            InitializeComponent();

            _mainViewModel = mainViewModel;

            ViewModel = new WorldGlobeSceneViewModel(mainViewModel, this);

            DataContext = ViewModel;

            WorldGlobeViewerTitleBar.DataContext = ViewModel;

            WorldGlobeViewerMenu.DataContext = ViewModel;

            Loaded += (s, e) =>
            {
                WorldGlobeViewerTitleBar.MinimizeClicked += (s, e) => MinimizeHandler();
                WorldGlobeViewerTitleBar.MaximizeClicked += (s, e) => MaximizeHandler();
                WorldGlobeViewerTitleBar.ExitClicked += (s, e) => ExitHandler();

                WorldGlobeViewerMenu.ExitClicked += (s, e) => ExitHandler();

                _animationController.Register(this);

                Loaded += WorldGlobeViewer_Loaded;
                Unloaded += WorldGlobeViewer_Unloaded;

                ModelViewer.SetBackgroundColor(DefaultSpaceColor);

                _worldVisualTransform.Children.Clear();

                _worldVisualTransform.Children.Add(new RotateTransform3D(_globeYaw));

                _worldVisualTransform.Children.Add(new RotateTransform3D(_globePitch));

                // Use HelixToolkit for left-drag panning,
                // but not for right-drag camera rotation.
                ModelViewer.SetCameraRotationEnabled(false);

                ModelViewer.PreviewMouseDown += ModelViewer_PreviewMouseDown;
                ModelViewer.PreviewMouseMove += ModelViewer_PreviewMouseMove;
                ModelViewer.PreviewMouseUp += ModelViewer_PreviewMouseUp;
                ModelViewer.LostMouseCapture += ModelViewer_LostMouseCapture;

                CreateGlobe();

                if (ViewModel.AtmosphereModel.EnableClouds)
                {
                    ViewModel.AtmosphereModel.UseDefaultCloudTexture = true;
                    RefreshCloudLayer();
                }


                FrameRateCombo.ItemsSource = SupportedFrameRates;
            };
        }

        private void WorldGlobeViewer_Loaded(object sender, RoutedEventArgs e)
        {
            SetAnimationEnabled(ViewModel.EnableAnimation);
        }

        private void WorldGlobeViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            StopRendering();
            _animationController.ResetClock();
        }

        public void SetAnimationEnabled(bool enabled)
        {
            _animationController.IsEnabled = enabled;
            _animationController.ResetClock();

            // Camera-facing effects need continuous updates even when
            // animation is disabled.
            bool needsRendering =
                enabled || _celestialSystemRenderer is not null;

            if (needsRendering && IsLoaded)
            {
                StartRendering();
            }
            else
            {
                StopRendering();
            }
        }

        private void StartRendering()
        {
            if (_isRenderingSubscribed)
            {
                return;
            }

            CompositionTarget.Rendering += GlobeRendering;
            _isRenderingSubscribed = true;
        }

        private void StopRendering()
        {
            if (!_isRenderingSubscribed)
            {
                return;
            }

            CompositionTarget.Rendering -= GlobeRendering;
            _isRenderingSubscribed = false;
        }

        private void GlobeRendering(object? sender, EventArgs e)
        {
            if (e is not RenderingEventArgs renderingArgs)
            {
                return;
            }

            _animationController.Advance(renderingArgs.RenderingTime);

            // Keep camera-facing effects oriented toward the viewer.
            UpdateCameraFacingLocalStar();
        }

        private void UpdateCameraFacingLocalStar()
        {
            Camera? camera = ModelViewer.Camera;
            _celestialSystemRenderer?.UpdateCamera(camera);
        }

        void ICelestialAnimatedObject.AdvanceAnimation(CelestialAnimationFrame frame)
        {
            if (frame.DeltaSeconds <= 0.0)
                return;

            CelestialBodyAnimation? rotationAnimation =
                _worldBody?.Animations.FirstOrDefault(animation =>
                    animation.AnimationType ==
                    CelestialBodyAnimationType.BasicRotation);

            if (rotationAnimation?.Settings is BasicRotationSettings settings)
            {
                settings.RotationRate =
                    ViewModel.WorldGlobeModel.RotationRate;

                settings.RotationDirection =
                    ViewModel.WorldGlobeModel.RotationDirection;
            }

            CelestialBodyAnimation? cloudRotationAnimation =
                _cloudLayerAttachment?.Animations.FirstOrDefault(
                    animation => animation.AnimationType ==
                                 CelestialBodyAnimationType.BasicRotation);

            if (cloudRotationAnimation?.Settings is BasicRotationSettings cloudSettings)
            {
                cloudSettings.RotationDirection =
                    ViewModel.WorldGlobeModel.RotationDirection;
            }

            _celestialSystemRenderer?.AdvanceAnimation(frame);
        }

        void ICelestialAnimatedObject.ResetAnimation()
        {
            _celestialSystemRenderer?.ResetAnimation();
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

            ViewModel.WorldGlobeModel.UpdateCurrentTiltAngle(actualTilt);

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

        public void CreateGlobe()
        {
            SKBitmap? globeTexture = CreateWorldGlobeTexture();

            if (globeTexture != null)
            {
                InitializeWorldGlobeScene(globeTexture);
            }
        }

        public void InitializeWorldGlobeScene(SKBitmap worldTexture)
        {
            ArgumentNullException.ThrowIfNull(worldTexture);

            if (worldTexture.ToImageSource() is not BitmapSource bitmapSource)
                return;

            _worldTextureSource = bitmapSource;

            // Create the celestial system only once.
            if (_celestialSystem is null)
            {
                // create the world globe and its basic rotation animation
                _worldBody = new CelestialBody
                {
                    Name = "World",
                    Description = "The world represented by this globe.",
                    Radius = 1.0,
                    BaseColor = Colors.White,
                    RendererType = CelestialBodyRendererType.Planet,
                    TextureSource = new CelestialTextureSource
                    {
                        SourceType = CelestialTextureSourceType.RealmMap
                    }
                };

                _worldBody.Animations.Add(new CelestialBodyAnimation
                {
                    AnimationType = CelestialBodyAnimationType.BasicRotation,
                    Enabled = true,
                    Settings = new BasicRotationSettings
                    {
                        RotationRate = ViewModel.WorldGlobeModel.RotationRate,
                        RotationDirection = ViewModel.WorldGlobeModel.RotationDirection
                    }
                });

                // create the celestial system and add the world body to it

                _celestialSystem = new CelestialSystem
                {
                    Name = "World",
                    Description = "World Globe"
                };

                _worldNode = new CelestialBodyNode
                {
                    Body = _worldBody,
                    Center = new Point3D(0, 0, 0)
                };

                _celestialSystem.Bodies.Add(_worldNode);

                // The cloud attachment is initialized from the body's current
                // rotation settings, then maintains its own independent values.
                EnsureCloudLayerAttachment();

                _celestialSystemRenderer = new CelestialSystemRenderer(
                    ResolveBodyTexture,
                    ResolveAttachmentTexture);
            }

            // Build the celestial scene and apply the World Globe's visual
            // transform. This same method is used when the celestial
            // hierarchy is rebuilt after adding or removing a Local Star.
            RebuildCelestialScene();

            // Preserve the existing globe transform hierarchy.
            if (_globeBody is null)
            {
                _globeBody = new Model3DGroup
                {
                    Transform = GlobeTransform
                };

                _globeTiltGroup.Transform =
                    new RotateTransform3D(_globeTiltRotation);

                _globeTiltGroup.Children.Clear();
                _globeTiltGroup.Children.Add(_globeSpinGroup);

                _globeBody.Children.Add(_globeTiltGroup);

                _sceneRootModel.Children.Clear();
                _sceneRootModel.Children.Add(_globeBody);

                ModelViewer.ShowGrid(false);
                ModelViewer.ShowBoundingBox(false);
                ModelViewer.ShowWireframe(false);
                ModelViewer.ShowViewCube(false);
                ModelViewer.ShowCoordinateSystem(false);

                ModelViewer.SetModel(_sceneRootModel);
            }

            _celestialSystemRenderer?.RefreshAttachments();
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

        private TimeSpan? _lastRenderingTime;


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
            Vector3D northAxis = new(0, 1, 0);

            // Apply the configured axial tilt.
            northAxis = _globeTiltGroup.Transform.Transform(northAxis);

            // Apply each manual visual transform in sequence.
            foreach (Transform3D transform in _worldVisualTransform.Children)
            {
                northAxis = transform.Transform(northAxis);
            }

            northAxis.Normalize();

            Vector3D worldUp = new(0, 1, 0);

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
            ViewModel.WorldGlobeModel.WorldTiltAngle = 0;
            _globeTiltRotation.Angle = 0;

            // Reset the world's body rotation.
            _celestialSystemRenderer?.ResetAnimation();

            // Restart the rendering time baseline so the next frame
            // doesn't apply an unexpectedly large time delta.
            _lastRenderingTime = null;

            // Update the displayed tilt readout.
            ViewModel.WorldGlobeModel.UpdateCurrentTiltAngle(0);

            // Reset camera position and orientation.
            ModelViewer.ResetCamera();
        }

        public void UpdateAtmosphere(bool enabled, double height, double density, System.Windows.Media.Color color)
        {
            // Clamp incoming settings to their valid ranges.
            height = Math.Max(0.0, height);
            density = Math.Clamp(density, 0.0, 1.0);

            // Create the atmosphere model only once.
            if (_atmosphereModel is null)
            {
                _atmosphereModel = new GeometryModel3D
                {
                    Geometry = CreateSphereMesh(),
                };
            }

            // The atmosphere must sit outside the globe surface.
            // A small minimum offset avoids coincident surfaces.
            double atmosphereRadius = 1.0 + Math.Max(height, 0.001);

            _atmosphereModel.Transform = new ScaleTransform3D(
                atmosphereRadius,
                atmosphereRadius,
                atmosphereRadius);

            // Use density as the opacity for this initial implementation.
            SolidColorBrush atmosphereBrush = new(color)
            {
                Opacity = density
            };

            atmosphereBrush.Freeze();

            Material atmosphereMaterial = new DiffuseMaterial(atmosphereBrush);
            atmosphereMaterial.Freeze();

            _atmosphereModel.Material = atmosphereMaterial;
            _atmosphereModel.BackMaterial = atmosphereMaterial;

            // The atmosphere belongs to the World Globe's visual root,
            // so it follows the globe's position and manual rotation.
            if (_worldBody is not null && _celestialSystemRenderer is not null)
            {
                if (enabled)
                {
                    _celestialSystemRenderer.AddBodyVisual(
                        _worldBody,
                        _atmosphereModel);
                }
                else
                {
                    _celestialSystemRenderer.RemoveBodyVisual(
                        _worldBody,
                        _atmosphereModel);
                }
            }
        }

        public void RefreshAtmosphere()
        {
            UpdateAtmosphere(
                ViewModel.AtmosphereModel.EnableAtmosphere,
                ViewModel.AtmosphereModel.AtmosphereAltitude / 1000.0,
                ViewModel.AtmosphereModel.AtmosphereDensity / 100.0,
                ViewModel.AtmosphereModel.AtmosphereColor);
        }

        public void LoadCloudTexture(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) ||
                !File.Exists(filePath))
            {
                throw new FileNotFoundException("Cloud texture file not found.", filePath);
            }

            BitmapImage bitmap = new();

            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            SetCloudTexture(bitmap);
        }

        private void SetCloudTexture(BitmapSource source)
        {
            _cloudSourceTexture = source;

            // Force regeneration when the input image changes.
            _tintedCloudTexture = null;
            _tintedCloudColor = null;
            _tintedCloudOpacity = null;

            RefreshCloudLayer();
        }

        public void LoadDefaultCloudTexture()
        {
            string path = WorldGlobeAssets.GetPath("cloud_combined_2048.jpg");
            LoadCloudTexture(path);
        }

        private void EnsureCloudLayerAttachment()
        {
            if (_worldBody is null)
                return;

            _cloudLayerAttachment = _worldBody.Attachments.FirstOrDefault(
                attachment => attachment.Type == CelestialBodyAttachmentType.CloudLayer);

            if (_cloudLayerAttachment is not null)
                return;

            _cloudLayerAttachment = new CelestialBodyAttachment
            {
                Name = "Cloud Layer",
                Description = "A rotating cloud layer surrounding the world.",
                Type = CelestialBodyAttachmentType.CloudLayer,
                Enabled = ViewModel.AtmosphereModel.EnableClouds
            };

            _cloudLayerAttachment.Animations.Add(new CelestialBodyAnimation
            {
                AnimationType = CelestialBodyAnimationType.BasicRotation,
                Enabled = true,
                Settings = new BasicRotationSettings
                {
                    RotationRate = Math.Clamp(
                        ViewModel.AtmosphereModel.CloudRotationRate, 0.0, 10.0),
                    RotationDirection = WorldRotationDirection.Prograde
                }
            });

            _worldBody.Attachments.Add(_cloudLayerAttachment);
        }

        public void RefreshCloudLayer()
        {
            EnsureCloudLayerAttachment();

            if (_cloudLayerAttachment is null)
                return;

            _cloudLayerAttachment.Enabled =
                ViewModel.AtmosphereModel.EnableClouds;

            double cloudOpacity = Math.Clamp(
                ViewModel.AtmosphereModel.CloudOpacity, 0.0, 1.0);

            CelestialBodyAnimation? rotationAnimation =
                _cloudLayerAttachment.Animations.FirstOrDefault(
                    animation =>
                        animation.AnimationType ==
                        CelestialBodyAnimationType.BasicRotation);

            if (rotationAnimation?.Settings is BasicRotationSettings rotationSettings)
            {
                rotationSettings.RotationRate = Math.Clamp(
                    ViewModel.AtmosphereModel.CloudRotationRate, 0.0, 10.0);
            }

            if (_cloudSourceTexture is not null)
            {
                Color cloudColor = ViewModel.AtmosphereModel.CloudColor;

                if (_tintedCloudTexture is null ||
                    _tintedCloudColor != cloudColor ||
                    _tintedCloudOpacity != cloudOpacity)
                {
                    _tintedCloudTexture = CreateTintedCloudTexture(
                        _cloudSourceTexture, cloudColor, cloudOpacity);

                    _tintedCloudColor = cloudColor;
                    _tintedCloudOpacity = cloudOpacity;
                }
            }

            _celestialSystemRenderer?.RefreshAttachments();
        }

        private static BitmapSource CreateTintedCloudTexture(
            BitmapSource source,
            Color tint,
            double opacity)
        {
            BitmapSource converted = new FormatConvertedBitmap(
                source,
                PixelFormats.Bgra32,
                null,
                0);

            int width = converted.PixelWidth;
            int height = converted.PixelHeight;
            int stride = width * 4;

            byte[] pixels = new byte[stride * height];
            converted.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                double blue = pixels[i];
                double green = pixels[i + 1];
                double red = pixels[i + 2];
                double sourceAlpha = pixels[i + 3] / 255.0;

                // Treat brightness as the cloud mask:
                // black = transparent, white = fully present.
                double brightness =
                    (0.2126 * red +
                     0.7152 * green +
                     0.0722 * blue) / 255.0;

                byte alpha = (byte)Math.Round(
                    sourceAlpha * brightness * opacity * 255.0);

                pixels[i] = tint.B;
                pixels[i + 1] = tint.G;
                pixels[i + 2] = tint.R;
                pixels[i + 3] = alpha;
            }

            BitmapSource result = BitmapSource.Create(
                width,
                height,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                pixels,
                stride);

            result.Freeze();
            return result;
        }

        private const string DefaultRingTextureUri =
            "pack://application:,,,/Assets/world_globe/saturnringcolor2.jpg";

        private static string GetRingTexturePath(string? texturePath)
        {
            if (!string.IsNullOrWhiteSpace(texturePath) &&
                File.Exists(texturePath))
            {
                return Path.GetFullPath(texturePath);
            }

            return DefaultRingTextureUri;
        }

        private static BitmapSource LoadRingTexture(string? texturePath)
        {
            string resolvedPath = GetRingTexturePath(texturePath);

            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(resolvedPath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }

        private ImageSource? ResolveBodyTexture(CelestialBody body)
        {
            ArgumentNullException.ThrowIfNull(body);

            return ResolveTextureSource(body.TextureSource);
        }

        private BitmapSource? ResolveAttachmentTexture(
            CelestialBodyAttachment attachment)
        {
            ArgumentNullException.ThrowIfNull(attachment);

            return attachment.Type switch
            {
                CelestialBodyAttachmentType.CloudLayer =>
                    _tintedCloudTexture,

                CelestialBodyAttachmentType.PlanetaryRings =>
                    ResolvePlanetaryRingTexture(attachment),

                _ => ResolveTextureSource(attachment.TextureSource) as BitmapSource
            };
        }

        private ImageSource? ResolveTextureSource(CelestialTextureSource? textureSource)
        {
            if (textureSource is null)
                return null;

            return textureSource.SourceType switch
            {
                CelestialTextureSourceType.RealmMap => _worldTextureSource,

                CelestialTextureSourceType.Asset =>
                    LoadCachedTexture(textureSource.AssetPath, isAsset: true),

                CelestialTextureSourceType.BitmapFile =>
                    LoadCachedTexture(textureSource.FilePath, isAsset: false),

                // Procedural textures need a generator registry before they
                // can be resolved here.
                _ => null
            };
        }

        private BitmapSource? LoadCachedTexture(
            string? texturePath,
            bool isAsset)
        {
            if (string.IsNullOrWhiteSpace(texturePath))
                return null;

            string resolvedPath = texturePath.Trim();

            if (isAsset && !Path.IsPathRooted(resolvedPath))
            {
                resolvedPath = WorldGlobeAssets.GetPath(resolvedPath);
            }

            if (!File.Exists(resolvedPath))
                return null;

            resolvedPath = Path.GetFullPath(resolvedPath);

            if (_textureCache.TryGetValue(
                resolvedPath,
                out BitmapSource? cachedTexture))
            {
                return cachedTexture;
            }

            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(resolvedPath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            _textureCache[resolvedPath] = bitmap;

            return bitmap;
        }

        private BitmapSource? ResolvePlanetaryRingTexture(
            CelestialBodyAttachment attachment)
        {
            if (!attachment.Enabled ||
                attachment.AttachmentSettings is not PlanetaryRingSettings settings ||
                !settings.Enabled)
            {
                return null;
            }

            string texturePath = GetRingTexturePath(settings.TexturePath);

            if (_textureCache.TryGetValue(
                    texturePath,
                    out BitmapSource? cachedTexture))
            {
                return cachedTexture;
            }

            try
            {
                BitmapSource texture = LoadRingTexture(texturePath);

                _textureCache[texturePath] = texture;

                return texture;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to load planetary ring texture '{texturePath}': {ex}");

                return null;
            }
        }

        private void EnsurePlanetaryRingsAttachment()
        {
            if (_worldBody is null)
                return;

            _planetaryRingsAttachment = _worldBody.Attachments.FirstOrDefault(
                attachment => attachment.Type == CelestialBodyAttachmentType.PlanetaryRings);

            if (_planetaryRingsAttachment is not null)
                return;

            _planetaryRingsAttachment = new CelestialBodyAttachment
            {
                Name = "Planetary Rings",
                Description = "A textured ring system surrounding the world.",
                Type = CelestialBodyAttachmentType.PlanetaryRings,
                Enabled = false,
                AttachmentSettings = new PlanetaryRingSettings
                {
                    Enabled = false,
                    TexturePath = string.Empty
                }
            };

            _worldBody.Attachments.Add(_planetaryRingsAttachment);
        }

        public void UpdateWorldGlobeRings(
            PlanetaryRingSettings settings,
            CelestialBodyAnimation rotationAnimation,
            CelestialBodyAnimation turbulenceAnimation)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(rotationAnimation);
            ArgumentNullException.ThrowIfNull(turbulenceAnimation);

            EnsurePlanetaryRingsAttachment();

            if (_planetaryRingsAttachment is null)
                return;

            if (rotationAnimation.AnimationType != CelestialBodyAnimationType.BasicRotation)
                throw new ArgumentException(
                    "Expected a BasicRotation animation.",
                    nameof(rotationAnimation));

            if (turbulenceAnimation.AnimationType != CelestialBodyAnimationType.RingTurbulenceActivity)
                throw new ArgumentException(
                    "Expected a RingTurbulenceActivity animation.",
                    nameof(turbulenceAnimation));

            // Keep a separate settings instance for ring geometry and appearance.
            _planetaryRingsAttachment.AttachmentSettings = settings.Clone();
            _planetaryRingsAttachment.Enabled = settings.Enabled;

            // Keep the attachment's animation collection synchronized with the
            // animation objects owned by the view model.
            SyncAttachmentAnimation(rotationAnimation);
            SyncAttachmentAnimation(turbulenceAnimation);

            string requestedTexturePath = GetRingTexturePath(settings.TexturePath);

            if (!_textureCache.ContainsKey(requestedTexturePath))
            {
                _textureCache[requestedTexturePath] =
                    LoadRingTexture(settings.TexturePath);
            }

            _celestialSystemRenderer?.RefreshAttachments();
        }

        private void SyncAttachmentAnimation(CelestialBodyAnimation animation)
        {
            CelestialBodyAnimation? existing =
                _planetaryRingsAttachment!.Animations.FirstOrDefault(
                    item => item.AnimationType == animation.AnimationType);

            if (existing is null)
            {
                _planetaryRingsAttachment.Animations.Add(animation);
            }
            else if (!ReferenceEquals(existing, animation))
            {
                int index = _planetaryRingsAttachment.Animations.IndexOf(existing);
                _planetaryRingsAttachment.Animations[index] = animation;
            }
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

        #region Local Star Methods

        public void ShowLocalStar(CelestialBody star, Point3D worldPosition)
        {
            ArgumentNullException.ThrowIfNull(star);

            if (_celestialSystem is null || _worldNode is null)
                return;

            if (_localStarNode is not null)
            {
                _celestialSystem.Bodies.Remove(_localStarNode);
                _localStarNode = null;
            }

            _celestialSystem.Bodies.Remove(_worldNode);

            _localStarNode = new CelestialBodyNode
            {
                Body = star,
                Center = new Point3D(0, 0, 0)
            };

            _localStarNode.Children.Add(_worldNode);

            UpdateWorldPosition(worldPosition);

            _celestialSystem.Bodies.Add(_localStarNode);

            RebuildCelestialScene();
        }

        public void UpdateLocalStar(CelestialBody star, Point3D worldPosition)
        {
            ArgumentNullException.ThrowIfNull(star);

            if (_localStarNode is null || _worldNode is null)
            {
                ShowLocalStar(star, worldPosition);
                return;
            }

            _localStarNode.Body = star;

            UpdateWorldPosition(worldPosition);

            _celestialSystemRenderer!.UpdateBody(
                star,
                new Point3D(0, 0, 0));

            _celestialSystemRenderer.UpdateBodyPosition(
                _worldNode.Body,
                worldPosition);

            UpdateCameraFacingLocalStar();
        }

        public void RemoveLocalStar()
        {
            if (_celestialSystem is null ||
                _worldNode is null ||
                _localStarNode is null)
            {
                return;
            }

            _localStarNode.Children.Remove(_worldNode);
            _celestialSystem.Bodies.Remove(_localStarNode);

            UpdateWorldPosition(new Point3D(0, 0, 0));

            _celestialSystem.Bodies.Add(_worldNode);

            _localStarNode = null;

            RebuildCelestialScene();
        }

        private void UpdateWorldPosition(Point3D newPosition)
        {
            if (_worldNode is null)
                return;

            Vector3D delta = newPosition - _worldNode.Center;

            // Move the camera by exactly the same amount as the globe.
            ModelViewer.TranslateCamera(delta);

            // Then update the globe's position relative to the star.
            // Its surface, attachments, and atmosphere all inherit this
            // node translation through the World Globe visual hierarchy.
            _worldNode.Center = newPosition;
        }

        private void RebuildCelestialScene()
        {
            if (_celestialSystemRenderer is null ||
                _celestialSystem is null ||
                _worldBody is null)
            {
                return;
            }

            Model3DGroup celestialScene =
                _celestialSystemRenderer.BuildScene(_celestialSystem);

            // BuildScene creates new BodyRendererEntry and VisualRoot
            // instances. Reapply the World Globe's manual orientation
            // transform to the newly-created visual root every time.
            _celestialSystemRenderer.SetBodyVisualTransform(
                _worldBody,
                _worldVisualTransform);

            _globeSpinGroup.Children.Clear();
            _globeSpinGroup.Children.Add(celestialScene);

            _celestialSystemRenderer?.RefreshAttachments();

            // Atmosphere is an additional World Globe visual, rather than
            // a child of the outer tilt group. Restore it after rebuilding.
            if (_atmosphereModel is not null &&
                ViewModel.AtmosphereModel.EnableAtmosphere)
            {
                _celestialSystemRenderer?.AddBodyVisual(
                    _worldBody,
                    _atmosphereModel);
            }

            UpdateCameraFacingLocalStar();
        }

        #endregion

    }
}
