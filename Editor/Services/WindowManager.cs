using RealmStudioX.WPF.EditorUtilities;
using System.Windows;
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

        private readonly List<AttachedWindowInfo> _attachedWindows = [];
        private readonly HashSet<Window> _attachedOwners = [];

        /// <summary>
        /// Shows a window.
        /// </summary>
        public void Show(RealmStudioWindow window)
        {
            AttachedWindowInfo? info =
                _attachedWindows.FirstOrDefault(a => a.Window == window);

            if (info != null)
            {
                PositionAttachedWindow(info);
            }

            window.BeginAnimation(Window.OpacityProperty, null);
            window.Opacity = 0;

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
        /// Closes a window.
        /// </summary>
        public void Close(RealmStudioWindow window)
        {
            _animationService.AnimateHide(
                window,
                () => window.Close());
        }

        public T Create<T>() where T : RealmStudioWindow, new()
        {
            T window = new T();

            return window;
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