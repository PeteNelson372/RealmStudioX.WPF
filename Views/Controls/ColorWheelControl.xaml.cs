using RealmStudioX.WPF.Editor.UserInterface;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace RealmStudioX.WPF.Views.Controls
{
    public partial class ColorWheelControl : UserControl
    {
        public ColorWheelControl()
        {
            InitializeComponent();

            Loaded += (_, _) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    GenerateWheel();
                    UpdateMarkerPosition();
                }), System.Windows.Threading.DispatcherPriority.Render);
            };

            SizeChanged += (_, _) =>
            {
                GenerateWheel();
                UpdateMarkerPosition();
            };
        }

        // Hue property (binds to ViewModel)
        public double Hue
        {
            get => (double)GetValue(HueProperty);
            set => SetValue(HueProperty, value);
        }

        public static readonly DependencyProperty HueProperty =
            DependencyProperty.Register(nameof(Hue),
                typeof(double),
                typeof(ColorWheelControl),
                new FrameworkPropertyMetadata(0.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnColorComponentChanged));

        public double Saturation
        {
            get => (double)GetValue(SaturationProperty);
            set => SetValue(SaturationProperty, value);
        }

        public static readonly DependencyProperty SaturationProperty =
            DependencyProperty.Register(nameof(Saturation),
                typeof(double),
                typeof(ColorWheelControl),
                new FrameworkPropertyMetadata(1.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnColorComponentChanged));

        private static void OnColorComponentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ColorWheelControl)d;
            control.UpdateMarkerPosition();
        }

        private void GenerateWheel()
        {
            int size = (int)Math.Min(ActualWidth, ActualHeight) * 2;

            if (size < 10)
                return;

            int stride = size * 4;
            byte[] pixels = new byte[size * stride];

            // Critical: pixel-perfect center
            double center = (size - 1) / 2.0;
            double radius = center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    double dx = x - center;
                    double dy = y - center;

                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance <= radius + 1) // allow 1px feather zone
                    {
                        double angle = Math.Atan2(dy, dx);
                        double hue = (angle * 180 / Math.PI + 360) % 360;

                        double saturation = Math.Clamp(distance / radius, 0.0, 1.0);

                        Color color = ColorHelper.HsvToRgb(hue, saturation, 1.0);

                        // Anti-alias alpha
                        double alpha = 1.0;

                        double edgeWidth = 1.5;

                        if (distance > radius - edgeWidth)
                        {
                            alpha = Math.Clamp((radius - distance) / edgeWidth, 0.0, 1.0);
                        }

                        byte a = (byte)(alpha * 255);

                        int index = y * stride + x * 4;

                        pixels[index + 0] = color.B;
                        pixels[index + 1] = color.G;
                        pixels[index + 2] = color.R;
                        pixels[index + 3] = a;
                    }
                    else
                    {
                        // Transparent outside circle
                        int index = y * stride + x * 4;
                        pixels[index + 3] = 0;
                    }
                }
            }

            var bmp = BitmapSource.Create(
                size, size,
                96, 96,
                PixelFormats.Bgra32,
                null,
                pixels,
                stride);

            WheelImage.Source = bmp;
        }



        private bool _isDragging;

        private void Wheel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
            _isDragging = true;
            WheelImage.CaptureMouse();
            UpdateHueSat(e);
        }

        private void Wheel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                UpdateHueSat(e);
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
            WheelImage.ReleaseMouseCapture();
        }

        private void UpdateHueSat(MouseEventArgs e)
        {
            var pos = e.GetPosition(WheelImage);

            double width = WheelImage.ActualWidth;
            double height = WheelImage.ActualHeight;

            if (width <= 0 || height <= 0)
                return;

            double size = Math.Min(width, height);
            double center = (size - 1) / 2.0;

            double dx = pos.X - center;
            double dy = pos.Y - center;

            double radius = center;

            double distance = Math.Sqrt(dx * dx + dy * dy);

            // Saturation = radial distance (clamped, NOT position)
            double saturation = Math.Clamp(distance / radius, 0.0, 1.0);
            saturation = Math.Pow(saturation, 0.9);

            // Hue = angle (use raw dx/dy, NOT clamped)
            double angle = Math.Atan2(dy, dx);
            double hue = (angle * 180.0 / Math.PI + 360.0) % 360.0;

            Hue = hue;
            Saturation = saturation;

            UpdateMarkerPosition();
        }

        private void UpdateMarkerPosition()
        {
            double width = WheelImage.ActualWidth;
            double height = WheelImage.ActualHeight;

            if (width <= 0 || height <= 0)
                return;

            double size = Math.Min(width, height);
            double center = (size - 1) / 2.0;

            double radius = center;

            double angleRad = Hue * Math.PI / 180.0;
            double r = radius * Saturation;

            double x = center + r * Math.Cos(angleRad);
            double y = center + r * Math.Sin(angleRad);

            // Outer
            Canvas.SetLeft(MarkerOuter, x - MarkerOuter.Width / 2);
            Canvas.SetTop(MarkerOuter, y - MarkerOuter.Height / 2);

            // Inner
            Canvas.SetLeft(MarkerInner, x - MarkerInner.Width / 2);
            Canvas.SetTop(MarkerInner, y - MarkerInner.Height / 2);
        }
    }
}