using RealmStudioX.WPF.Views;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace RealmStudioX.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SKGLControl? _skiaControl;

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

            Loaded += (s, e) =>
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

                MainTabs.SelectTab("Background");
                MainTabs.SelectTab("Ocean");
                MainTabs.SelectTab("Background");

                if (startup.IsNew)
                {
                    // Create new map
                    // e.g. _controller.CreateMap(startup.Width, startup.Height, startup.Theme);
                }
                else
                {
                    // Load existing map
                    // e.g. _controller.LoadMap(startup.FilePath);
                }
            };

            InitializeSkiaControl();
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
        // Scrollbars
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

        private void NewVersionHandler()
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