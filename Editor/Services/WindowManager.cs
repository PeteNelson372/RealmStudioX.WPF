using RealmStudioX.WPF.EditorUtilities;
using System.Windows;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    /// <summary>
    /// Manages the lifetime of RealmStudio windows.
    /// </summary>
    public sealed class WindowManager
    {
        private readonly WindowAnimationService _animationService = new();
        public WindowAnimationService AnimationService => _animationService;

        private readonly Dictionary<Type, RealmStudioWindow> _windows = [];

        private readonly List<AttachedWindowInfo> _attachedWindows = [];
        private readonly HashSet<Window> _attachedOwners = [];

        /// <summary>
        /// Gets all registered windows.
        /// </summary>
        public IReadOnlyCollection<RealmStudioWindow> Windows => _windows.Values;

        /// <summary>
        /// Returns true if a window of the specified type is registered.
        /// </summary>
        public bool IsOpen<T>()
            where T : RealmStudioWindow
        {
            return _windows.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Returns true if a window of the specified type is registered.
        /// </summary>
        public bool IsOpen(Type windowType)
        {
            return _windows.ContainsKey(windowType);
        }

        /// <summary>
        /// Gets a registered window of the specified type.
        /// </summary>
        public T? GetWindow<T>() where T : RealmStudioWindow
        {
            if (_windows.TryGetValue(typeof(T), out RealmStudioWindow? window))
            {
                return (T)window;
            }

            return null;
        }

        /// <summary>
        /// Registers a window with the manager.
        /// </summary>
        public void Register(RealmStudioWindow window)
        {
            Type windowType = window.GetType();

            if (_windows.TryAdd(windowType, window))
            {
                window.Closed += Window_Closed;
            }
        }

        /// <summary>
        /// Removes a window from the manager.
        /// </summary>
        public void Unregister(RealmStudioWindow window)
        {
            Type windowType = window.GetType();

            if (_windows.Remove(windowType))
            {
                window.Closed -= Window_Closed;
            }
        }

        /// <summary>
        /// Shows a window.
        /// </summary>
        public void Show(RealmStudioWindow window)
        {
            Register(window);

            AttachedWindowInfo? info =
                _attachedWindows.FirstOrDefault(a => a.Window == window);

            if (info != null)
            {
                PositionAttachedWindow(info);
            }

            _animationService.PrepareShow(window);

            window.Show();

            window.Dispatcher.BeginInvoke(() =>
            {
                _animationService.AnimateShow(window);

            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Shows a modal dialog.
        /// </summary>
        public bool? ShowDialog(RealmStudioWindow window)
        {
            Register(window);

            _animationService.PrepareShow(window);

            window.Loaded += Window_Loaded;

            return window.ShowDialog();

            void Window_Loaded(object? sender, RoutedEventArgs e)
            {
                window.Loaded -= Window_Loaded;

                _animationService.AnimateShow(window);
            }
        }


        /// <summary>
        /// Hides a window.
        /// </summary>
        public void Hide<T>()
            where T : RealmStudioWindow
        {
            T? window = GetWindow<T>();

            if (window == null)
            {
                return;
            }

            _animationService.AnimateHide(
                window,
                () =>
                {
                    window.Hide();

                    //
                    // Restore the window to its normal state so it is
                    // ready for the next PrepareShow().
                    //

                    window.Opacity = 1.0;

                    if (window.RenderTransform is TransformGroup group)
                    {
                        if (group.Children[0] is ScaleTransform scale)
                        {
                            scale.ScaleX = 1;
                            scale.ScaleY = 1;
                        }

                        if (group.Children[1] is TranslateTransform translate)
                        {
                            translate.X = 0;
                            translate.Y = 0;
                        }
                    }
                });
        }


        /// <summary>
        /// Closes a window.
        /// </summary>
        public void Close<T>()
            where T : RealmStudioWindow
        {
            T? window = GetWindow<T>();

            if (window == null)
            {
                return;
            }

            _animationService.AnimateHide(
                window,
                () => window.Close());
        }

        /// <summary>
        /// Closes all managed windows.
        /// </summary>
        public void CloseAll()
        {
            foreach (RealmStudioWindow window in _windows.Values.ToList())
            {
                window.Close();
            }

            _windows.Clear();
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            if (sender is not RealmStudioWindow window)
            {
                return;
            }

            // Remove any attached window information.
            _attachedWindows.RemoveAll(a => a.Window == window);

            Unregister(window);
        }

        public T GetOrCreate<T>() where T : RealmStudioWindow, new()
        {
            T? window = GetWindow<T>();

            if (window != null)
            {
                return window;
            }

            window = new T();

            Register(window);

            return window;
        }

        public T Toggle<T>() where T : RealmStudioWindow, new()
        {
            T window = GetOrCreate<T>();

            if (IsVisible<T>())
            {
                Hide<T>();
            }
            else
            {
                Show(window);
            }

            return window;
        }

        public bool IsVisible<T>() where T : RealmStudioWindow
        {
            T? window = GetWindow<T>();

            return window != null && window.IsVisible;
        }

        public void AttachToControl(
            RealmStudioWindow window,
            Window owner,
            FrameworkElement anchorControl,
            Point anchorOffset,
            double horizontalOffset = 0,
            double verticalOffset = 0)
        {
            // Already attached? Nothing to do.
            if (_attachedWindows.Any(a => a.Window == window))
            {
                return;
            }

            window.Owner = owner;

            AttachedWindowInfo info = new()
            {
                Window = window,
                Owner = owner,
                AnchorControl = anchorControl,
                AnchorOffset = anchorOffset,
                HorizontalOffset = horizontalOffset,
                VerticalOffset = verticalOffset
            };

            _attachedWindows.Add(info);

            if (_attachedOwners.Add(owner))
            {
                owner.LocationChanged += OwnerWindowChanged;
                owner.SizeChanged += OwnerWindowChanged;
                owner.StateChanged += OwnerWindowChanged;
            }

            anchorControl.LayoutUpdated += AnchorControlLayoutUpdated;

            PositionAttachedWindow(info);
        }

        private void OwnerWindowChanged(object? sender, EventArgs e)
        {
            Window owner = (Window)sender!;

            foreach (AttachedWindowInfo info in _attachedWindows)
            {
                if (info.Owner == owner && info.Window.IsVisible)
                {
                    PositionAttachedWindow(info);
                }
            }
        }

        private static void PositionAttachedWindow(AttachedWindowInfo info)
        {
            UserInterfaceUtilities.PositionWindowRelativeToControl(
                info.Window,
                info.AnchorControl,
                info.AnchorOffset,
                info.HorizontalOffset,
                info.VerticalOffset);
        }
        private void AnchorControlLayoutUpdated(object? sender, EventArgs e)
        {
            foreach (AttachedWindowInfo info in _attachedWindows)
            {
                if (ReferenceEquals(info.AnchorControl, sender) &&
                    info.Window.IsVisible)
                {
                    PositionAttachedWindow(info);
                }
            }
        }


        private sealed class AttachedWindowInfo
        {
            public RealmStudioWindow Window { get; init; } = null!;

            public FrameworkElement AnchorControl { get; init; } = null!;

            public Point AnchorOffset { get; init; }

            public double HorizontalOffset { get; init; }

            public double VerticalOffset { get; init; }

            public Window Owner { get; init; } = null!;
        }
    }
}