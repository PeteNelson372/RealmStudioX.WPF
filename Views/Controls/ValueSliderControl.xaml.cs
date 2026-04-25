using RealmStudioX.WPF.Editor.UserInterface;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using UserControl = System.Windows.Controls.UserControl;

namespace RealmStudioX.WPF.Views.Controls
{
    public partial class ValueSliderControl : UserControl
    {
        private bool _isDragging;

        public ValueSliderControl()
        {
            InitializeComponent();

            Loaded += OnLoaded;

            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
        }

        // =========================
        // 🎯 VALUE (0–1)
        // =========================

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(ValueSliderControl),
                new FrameworkPropertyMetadata(
                    1.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValueChanged));

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (ValueSliderControl)d;
            c.UpdateMarker();
        }

        // =========================
        // 🎨 HUE
        // =========================

        public double Hue
        {
            get => (double)GetValue(HueProperty);
            set => SetValue(HueProperty, value);
        }

        public static readonly DependencyProperty HueProperty =
            DependencyProperty.Register(
                nameof(Hue),
                typeof(double),
                typeof(ValueSliderControl),
                new PropertyMetadata(0.0, OnHueSatChanged));

        // =========================
        // 🎨 SATURATION
        // =========================

        public double Saturation
        {
            get => (double)GetValue(SaturationProperty);
            set => SetValue(SaturationProperty, value);
        }

        public static readonly DependencyProperty SaturationProperty =
            DependencyProperty.Register(
                nameof(Saturation),
                typeof(double),
                typeof(ValueSliderControl),
                new PropertyMetadata(1.0, OnHueSatChanged));

        private static void OnHueSatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (ValueSliderControl)d;
            c.UpdateGradient();
        }

        // =========================
        // 🎨 GRADIENT
        // =========================

        private void UpdateGradient()
        {
            var top = ColorHelper.HsvToRgb(Hue, Saturation, 1.0);

            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };

            brush.GradientStops.Add(new GradientStop(top, 0.0));
            brush.GradientStops.Add(new GradientStop(Colors.Black, 1.0));

            GradientRect.Fill = brush;
        }

        // =========================
        // 📍 MARKER
        // =========================

        private void UpdateMarker()
        {
            double height = GradientRect.ActualHeight;

            if (height <= 0)
                return;

            double y = (1.0 - Value) * height;

            Canvas.SetTop(MarkerOuter, y - MarkerOuter.Height / 2);
            Canvas.SetLeft(MarkerOuter, (ActualWidth - MarkerOuter.Width) / 2);

            Canvas.SetTop(MarkerInner, y - MarkerInner.Height / 2);
            Canvas.SetLeft(MarkerInner, (ActualWidth - MarkerInner.Width) / 2);
        }

        // =========================
        // 🖱️ MOUSE INTERACTION
        // =========================

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
            _isDragging = true;
            CaptureMouse();
            UpdateFromMouse(e);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
                UpdateFromMouse(e);
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            ReleaseMouseCapture();
        }

        private void UpdateFromMouse(MouseEventArgs e)
        {
            var pos = e.GetPosition(GradientRect);

            double height = GradientRect.ActualHeight;
            if (height <= 0)
                return;

            double v = 1.0 - (pos.Y / height);
            v = Math.Clamp(v, 0.0, 1.0);

            Value = v;
        }

        // =========================
        // 🔁 INIT
        // =========================

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateGradient();
            UpdateMarker();
        }
    }
}