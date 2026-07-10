using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.Editor.Services;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.Models.Startup;
using RealmStudioX.WPF.ViewModels.Controls;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views;
using RealmStudioX.WPF.Views.Controls;
using RealmStudioX.WPF.Views.Dialogs;
using RealmStudioX.WPF.Views.Panels;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
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
    public partial class MainWindow : RealmStudioMainWindow
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        private SKGLControl? _skiaControl;
        public SKGLControl? SkiaControl => _skiaControl;

        private readonly EditorController? _editor;
        private readonly FontManager _fontManager;
        
        private readonly AssetManager _assetManager;
        private readonly RenderContext _renderContext;

        private readonly InputRouter _inputRouter;

        public MainWindowViewModel ViewModel { get; }

        public event EventHandler? OpenClicked;
        public event EventHandler? SaveClicked;
        public event EventHandler? MinimizeClicked;
        public event EventHandler? MaximizeClicked;
        public event EventHandler? ExitClicked;

        private readonly Dictionary<string, UserControl> _toolPanels = new()
        {
            ["Project"] = new ProjectToolPanel(),
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

        public MainWindow(CreateOpenPackageResult startup, AssetManager assetManager, FontManager fontManager)
        {
            InitializeComponent();

            _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));

            _fontManager = fontManager ?? throw new ArgumentNullException(nameof(fontManager));

            _editor = new EditorController(_assetManager, _fontManager);

            _editor.ActiveDrawingLayerChanged += OnActiveDrawingLayerChanged;
            _editor.MouseMoved += OnMouseMoved;
            _editor.MouseDown += OnMouseDown;
            _editor.MouseUp += OnMouseUp;
            _editor.MouseDoubleClick += OnMouseDoubleClick;
            _editor.RedrawRequested += () => _skiaControl?.Invalidate();
            _editor.MapSceneChanged += UpdateMapScene;

            _inputRouter = new(_editor);

            _renderContext = new RenderContext(_assetManager.SymbolImageCache, _editor.State);

            ViewModel = new MainWindowViewModel(_editor, _assetManager, _fontManager);
            DataContext = ViewModel;

            ViewModel.RenderContext = _renderContext;

            Loaded += async (s, e) =>
            {
                TitleBar.DataContext = ViewModel;

                TitleBar.OpenClicked += (s, e) => OpenHandler();
                TitleBar.SaveClicked += (s, e) => SaveHandler();
                TitleBar.MinimizeClicked += (s, e) => MinimizeHandler();
                TitleBar.MaximizeClicked += (s, e) => MaximizeHandler();
                TitleBar.ExitClicked += (s, e) => ExitHandler();
                TitleBar.UpdateAvailableClicked += (s, e) => NewVersionHandler();

                MainTabs.TabSelectionChanged += (s, e) => MainTabControl_SelectionChanged(s, e);

                ViewModel.RequestOpenNameGeneratorConfig += OpenNameGeneratorConfigDialog;

                ViewModel.RecoveryService.AutosaveCompleted += AutosaveService_AutosaveCompleted;

                // force an update to the MainTabs selection to ensure the correct tool panel is displayed on startup
                MainTabs.SelectTab("Background");
                MainTabs.SelectTab("Ocean");
                MainTabs.SelectTab("Background");

                // if the user chooses to create a new project,
                // OpenMapProject will forward the startup result
                // to CreateMapProject
                ViewModel.OpenRealmProject(startup);

                _editor.State.StatusMessage = $"Loaded {_assetManager.AssetCount} assets.";

                _editor.State.DrawingModeChanged += OnDrawingModeChanged;
            };

            InitializeSkiaControl();
        }

        private void AutosaveService_AutosaveCompleted(object? sender, EventArgs e)
        {
            RealmStudioXLogger.Info($"Autosave completed at {DateTime.Now}");
            _editor!.State.StatusMessage = $"Autosave completed.";
        }

        private void OnDrawingModeChanged(MapDrawingMode previous, MapDrawingMode current)
        {
            // this handles editorState.DrawingModeChanged event
            ViewModel.DrawingModeLabel = ViewModel.SetDrawingModeLabel();
        }

        private void OpenNameGeneratorConfigDialog()
        {
            WindowManager wm = MainWindowViewModel.WindowManager;

            NameGenConfig dialog = wm.Create<NameGenConfig>();

            dialog.Owner = this;
            dialog.DataContext = ViewModel.NameGenConfigViewModel;

            var result = dialog.ShowDialog();
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

        private void OnActiveDrawingLayerChanged(MapLayer layer)
        {
            ViewModel.SetDrawingLayerLabel();
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

                HandleMouseWheel(state);

                // let the editor controller be able to handle the MouseWheel event
                // or pass it on to the active tool
                _editor.OnMouseWheel(state);

                _skiaControl.Invalidate();
            };

            _skiaControl.MouseEnter += (s, e) =>
            {
                Cursor = System.Windows.Input.Cursors.None;
                _skiaControl.Cursor = System.Windows.Forms.Cursors.Default;

                if (_editor.CurrentDrawingMode == MapDrawingMode.SymbolPlace
                || _editor.CurrentDrawingMode == MapDrawingMode.SymbolColor)
                {
                    if (!ViewModel.SymbolsViewModel.UseAreaBrush)
                    {
                        _skiaControl.Cursor = System.Windows.Forms.Cursors.Cross;
                    }
                }
            };

            _skiaControl.MouseLeave += (s, e) =>
            {
                Cursor = System.Windows.Input.Cursors.Arrow;
                _skiaControl.Cursor = System.Windows.Forms.Cursors.Default;
            };

            
            _skiaControl.KeyDown += (s, e) =>
            {
                Keyboard.ClearFocus();
                _skiaControl!.Select();
                _skiaControl.Focus();

                _inputRouter.HandleKeyDown(KeyInterop.KeyFromVirtualKey((int)e.KeyCode),
                    KeyHandler.GetModifiers());

                e.Handled = true;
            };

            _skiaControl.KeyUp += (s, e) =>
            {
                _inputRouter.HandleKeyUp(KeyInterop.KeyFromVirtualKey((int)e.KeyCode),
                    KeyHandler.GetModifiers());
                e.Handled = true;
            };

            _skiaControl.KeyPress += (s, e) =>
            {
                // if the active tool wants to handle key press, send the key to it
                if (_editor.ActiveEditorTool is IKeyHandler keyHandler)
                {
                    _inputRouter.HandleTextInput(e.KeyChar);
                }
            };
            
        }

        private void HandleMouseWheel(PointerState state)
        {
            if (_editor == null)
            {
                return;
            }

            const int cursorDelta = 5;
            int sizeDelta = state.WheelDelta < 0 ? -cursorDelta : cursorDelta;

            if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.LandErase)
            {
                ViewModel.LandformViewModel.LandformEraserSize += sizeDelta;
            }
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.LandPaint)
            {
                ViewModel.LandformViewModel.LandformBrushSize += sizeDelta;
            }
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.LandColor)
            {
                //ViewModel.LandformViewModel.LandPaintBrushSize += sizeDelta;
            }
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.LandColorErase)
            {
                //ViewModel.LandformViewModel.LandPaintEraserSize += sizeDelta;
            }
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.OceanErase)
            {
                ViewModel.DrawingViewModel.MouseWheelLineBrushSizeChanged(sizeDelta);
            }
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.WaterPaint)
            {
                ViewModel.WaterViewModel.WaterBrushSize += sizeDelta;
            }            
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.WaterErase)
            {
                ViewModel.WaterViewModel.WaterEraserSize += sizeDelta;
            }            
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.LakePaint)
            {
                ViewModel.WaterViewModel.WaterBrushSize += sizeDelta;
            }
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.WaterColor)
            {
                //ViewModel.WaterViewModel.WaterColorBrushSize += sizeDelta;
            }
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.WaterColorErase)
            {
                //ViewModel.WaterViewModel.WaterColorEraserSize += sizeDelta;
            }
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.MapHeightIncrease)
            {
                //MainMediator.SelectedBrushSize = RealmMapMethods.GetNewBrushSize(LandBrushSizeTrack, sizeDelta);
            }
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.MapHeightDecrease)
            {
                //MainMediator.SelectedBrushSize = RealmMapMethods.GetNewBrushSize(LandBrushSizeTrack, sizeDelta);
            }
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.DrawingLine)
            {
                ViewModel.DrawingViewModel.MouseWheelLineBrushSizeChanged(sizeDelta);
            }
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.DrawingPaint)
            {
                ViewModel.DrawingViewModel.MouseWheelLineBrushSizeChanged(sizeDelta);
            }
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.DrawingPolygon)
            {
                ViewModel.DrawingViewModel.MouseWheelLineBrushSizeChanged(sizeDelta);
            }
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.DrawingEllipse)
            {
                ViewModel.DrawingViewModel.MouseWheelLineBrushSizeChanged(sizeDelta);
            }
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.DrawingRectangle)
            {
                ViewModel.DrawingViewModel.MouseWheelLineBrushSizeChanged(sizeDelta);
            }
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.DrawingErase)
            {
                ViewModel.DrawingViewModel.MouseWheelLineBrushSizeChanged(sizeDelta);
            }
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.DrawingStamp)
            {
                ViewModel.DrawingViewModel.StampScale += sizeDelta / 100f;
            }            
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.SymbolPlace)
            {
                if (ViewModel.SymbolsViewModel.UseAreaBrush)
                {
                    // area brush size is changed when UseAreaBrush is true
                    ViewModel.SymbolsViewModel.AreaBrushSize += sizeDelta;
                }
                else
                {
                    ViewModel.SymbolsViewModel.SymbolScale += (sizeDelta * 0.01);
                }
            }
            else if (state.Modifiers == InputModifiers.None && _editor.CurrentDrawingMode == MapDrawingMode.SymbolErase)
            {
                // when erasing symbols, the area brush size is changed using the mouse wheel,
                // and the area brush size determines the size of the area in which symbols will be erased
                ViewModel.SymbolsViewModel.AreaBrushSize += sizeDelta;
            }
            else if (state.Modifiers == InputModifiers.Shift && _editor.CurrentDrawingMode == MapDrawingMode.SymbolPlace)
            {
                ViewModel.SymbolsViewModel.SymbolRotation += sizeDelta;
            }
            else if (state.Modifiers == InputModifiers.Control)
            {
                if (_editor.Scene == null)
                {
                    return;
                }

                const float zoomStep = 1.1f;

                float factor = state.WheelDelta > 0 ? zoomStep : 1f / zoomStep;

                _editor.Scene.Camera.SetZoom(_editor.Scene.Camera.Zoom * factor, _skiaControl!.Width, _skiaControl!.Height);

                _editor.Scene.Camera.ZoomAtScreenPoint(
                    _editor.Scene.Camera.Zoom * factor,
                    new SKPoint(state.ScreenPoint.X, state.ScreenPoint.Y), _skiaControl!.Width, _skiaControl!.Height);
            }

        }

        private void OnPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(_skiaControl);
            ArgumentNullException.ThrowIfNull(e.Surface);

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.WhiteSmoke);

            if (_editor != null && _editor.Scene != null && _editor.Scene.Map != null && _renderContext != null)
            {
                // paint the _skiaControl surface, compositing all of the layers
                using (new SKAutoCanvasRestore(canvas))
                {
                    canvas.ResetMatrix();
                    canvas.Translate(_editor.Scene.Camera.Pan.X, _editor.Scene.Camera.Pan.Y);
                    canvas.Scale(_editor.Scene.Camera.Zoom);

                    if (_skiaControl.GRContext != null
                        && _editor.Scene.Map != null)
                    {
                        _renderContext.Zoom = _editor.Scene.Camera.Zoom;
                        _editor.Scene.Render(canvas);

                        _editor.RenderOverlay(canvas);

                        // TODO: handle rendering height map

                        canvas.DrawRect(new SKRect(0, 0, _editor.Scene.Map!.MapWidth, _editor.Scene.Map.MapHeight), PaintObjects.MapBoundaryPaint);
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
        // Main Menu Commands
        //==========================================

        private void OpenHandler()
        {
            // open a project
            ViewModel.ShowCreateOpenDialog();

        }

        private void SaveHandler()
        {
            // save the current project
            ViewModel.SaveRealmProject();
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            if (!ViewModel.TryShutdown())
            {
                e.Cancel = true;
                return;
            }

            base.OnClosing(e);
        }

        private void ExitHandler()
        {
            Close();
        }

        private static void NewVersionHandler()
        {
            // TODO: check for a new version
            MessageDialog dlg = MessageDialogFactory.InformationDialog("Update Available", "New version available. Please update to the latest version.");
            dlg.ShowDialog();
        }

        //==========================================
        // Tab Selection Handler
        //==========================================
        private void MainTabControl_SelectionChanged(object? s, EventArgs e)
        {
            ShowToolPanel(((TabItem)MainTabs.MainTabControl.SelectedItem).Header.ToString());

            ViewModel.PaintService?.Reset();
        }

        private void ShowToolPanel(string? tab)
        {
            if (string.IsNullOrEmpty(tab))
            {
                SecondaryPanelHost.Content = null;
                return;
            }

            if (_toolPanels[tab] is ProjectToolPanel prtp)
            {
                prtp.DataContext = ViewModel.ProjectViewModel;
            }

            if (_toolPanels[tab] is OceanToolPanel oceantoolpanel)
            {
                oceantoolpanel.DataContext = ViewModel.OceanViewModel;
            }

            if (_toolPanels[tab] is LandToolPanel landtoolpanel)
            {
                landtoolpanel.DataContext = ViewModel.LandformViewModel;
            }

            if (_toolPanels[tab] is WaterToolPanel watertoolpanel)
            {
                watertoolpanel.DataContext = ViewModel.WaterViewModel;
            }

            if (_toolPanels[tab] is PathsToolPanel ptp)
            {
                ptp.DataContext = ViewModel.PathViewModel;
            }

            if (_toolPanels[tab] is SymbolsToolPanel stp)
            {
                stp.DataContext = ViewModel.SymbolsViewModel;
            }

            if (_toolPanels[tab] is LabelsToolPanel ltp)
            {
                ltp.DataContext = ViewModel.LabelsViewModel;
            }

            if (_toolPanels[tab] is OverlaysToolPanel otp)
            {
                otp.DataContext = ViewModel.OverlaysViewModel;
            }

            if (_toolPanels[tab] is RegionsToolPanel rtp)
            {
                rtp.DataContext = ViewModel.RegionViewModel;
            }

            if (_toolPanels[tab] is DrawingToolPanel dtp)
            {
                dtp.DataContext = ViewModel.DrawingViewModel;
            }

            SecondaryPanelHost.Content = _toolPanels[tab];
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_editor == null)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Up:
                case Key.Down:
                case Key.Left:
                case Key.Right:
                case Key.Home:
                case Key.End:

                    _inputRouter.HandleKeyDown(e.Key, Keyboard.Modifiers);

                    e.Handled = true;
                    return;
            }
        }

        private void OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            _editor?.CommitSymbolNudge();
            _editor?.CommitLabelNudge();
            e.Handled = true;
        }

        private void OnTextInput(object sender, TextCompositionEventArgs e)
        {
            if (_editor == null)
            {
                return;
            }

            if (_editor.ActiveEditorTool is IKeyHandler)
            {
                if (!string.IsNullOrEmpty(e.Text))
                {
                    _inputRouter.HandleTextInput(e.Text[0]);
                }
            }
        }
    }
}