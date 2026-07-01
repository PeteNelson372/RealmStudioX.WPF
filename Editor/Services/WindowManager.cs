using System.Windows;
using System.Windows.Media;

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
            if (sender is RealmStudioWindow window)
            {
                Unregister(window);
            }
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
    }

}