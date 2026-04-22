using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views;
using RealmStudioX.WPF.Views.Controls;
using RealmStudioX.WPF.Views.Panels;
using ShimSkiaSharp;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using SKPaint = SkiaSharp.SKPaint;
using SKPoint = SkiaSharp.SKPoint;
using SKSize = SkiaSharp.SKSize;
using SKTypeface = SkiaSharp.SKTypeface;
using UserControl = System.Windows.Controls.UserControl;

namespace RealmStudioX.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SKGLControl? _skiaControl;
        private EditorController? _editor;
        private FontManager _fontManager;
        private AssetManager _assetManager;

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

        public MainWindow(StartupResult startup)
        {
            InitializeComponent();

            _editor = new EditorController();
            _editor.DrawingModeChanged += OnDrawingModeChanged;
            _editor.ColorPaintBrushChanged += OnColorPaintBrushChanged;
            _editor.ActiveDrawingLayerChanged += OnActiveDrawingLayerChanged;
            _editor.MouseMoved += OnMouseMoved;
            _editor.MouseDown += OnMouseDown;
            _editor.MouseUp += OnMouseUp;
            _editor.MouseDoubleClick += OnMouseDoubleClick;
            _editor.RedrawRequested += () => _skiaControl?.Invalidate();

            _fontManager = new FontManager();

            ViewModel = new MainWindowViewModel(_editor);
            DataContext = ViewModel;

            // create the AssetManager instance
            _assetManager = new();

            AssetManager.RootRealmStudioXDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RealmStudioX");

            Loaded += async (s, e) =>
            {
                await InitializeApplicationAsync();

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
                    _editor.Scene = new MapScene(map,_fontManager);

                    ViewModel.MapName = map.MapName;
                    ViewModel.MapSizeLabel = $"Map Size: {map.MapWidth} x {map.MapHeight}";

                    OnDrawingModeChanged(MapDrawingMode.None);

                    _editor.SetActiveDrawingLayer(MapBuilder.GetMapLayerByIndex(_editor.Scene.Map, MapBuilder.DRAWINGLAYER));
                    ViewModel.ZoomLevelLabel = $"Zoom: {_editor.Scene.Camera.Zoom * 100.0f}%";
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

        private async Task InitializeApplicationAsync()
        {
            await _assetManager.LoadAsync();

            await _fontManager.InitializeAsync(Assembly.GetExecutingAssembly());
        }

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

                _editor.ActiveEditorTool?.OnMouseDown(world, button);

                PointerState state = new()
                {
                    ScreenPoint = screen,
                    WorldPoint = world,
                    Button = button
                };

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

                _editor.ActiveEditorTool?.OnMouseMove(world, button);

                PointerState state = new()
                {
                    ScreenPoint = screen,
                    WorldPoint = world,
                    Button = button
                };

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

                _editor.ActiveEditorTool?.OnMouseDown(world, button);

                PointerState state = new()
                {
                    ScreenPoint = screen,
                    WorldPoint = world,
                    Button = button
                };

                _editor.NotifyMouseDown(state);

                _skiaControl.Invalidate();
            };

            _skiaControl.MouseDoubleClick += (s, e) =>
            {
                if (_editor.Scene == null)
                    return;

                var screen = new SKPoint(e.X, e.Y);
                var world = _editor.ScreenToWorld(screen);

                var button = ConvertButton(e);

                _editor.ActiveEditorTool?.OnMouseDoubleClick(world, button);

                PointerState state = new()
                {
                    ScreenPoint = screen,
                    WorldPoint = world,
                    Button = button
                };

                _editor.NotifyMouseDown(state);

                _skiaControl.Invalidate();
            };

            _skiaControl.Resize += (s, e) =>
            {
                _editor.SetViewportSize(new SKSize(_skiaControl.Width, _skiaControl.Height));
            };
        }


        private void OnPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            using var typeface = SKTypeface.FromFamilyName("Segoe");
            using var font = typeface.ToFont(18);
            using var paint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true
            };

            canvas.DrawText("Drawing is working.", canvas.LocalClipBounds.MidX, canvas.LocalClipBounds.MidY, font, paint);
        }

        //==========================================
        // Mouse Interaction Event Handlers
        //==========================================

        private void OnMouseUp(PointerState state)
        {

        }

        private void OnMouseMoved(PointerState state)
        {
            ViewModel.CursorPointLabel = $"Cursor Point: {state.ScreenPoint.X:F0}, {state.ScreenPoint.Y:F0}";
            ViewModel.DrawingPointLabel = $"Map Point: {state.WorldPoint.X:F0}, {state.WorldPoint.Y:F0}";
        }

        private void OnMouseDown(PointerState state)
        {

        }

        private void OnMouseDoubleClick(PointerState state)
        {

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

        private void OnHScroll(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //_cameraX = (float)e.NewValue;
            //InvalidateCanvas();
        }

        private void OnVScroll(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //_cameraY = (float)e.NewValue;
            //InvalidateCanvas();
        }

        private void OnFitToScreen(object sender, RoutedEventArgs e)
        {

        }

        private void OnResetZoom(object sender, RoutedEventArgs e)
        {

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

        //==========================================
        // Pointer State Struct
        //==========================================
        public struct PointerState
        {
            public SKPoint WorldPoint;
            public SKPoint ScreenPoint;
            public EditorMouseButton Button;
        }
    }
}