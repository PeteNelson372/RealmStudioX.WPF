using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ToolTip = System.Windows.Controls.ToolTip;

namespace RealmStudioX.WPF.Editor.Behaviors
{
    public static class SliderToolTipBehavior
    {
        public static readonly DependencyProperty EnableFormattedToolTipProperty =
            DependencyProperty.RegisterAttached(
                "EnableFormattedToolTip",
                typeof(bool),
                typeof(SliderToolTipBehavior),
                new PropertyMetadata(false, OnChanged));

        public static bool GetEnableFormattedToolTip(DependencyObject obj)
            => (bool)obj.GetValue(EnableFormattedToolTipProperty);

        public static void SetEnableFormattedToolTip(DependencyObject obj, bool value)
            => obj.SetValue(EnableFormattedToolTipProperty, value);

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Slider slider)
                return;

            if ((bool)e.NewValue)
            {
                slider.PreviewMouseLeftButtonDown += OnStart;
                slider.PreviewMouseLeftButtonUp += OnEnd;
                slider.ValueChanged += OnValueChanged;
            }
            else
            {
                slider.PreviewMouseLeftButtonDown -= OnStart;
                slider.PreviewMouseLeftButtonUp -= OnEnd;
                slider.ValueChanged -= OnValueChanged;
            }
        }

        private static readonly DependencyProperty ToolTipProperty =
            DependencyProperty.RegisterAttached(
                "ToolTipInstance",
                typeof(ToolTip),
                typeof(SliderToolTipBehavior));

        private static void OnStart(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                var tt = new ToolTip
                {
                    PlacementTarget = slider,
                    Placement = System.Windows.Controls.Primitives.PlacementMode.Top,
                    StaysOpen = true
                };

                slider.SetValue(ToolTipProperty, tt);

                UpdateToolTip(slider, slider.Value);
                tt.IsOpen = true;
            }
        }

        private static void OnEnd(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                if (slider.GetValue(ToolTipProperty) is ToolTip tt)
                {
                    tt.IsOpen = false;
                }
            }
        }

        private static void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider)
            {
                UpdateToolTip(slider, e.NewValue);
            }
        }

        private static void UpdateToolTip(Slider slider, double value)
        {
            if (slider.GetValue(ToolTipProperty) is ToolTip tt)
            {
                tt.Content = $"{value * 100:0}%";
            }
        }
    }
}
