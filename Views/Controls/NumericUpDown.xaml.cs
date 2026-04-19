using System.Windows;

namespace RealmStudioX.WPF.Views.Controls
{
    /// <summary>
    /// Interaction logic for NumericUpDown.xaml
    /// </summary>
    public partial class NumericUpDown : System.Windows.Controls.UserControl
    {
        private bool _suppressValueChanged;

        public NumericUpDown()
        {
            InitializeComponent();
        }

        public static readonly RoutedEvent ValueChangedEvent =
            EventManager.RegisterRoutedEvent(
            nameof(ValueChanged),
            RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<double>),
            typeof(NumericUpDown));

        public event RoutedPropertyChangedEventHandler<double> ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }


        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(NumericUpDown),
            new FrameworkPropertyMetadata(
                0.0,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnValueChanged));

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (NumericUpDown)d;

            if (control._suppressValueChanged)
                return;

            if (Equals(e.OldValue, e.NewValue))
                return;

            var args = new RoutedPropertyChangedEventArgs<double>(
                (double)e.OldValue,
                (double)e.NewValue)
            {
                RoutedEvent = ValueChangedEvent
            };

            control.RaiseEvent(args);
        }

        public void SetValueSilently(double value)
        {
            try
            {
                _suppressValueChanged = true;
                SetCurrentValue(ValueProperty, value);

                ValueBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateTarget();
            }
            finally
            {
                _suppressValueChanged = false;
            }
        }

        public void SetValue(double value, bool raiseEvent = true)
        {
            if (!raiseEvent)
            {
                SetValueSilently(value);
            }
            else
            {
                Value = value;
            }
        }

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(NumericUpDown), new PropertyMetadata(0.0));

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(NumericUpDown), new PropertyMetadata(double.MaxValue));

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly DependencyProperty IncrementProperty =
            DependencyProperty.Register(nameof(Increment), typeof(double), typeof(NumericUpDown), new PropertyMetadata(1.0));

        public double Increment
        {
            get => (double)GetValue(IncrementProperty);
            set => SetValue(IncrementProperty, value);
        }

        private void OnIncrease(object sender, RoutedEventArgs e)
        {
            Value = Math.Min(Value + Increment, Maximum);
        }

        private void OnDecrease(object sender, RoutedEventArgs e)
        {
            Value = Math.Max(Value - Increment, Minimum);
        }
    }
}
