using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace RealmStudioX.WPF.Views.Controls
{
    /// <summary>
    /// Interaction logic for NumericUpDown.xaml
    /// </summary>
    public partial class NumericUpDown : System.Windows.Controls.UserControl
    {
        private bool _suppressValueChanged;
        private const double HardMin = -1e12;
        private const double HardMax = 1e12;

        private DispatcherTimer _repeatTimer;
        private bool _isIncreasing;
        private int _repeatInterval = 400; // ms (initial delay)

        public NumericUpDown()
        {
            InitializeComponent();

            this.PreviewMouseWheel += OnMouseWheel;

            _repeatTimer = new DispatcherTimer();
            _repeatTimer.Tick += OnRepeatTick;

            this.PreviewMouseUp += OnMouseUp;
            this.MouseLeave += (s, e) => OnMouseLeave(s, e);
        }

        public double CoarseFactor { get; set; } = 0.01; // 1%
        public double FineFactor { get; set; } = 0.1;

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                SetValue(Clamp(Value + Increment));
            }
            else if (e.Delta < 0)
            {
                SetValue(Clamp(Value - Increment));
            }

            e.Handled = true;
        }

        private void StartRepeat()
        {
            _repeatInterval = 400; // reset delay
            _repeatTimer.Interval = TimeSpan.FromMilliseconds(_repeatInterval);
            _repeatTimer.Start();

            ApplyStep(); // immediate first step
        }

        private void StopRepeat()
        {
            _repeatTimer.Stop();
        }

        private void OnRepeatTick(object? sender, EventArgs e)
        {
            ApplyStep();

            // accelerate
            if (_repeatInterval > 50)
            {
                _repeatInterval -= 30;
                _repeatTimer.Interval = TimeSpan.FromMilliseconds(_repeatInterval);
            }
        }

        private void ApplyStep()
        {
            double newValue = _isIncreasing
                ? Value + Increment
                : Value - Increment;

            SetValue(Clamp(newValue));
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

            control.UpdateText();

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
                SetCurrentValue(ValueProperty, Normalize(value));
            }
            finally
            {
                _suppressValueChanged = false;
            }
        }

        public void SetValue(double value, bool raiseEvent = true)
        {
            value = Normalize(value);

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
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(NumericUpDown),
                new PropertyMetadata(HardMin, OnMinimumChanged));

        private static void OnMinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (NumericUpDown)d;
            if (control.Minimum > control.Maximum)
                control.Maximum = control.Minimum;

            control.SetCurrentValue(ValueProperty, control.Clamp(control.Value));
        }

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(NumericUpDown),
                new PropertyMetadata(HardMax, OnMaximumChanged));

        private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (NumericUpDown)d;
            if (control.Maximum < control.Minimum)
                control.Minimum = control.Maximum;

            control.SetCurrentValue(ValueProperty, control.Clamp(control.Value));
        }

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

        public static readonly DependencyProperty DecimalPlacesProperty =
            DependencyProperty.Register(
            nameof(DecimalPlaces),
            typeof(int),
            typeof(NumericUpDown),
            new PropertyMetadata(0));

        public int DecimalPlaces
        {
            get => (int)GetValue(DecimalPlacesProperty);
            set => SetValue(DecimalPlacesProperty, value);
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.Captured == this)
                Mouse.Capture(null);

            StopRepeat();
        }

        private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            StopRepeat();

            if (Mouse.Captured == this)
                Mouse.Capture(null);
        }

        private void OnIncrease(object sender, RoutedEventArgs e)
        {
            _isIncreasing = true;

            Mouse.Capture(this);
            StartRepeat();
        }

        private void OnDecrease(object sender, RoutedEventArgs e)
        {
            _isIncreasing = false;

            Mouse.Capture(this);
            StartRepeat();
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow digits, decimal, minus
            e.Handled = !Regex.IsMatch(e.Text, @"[0-9\.\-]");
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            CommitText();
        }

        private void OnTextBoxGotFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(ValueBox.SelectAll), DispatcherPriority.Input);
        }


        private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!IsEnabled)
                return;

            bool shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            bool ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            if (e.Key == Key.Up)
            {
                if (!double.TryParse(ValueBox.Text, out _))
                    return; // ignore until valid

                CommitText(); // commit ONLY when needed

                double step = GetStepSize(shift, ctrl);
                SetValue(Value + step);
                e.Handled = true;
                return;
            }
            else if (e.Key == Key.Down)
            {
                if (!double.TryParse(ValueBox.Text, out _))
                    return; // ignore until valid

                CommitText();

                double step = GetStepSize(shift, ctrl);
                SetValue(Value - step);
                e.Handled = true;
                return;
            }
            else if (e.Key == Key.PageUp)
            {
                if (!double.TryParse(ValueBox.Text, out _))
                    return; // ignore until valid

                CommitText();

                double step = GetStepSize(true, false) * 5;
                SetValue(Value + step);
                e.Handled = true;
                return;
            }
            else if (e.Key == Key.PageDown)
            {
                if (!double.TryParse(ValueBox.Text, out _))
                    return; // ignore until valid

                CommitText();

                double step = GetStepSize(true, false) * 5;
                SetValue(Value - step);
                e.Handled = true;
                return;
            }
            else if (e.Key == Key.Enter)
            {
                CommitText();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Cancel();

                // move focus away so caret disappears
                MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

                e.Handled = true;
                return;
            }
        }

        public void Cancel()
        {
            // Revert text to current Value (no event)
            UpdateText();

            // Optional: select all so user can immediately retype
            ValueBox.SelectAll();
        }

        private void CommitText()
        {
            if (double.TryParse(ValueBox.Text, out double parsed))
            {
                SetValue(parsed);
            }
            else
            {
                UpdateText();
            }
        }

        public bool Commit()
        {
            if (double.TryParse(ValueBox.Text, out double parsed))
            {
                SetValue(parsed);
                return true;
            }
            else
            {
                UpdateText(); // revert
                return false;
            }
        }

        private double Normalize(double value)
        {
            value = Clamp(value);

            if (DecimalPlaces >= 0)
                value = Math.Round(value, DecimalPlaces);

            return value;
        }

        private double Clamp(double value)
        {
            return Math.Clamp(value, Minimum, Maximum);
        }

        private double GetStepSize(bool shift, bool ctrl)
        {
            double step = Increment;

            if (shift)
            {
                double range = Maximum - Minimum;

                if (range > 0 && range < double.MaxValue / 2)
                    step = Math.Max(step, range * 0.01);
                else
                    step *= 10;
            }
            else if (ctrl)
            {
                step *= 0.1;
            }

            // enforce integer mode
            if (DecimalPlaces == 0)
                step = Math.Max(1, Math.Round(step));

            return step;
        }

        private void UpdateText()
        {
            ValueBox.Text = Value.ToString($"F{DecimalPlaces}");
        }
    }
}
