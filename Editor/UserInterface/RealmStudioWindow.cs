using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    public abstract class RealmStudioWindow : Window
    {
        /// <summary>
        /// Unique identifier for this window type.
        /// Used for restoring position, size, etc.
        /// </summary>
        public abstract string WindowId { get; }

        /// <summary>
        /// What kind of RealmStudio window this is.
        /// </summary>
        public abstract RealmWindowType WindowType { get; }

        /// <summary>
        /// Should this window remember its last position?
        /// </summary>
        public virtual bool RememberPosition => true;

        /// <summary>
        /// Should this window remember its last size?
        /// </summary>
        public virtual bool RememberSize => true;

        public bool IsAnimationEnabled { get; set; } = true;

        public virtual WindowAnimationProfile AnimationProfile => WindowAnimationProfiles.None;

        protected RealmStudioWindow()
        {
            Loaded += OnRealmWindowLoaded;
            Closing += OnRealmWindowClosing;
        }

        protected virtual void OnRealmWindowLoaded(
            object? sender,
            RoutedEventArgs e)
        {
        }

        protected virtual void OnRealmWindowClosing(
            object? sender,
            CancelEventArgs e)
        {
        }
    }

    /// <summary>
    /// Defines the behavior and intended usage of a RealmStudio window.
    /// </summary>
    public enum RealmWindowType
    {
        /// <summary>
        /// The application's primary window.
        /// </summary>
        MainWindow,

        /// <summary>
        /// A standard modal dialog that blocks interaction with its owner.
        /// </summary>
        ModalDialog,

        /// <summary>
        /// A modeless dialog that can remain open while editing.
        /// </summary>
        ModelessDialog,

        /// <summary>
        /// A floating utility window that assists the user during editing.
        /// </summary>
        ToolWindow,

        /// <summary>
        /// A floating palette that displays editing controls or resources.
        /// </summary>
        Palette,

        /// <summary>
        /// A compact floating toolbar containing frequently used commands.
        /// </summary>
        FloatingToolbar
    }

    public sealed class WindowAnimationProfile
    {
        public WindowAnimationOptions Show { get; init; } = new();
        public WindowAnimationOptions Hide { get; init; } = new();
    }

    public static class WindowAnimationProfiles
    {
        public static readonly WindowAnimationProfile None =
            new()
            {
                Show = new()
                {
                    Style = WindowAnimationStyle.None
                },

                Hide = new()
                {
                    Style = WindowAnimationStyle.None
                }
            };

        public static readonly WindowAnimationProfile FloatingToolbar =
            new()
            {
                Show = new()
                {
                    Style =
                        WindowAnimationStyle.Fade,

                    Duration =
                        TimeSpan.FromMilliseconds(120),

                    Easing =
                        new QuadraticEase()
                        {
                            EasingMode = EasingMode.EaseOut
                        }
                },

                Hide = new()
                {
                    Style =
                        WindowAnimationStyle.Fade,

                    Duration =
                        TimeSpan.FromMilliseconds(70),

                    Easing =
                        new QuadraticEase()
                        {
                            EasingMode = EasingMode.EaseIn
                        }
                }
            };

        public static readonly WindowAnimationProfile ModalDialog =
            new()
            {
                Show = new()
                {
                    Style =
                        WindowAnimationStyle.Fade |
                        WindowAnimationStyle.Scale,

                    Duration =
                        TimeSpan.FromMilliseconds(180),

                    ScaleFrom = .985,

                    Easing =
                        new CubicEase()
                        {
                            EasingMode = EasingMode.EaseOut
                        }
                },

                Hide = new()
                {
                    Style =
                        WindowAnimationStyle.Fade,

                    Duration =
                        TimeSpan.FromMilliseconds(120),

                    Easing =
                        new CubicEase()
                        {
                            EasingMode = EasingMode.EaseIn
                        }
                }
            };

        public static readonly WindowAnimationProfile ToolWindow =
            new()
            {
                Show = new()
                {
                    Style =
                        WindowAnimationStyle.Fade |
                        WindowAnimationStyle.Slide,

                    Duration =
                        TimeSpan.FromMilliseconds(140),

                    SlideY = 8,

                    Easing =
                        new CubicEase()
                        {
                            EasingMode = EasingMode.EaseOut
                        }
                },

                Hide = new()
                {
                    Style =
                        WindowAnimationStyle.Fade,

                    Duration =
                        TimeSpan.FromMilliseconds(100),

                    Easing =
                        new CubicEase()
                        {
                            EasingMode = EasingMode.EaseIn
                        }
                }
            };
    }
}
