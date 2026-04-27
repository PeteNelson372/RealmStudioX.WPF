using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views;
using RealmStudioX.WPF.Views.Controls;
using RealmStudioX.WPF.Views.Panels;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using SKPoint = SkiaSharp.SKPoint;
using SKRect = SkiaSharp.SKRect;
using SKSize = SkiaSharp.SKSize;
using UserControl = System.Windows.Controls.UserControl;

namespace RealmStudioX.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SKGLControl? _skiaControl;
        private readonly EditorController? _editor;
        private readonly FontManager _fontManager;
        private readonly AssetManager _assetManager;
        private readonly RenderContext _renderContext;

        public MainWindowViewModel ViewModel { get; }

        public event EventHandler? OpenClicked;
        public event EventHandler? SaveClicked;
        public event EventHandler? MinimizeClicked;
        public event EventHandler? MaximizeClicked;
        public event EventHandler? ExitClicked;

        private readonly Dictionary<string, UserControl> _toolPanels = new()
        {
            ["Background"] = new BackgroundToolPanel(),
            ["Ocean"] = new OceanToolPanel(),
            ["Land"] = new LandToolPanel(),
            ["Water"] = new WaterToolPanel(),
            ["Paths"] = new PathsToolPanel(),
            ["Symbols"] = new SymbolsToolPanel(),
            ["Labels"] = new LabelsToolPanel(),
            ["Overlays"] = new OverlaysToolPanel(),
            ["Regions"] = new RegionsToolPanel(),
            ["Drawing"] = new DrawingToolPanel(),
            ["Interior"] = new InteriorToolPanel(),
            ["Dungeon"] = new DungeonToolPanel(),
            ["Ship"] = new ShipToolPanel(),
            ["Planet"] = new PlanetToolPanel()
        };

        public MainWindow(StartupResult startup, AssetManager assetManager)
        {
            InitializeComponent();

            // create the AssetManager instance
            _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));

            _editor = new EditorController(_assetManager);
            _editor.DrawingModeChanged += OnDrawingModeChanged;
            _editor.ColorPaintBrushChanged += OnColorPaintBrushChanged;
            _editor.ActiveDrawingLayerChanged += OnActiveDrawingLayerChanged;
            _editor.MouseMoved += OnMouseMoved;
            _editor.MouseDown += OnMouseDown;
            _editor.MouseUp += OnMouseUp;
            _editor.MouseDoubleClick += OnMouseDoubleClick;
            _editor.RedrawRequested += () => _skiaControl?.Invalidate();
            _editor.MapSceneChanged += UpdateMapScene;

            _fontManager = new FontManager();

            _renderContext = new RenderContext(_assetManager.SymbolImageCache);

            ViewModel = new MainWindowViewModel(_editor, _assetManager);
            DataContext = ViewModel;

            Loaded += async (s, e) =>
            {
                TitleBar.OpenClicked += (s, e) => OpenHandler();
                TitleBar.SaveClicked += (s, e) => SaveHandler();
                TitleBar.MinimizeClicked += (s, e) => MinimizeHandler();
                TitleBar.MaximizeClicked += (s, e) => MaximizeHandler();
                TitleBar.ExitClicked += (s, e) => ExitHandler();
                TitleBar.UpdateAvailableClicked += (s, e) => NewVersionHandler();

                MainMenu.NewClicked += (s, e) => NewHandler();
                MainMenu.OpenClicked += (s, e) => OpenHandler();
                MainMenu.SaveClicked += (s, e) => SaveHandler();
                MainMenu.ExitClicked += (s, e) => ExitHandler();
                MainMenu.UndoClicked += (s, e) => UndoHandler();
                MainMenu.RedoClicked += (s, e) => RedoHandler();

                MainTabs.TabSelectionChanged += (s, e) => MainTabControl_SelectionChanged(s, e);

                // force an update to the MainTabs selection to ensure the correct tool panel is displayed on startup
                MainTabs.SelectTab("Background");
                MainTabs.SelectTab("Ocean");
                MainTabs.SelectTab("Background");

                if (startup.IsNew)
                {
                    // Create new map
                    if (string.IsNullOrEmpty(startup.MapName))
                    {
                        startup.MapName = "Default";
                    }

                    if (string.IsNullOrEmpty(startup.FilePath))
                    {
                        startup.FilePath = string.Empty;
                    }

                    RealmStudioMap map = MapBuilder.CreateMap(startup.FilePath, startup.MapName, startup.Width, startup.Height);
                    MapScene newScene = new(map, _fontManager)
                    {
                        RenderContext = _renderContext
                    };

                    newScene.Camera.Viewport = new SKRect(0, 0, _skiaControl!.Width, _skiaControl.Height);
                    _editor.SetScene(newScene);

                    ViewModel.AttachScene(newScene);

                    ViewModel.MapName = map.MapName;
                    ViewModel.MapSizeLabel = $"Map Size: {map.MapWidth} x {map.MapHeight}";

                    OnDrawingModeChanged(MapDrawingMode.None);

                    _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.DRAWINGLAYER));
                }
                else
                {
                    // Load existing map
                    // e.g. _controller.LoadMap(startup.FilePath);
                }

                ViewModel.ApplicationStatusMessage = $"Loaded {_assetManager.AssetCount} assets.";
            };

            InitializeSkiaControl();
        }

        private void UpdateMapScene()
        {
            if (_editor == null || _editor.Scene == null)
                return;

            ViewModel.Zoom = _editor.Scene.Camera.Zoom;
            ViewModel.UpdateZoomLabel(_editor.Scene.Camera.Zoom);
        }

        private void OnViewportSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ViewModel == null || _skiaControl == null)
                return;

            ViewModel.SetViewPortSize(
                new SKRect(0, 0, (float)_skiaControl.Width, (float)_skiaControl.Height));
        }

        private void OnDrawingModeChanged(MapDrawingMode mode)
        {
            ViewModel.DrawingModeLabel = ViewModel.SetDrawingModeLabel();
        }

        private void OnActiveDrawingLayerChanged(MapLayer layer)
        {
            ViewModel.SetDrawingLayerLabel();
        }

        private void OnColorPaintBrushChanged(ColorPaintBrush brush)
        {
            ViewModel.DrawingModeLabel = ViewModel.SetDrawingModeLabel();
        }

        private void OnOpen(object sender, RoutedEventArgs e)
            => OpenClicked?.Invoke(this, EventArgs.Empty);

        private void OnSave(object sender, RoutedEventArgs e)
            => SaveClicked?.Invoke(this, EventArgs.Empty);

        private void OnExit(object sender, RoutedEventArgs e)
            => ExitClicked?.Invoke(this, EventArgs.Empty);

        private void OnMinimize(object sender, RoutedEventArgs e)
            => MinimizeClicked?.Invoke(this, EventArgs.Empty);  

        private void OnMaximize(object sender, RoutedEventArgs e)
            => MaximizeClicked?.Invoke(this, EventArgs.Empty);


        //==========================================
        // SKGLControl
        //==========================================

        private void InitializeSkiaControl()
        {
            _skiaControl = new SKGLControl();
            _skiaControl.PaintSurface += OnPaintSurface;
            FormsHost.Child = _skiaControl;

            WireSkiaInput();

            StartRenderLoop();

            ViewModel.SetViewPortSize(
                new SKRect(0, 0, (float)_skiaControl.Width, (float)_skiaControl.Height));

            _editor?.Scene?.Camera.SetZoom(1.0f, _skiaControl.Width, _skiaControl.Height);
        }

        private void StartRenderLoop()
        {
            CompositionTarget.Rendering += (s, e) =>
            {
                // Guard the Windows-only call with a runtime OS check to satisfy CA1416.
                if (OperatingSystem.IsWindows())
                {
                    _skiaControl?.Invalidate();
                }
            };
        }

        
        private void WireSkiaInput()
        {
            ArgumentNullException.ThrowIfNull(_skiaControl, nameof(_skiaControl));
            ArgumentNullException.ThrowIfNull(_editor, nameof(_editor));

            // Mouse buttons
            _skiaControl.MouseDown += (s, e) =>
            {
                if (_editor.Scene == null)
                    return;

                var screen = new SKPoint(e.X, e.Y);

                var world = _editor.ScreenToWorld(screen);

                var button = ConvertButton(e);

                PointerState state = new()
                {
                    ScreenPoint = screen,
                    WorldPoint = world,
                    IsDoubleClick = false,
                    IsMouseWheelScrolled = false,
                    Button = button,
                    WheelDelta = 0,
                    Modifiers = ConvertModifiers(Keyboard.Modifiers)
                };

                // notify the editor that the mouse button has been pressed, so it can route the event
                _editor.OnMouseDown(state);

                // notify the UI that the mouse button has been pressed
                _editor.NotifyMouseDown(state);

                _skiaControl.Invalidate();
            };

            _skiaControl.MouseMove += (s, e) =>
            {
                if (_editor.Scene == null)
                    return;

                var screen = new SKPoint(e.X, e.Y);

                var world = _editor.ScreenToWorld(screen);

                var button = ConvertButton(e);

                PointerState state = new()
                {
                    ScreenPoint = screen,
                    WorldPoint = world,
                    IsDoubleClick = false,
                    IsMouseWheelScrolled = false,
                    Button = button,
                    WheelDelta = 0,
                    Modifiers = ConvertModifiers(Keyboard.Modifiers)
                };

                // notify the editor that the mouse has moved, so it can route the event
                // to the active tool or handle it itself (e.g. for panning the camera)
                _editor.OnMouseMove(state);

                // notify the UI that the mouse has moved
                _editor.NotifyMouseMoved(state);

                _skiaControl.Invalidate();
            };

            _skiaControl.MouseUp += (s, e) =>
            {
                if (_editor.Scene == null)
                    return;

                var screen = new SKPoint(e.X, e.Y);

                var world = _editor.ScreenToWorld(screen);

                var button = ConvertButton(e);

                PointerState state = new()
                {
                    ScreenPoint = screen,
                    WorldPoint = world,
                    IsDoubleClick = false,
                    IsMouseWheelScrolled = false,
                    Button = button,
                    WheelDelta = 0,
                    Modifiers = ConvertModifiers(Keyboard.Modifiers)
                };

                // notify the editor that the mouse button has been released, so it can route the event
                // to the active tool or handle it itself
                _editor.OnMouseUp(state);

                // notify the UI that the mouse button has been released
                _editor.NotifyMouseUp(state);

                _skiaControl.Invalidate();
            };

            _skiaControl.MouseDoubleClick += (s, e) =>
            {
                if (_editor.Scene == null)
                    return;

                var screen = new SKPoint(e.X, e.Y);
                var world = _editor.ScreenToWorld(screen);

                var button = ConvertButton(e);

                PointerState state = new()
                {
                    ScreenPoint = screen,
                    WorldPoint = world,
                    IsDoubleClick = true,
                    IsMouseWheelScrolled = false,
                    Button = button,
                    WheelDelta = 0,
                    Modifiers = ConvertModifiers(Keyboard.Modifiers)
                };

                // notify the editor that the mouse button has been double-clicked, so it can route the event
                _editor.OnMouseDoubleClick(state);

                // notify the UI that the mouse button has been double-clicked
                _editor.NotifyMouseDoubleClick(state);

                _skiaControl.Invalidate();
            };

            _skiaControl.Resize += (s, e) =>
            {
                _editor.SetViewportSize(new SKSize(_skiaControl.Width, _skiaControl.Height));
            };

            _skiaControl.MouseWheel += (s, e) =>
            {
                if (_editor.Scene == null)
                    return;
                var screen = new SKPoint(e.X, e.Y);
                var world = _editor.ScreenToWorld(screen);

                PointerState state = new()
                {
                    ScreenPoint = screen,
                    WorldPoint = world,
                    IsDoubleClick = false,
                    IsMouseWheelScrolled = true,
                    Button = EditorMouseButton.None,
                    WheelDelta = e.Delta,
                    Modifiers = ConvertModifiers(Keyboard.Modifiers)
                };

                _editor.OnMouseWheel(state);
                
                _skiaControl.Invalidate();
            };
        }


        private void OnPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(_skiaControl);
            ArgumentNullException.ThrowIfNull(e.Surface);

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            if (_editor != null && _editor.Scene != null && _editor.Scene.Map != null && _renderContext != null)
            {
                // paint the _skiaControl surface, compositing all of the layers

                using (new SKAutoCanvasRestore(canvas))
                {
                    canvas.ResetMatrix();
                    canvas.Translate(_editor.Scene.Camera.Pan.X, _editor.Scene.Camera.Pan.Y);
                    canvas.Scale(_editor.Scene.Camera.Zoom);

                    canvas.DrawRect(new SKRect(0, 0, _editor.Scene.Map.MapWidth, _editor.Scene.Map.MapHeight), PaintObjects.MapOutlinePaint);

                    if (_skiaControl.GRContext != null
                        && _editor.Scene.Map != null
                        && _editor.Scene.Map.MapLayers.Count == MapBuilder.MAP_LAYER_COUNT)
                    {
                        _renderContext.Zoom = _editor.Scene.Camera.Zoom;
                        _editor.Scene.Render(canvas);

                        _editor.RenderOverlay(canvas);

                        // TODO: handle rendering height map
                    }
                }
            }
        }

        private static InputModifiers ConvertModifiers(System.Windows.Input.ModifierKeys keys)
        {
            InputModifiers result = InputModifiers.None;

            if (keys.HasFlag(System.Windows.Input.ModifierKeys.Control))
                result |= InputModifiers.Control;

            if (keys.HasFlag(System.Windows.Input.ModifierKeys.Shift))
                result |= InputModifiers.Shift;

            if (keys.HasFlag(System.Windows.Input.ModifierKeys.Alt))
                result |= InputModifiers.Alt;

            return result;
        }

        //==========================================
        // Mouse Interaction Event Handlers
        //==========================================

        private void OnMouseUp(PointerState state)
        {
            // nothing to do here now
        }

        private void OnMouseMoved(PointerState state)
        {
            // update the status bar with the current cursor position in screen coordinates and map coordinates
            ViewModel.CursorPointLabel = $"Cursor Point: {state.ScreenPoint.X:F0}, {state.ScreenPoint.Y:F0}";
            ViewModel.DrawingPointLabel = $"Map Point: {state.WorldPoint.X:F0}, {state.WorldPoint.Y:F0}";
        }

        private void OnMouseDown(PointerState state)
        {
            // nothing to do here now
        }

        private void OnMouseDoubleClick(PointerState state)
        {
            // nothing to do here now
        }

        private static EditorMouseButton ConvertButton(System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
                return EditorMouseButton.Left;

            if (e.Button == System.Windows.Forms.MouseButtons.Right)
                return EditorMouseButton.Right;

            if (e.Button == System.Windows.Forms.MouseButtons.Middle)
                return EditorMouseButton.Middle;

            return EditorMouseButton.None;
        }

        private EditorMouseButton ConvertButton(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                return EditorMouseButton.Left;

            if (e.RightButton == MouseButtonState.Pressed)
                return EditorMouseButton.Right;

            if (e.MiddleButton == MouseButtonState.Pressed)
                return EditorMouseButton.Middle;

            return EditorMouseButton.None;
        }

        private EditorMouseButton ConvertButton(MouseButtonEventArgs e)
        {
            return e.ChangedButton switch
            {
                MouseButton.Left => EditorMouseButton.Left,
                MouseButton.Right => EditorMouseButton.Right,
                MouseButton.Middle => EditorMouseButton.Middle,
                _ => EditorMouseButton.None
            };
        }


        //==========================================
        // Scrollbars and Zoom Event Handlers
        //==========================================

        private void OnHScrollChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ViewModel == null) return;

            ViewModel.ScrollX = e.NewValue;
        }

        private void OnVScrollChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ViewModel == null) return;

            ViewModel.ScrollY = e.NewValue;
        }

        private void OnFitToScreen(object sender, RoutedEventArgs e)
        {
            if (_editor == null || _editor.Scene == null)
                return;

            _editor.Scene.Camera.ZoomToFit(_editor.Scene.Map.MapWidth, _editor.Scene.Map.MapHeight);
        }

        private void OnResetZoom(object sender, RoutedEventArgs e)
        {
            if (_editor == null || _editor.Scene == null)
                return;

            _editor.Scene.Camera.Reset(_skiaControl!.Width, _skiaControl.Height);
        }

        //==========================================
        // Title BarEvent Handlers
        //==========================================

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

        //==========================================
        // Main Menu Event Handlers
        //==========================================

        private void NewHandler()
        {
            // create a new map
        }

        private void OpenHandler()
        {
            // open a map
        }

        private void SaveHandler()
        {
            // save the current map
        }

        private void ExitHandler()
        {
            Close();
        }

        private void UndoHandler()
        {
            // undo the last action
        }

        private void RedoHandler()
        {
            // redo the last undone action
        }

        private static void NewVersionHandler()
        {
            MessageBox.Show("New version available! Please update to the latest version.", "Update Available", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        //==========================================
        // Tab Selection Handler
        //==========================================
        private void MainTabControl_SelectionChanged(object? s, EventArgs e)
        {
            Debug.WriteLine("Tab selection changed to " + ((TabItem)MainTabs.MainTabControl.SelectedItem).Header);

            ShowToolPanel(((TabItem)MainTabs.MainTabControl.SelectedItem).Header.ToString());
        }

        private void ShowToolPanel(string? tab)
        {
            if (string.IsNullOrEmpty(tab))
            {
                SecondaryPanelHost.Content = null;
                return;
            }

            SecondaryPanelHost.Content = _toolPanels[tab];
        }
    }
}