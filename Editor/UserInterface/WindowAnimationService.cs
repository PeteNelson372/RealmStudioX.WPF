using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    public class WindowAnimationService
    {
        public void AnimateShow(RealmStudioWindow window)
        {
            if (!window.IsAnimationEnabled)
                return;

            WindowAnimationOptions options =
                window.AnimationProfile.Show;

            if (options.Style == WindowAnimationStyle.None)
                return;

            Storyboard storyboard =
                BuildStoryboard(window, options, true);

            storyboard.Begin(window,
                HandoffBehavior.SnapshotAndReplace,
                true);
        }

        public void AnimateHide(RealmStudioWindow window, Action completed)
        {
            if (!window.IsAnimationEnabled)
            {
                return;
            }

            WindowAnimationOptions options = window.AnimationProfile.Hide;

            if (options.Style == WindowAnimationStyle.None)
            {
                completed();
                return;
            }

            PrepareTransforms(window, options, false);

            Storyboard storyboard = BuildStoryboard(window, options, false);

            storyboard.Completed += (_, _) =>
            {
                completed();
            };

            storyboard.Begin(window, HandoffBehavior.SnapshotAndReplace, true);
        }

        public void PrepareShow(RealmStudioWindow window)
        {
            PrepareTransforms(
                window,
                window.AnimationProfile.Show,
                true);
        }

        private static void PrepareTransforms(RealmStudioWindow window, WindowAnimationOptions options, bool showing)
        {
            window.RenderTransformOrigin = new Point(0.5, 0.5);

            TransformGroup group;

            if (window.RenderTransform is TransformGroup existing)
            {
                group = existing;
            }
            else
            {
                group = new TransformGroup();

                group.Children.Add(new ScaleTransform(1, 1));
                group.Children.Add(new TranslateTransform());

                window.RenderTransform = group;
            }

            ScaleTransform scale =
                (ScaleTransform)group.Children[0];

            TranslateTransform translate =
                (TranslateTransform)group.Children[1];

            //
            // Opacity
            //

            if (options.Style.HasFlag(WindowAnimationStyle.Fade))
            {
                window.Opacity = showing ? 0.0 : 1.0;
            }
            else
            {
                window.Opacity = 1.0;
            }

            //
            // Scale
            //

            if (options.Style.HasFlag(WindowAnimationStyle.Scale))
            {
                double value = showing ? options.ScaleFrom : 1.0;

                scale.ScaleX = value;
                scale.ScaleY = value;
            }
            else
            {
                scale.ScaleX = 1.0;
                scale.ScaleY = 1.0;
            }

            //
            // Translation
            //

            if (options.Style.HasFlag(WindowAnimationStyle.Slide))
            {
                translate.X = showing ? options.SlideX : 0.0;
                translate.Y = showing ? options.SlideY : 0.0;
            }
            else
            {
                translate.X = 0.0;
                translate.Y = 0.0;
            }
        }

        private Storyboard BuildStoryboard(Window window, WindowAnimationOptions options, bool showing)
        {
            Storyboard storyboard = new();

            if (options.Style.HasFlag(WindowAnimationStyle.Fade))
            {
                AddFade(window, storyboard, options, showing);
            }

            if (options.Style.HasFlag(WindowAnimationStyle.Scale))
            {
                AddScale(window, storyboard, options, showing);
            }

            if (options.Style.HasFlag(WindowAnimationStyle.Slide))
            {
                AddSlide(window, storyboard, options, showing);
            }

            return storyboard;
        }

        private static void AddFade(Window window, Storyboard storyboard, WindowAnimationOptions options, bool showing)
        {
            DoubleAnimation animation = new()
            {
                From = showing ? 0.0 : 1.0,
                To = showing ? 1.0 : 0.0,
                Duration = new Duration(options.Duration),
                EasingFunction = options.Easing
            };

            Storyboard.SetTarget(animation, window);
            Storyboard.SetTargetProperty(
                animation,
                new PropertyPath(Window.OpacityProperty));

            storyboard.Children.Add(animation);
        }

        private static void AddScale(
            Window window,
            Storyboard storyboard,
            WindowAnimationOptions options,
            bool showing)
        {
            TransformGroup group = (TransformGroup)window.RenderTransform;

            ScaleTransform scale =
                (ScaleTransform)group.Children[0];

            double from = showing ? options.ScaleFrom : 1.0;
            double to = showing ? 1.0 : options.ScaleFrom;

            DoubleAnimation xAnimation = new()
            {
                From = from,
                To = to,
                Duration = new Duration(options.Duration),
                EasingFunction = options.Easing
            };

            DoubleAnimation yAnimation = new()
            {
                From = from,
                To = to,
                Duration = new Duration(options.Duration),
                EasingFunction = options.Easing
            };

            Storyboard.SetTarget(xAnimation, window);
            Storyboard.SetTarget(yAnimation, window);

            Storyboard.SetTargetProperty(
                xAnimation,
                new PropertyPath(
                    "RenderTransform.Children[0].ScaleX"));

            Storyboard.SetTargetProperty(
                yAnimation,
                new PropertyPath(
                    "RenderTransform.Children[0].ScaleY"));

            storyboard.Children.Add(xAnimation);
            storyboard.Children.Add(yAnimation);
        }

        private static void AddSlide(
            Window window,
            Storyboard storyboard,
            WindowAnimationOptions options,
            bool showing)
        {
            double fromX = showing ? options.SlideX : 0;
            double toX = showing ? 0 : options.SlideX;

            double fromY = showing ? options.SlideY : 0;
            double toY = showing ? 0 : options.SlideY;

            DoubleAnimation xAnimation = new()
            {
                From = fromX,
                To = toX,
                Duration = new Duration(options.Duration),
                EasingFunction = options.Easing
            };

            DoubleAnimation yAnimation = new()
            {
                From = fromY,
                To = toY,
                Duration = new Duration(options.Duration),
                EasingFunction = options.Easing
            };

            Storyboard.SetTarget(xAnimation, window);
            Storyboard.SetTarget(yAnimation, window);

            Storyboard.SetTargetProperty(
                xAnimation,
                new PropertyPath(
                    "RenderTransform.Children[1].X"));

            Storyboard.SetTargetProperty(
                yAnimation,
                new PropertyPath(
                    "RenderTransform.Children[1].Y"));

            storyboard.Children.Add(xAnimation);
            storyboard.Children.Add(yAnimation);
        }
    }
}