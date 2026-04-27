using System.Windows;
using System.Windows.Input;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using Cursor = System.Windows.Input.Cursor;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ColorSelectionDialog.xaml
    /// </summary>
    public partial class ColorSelectionDialog : Window
    {
        public ColorSelectionViewModel ViewModel { get; }

        public Color SelectedColor => ViewModel.CurrentColor;

        public event Action<Color>? ColorSelected;

        private Cursor? _eyedropperCursor;

        public ColorSelectionDialog(Color initialColor)
        {
            InitializeComponent();

            ViewModel = new ColorSelectionViewModel
            {
                CurrentColor = initialColor
            };

            DataContext = ViewModel;
        }

        private Cursor GetEyedropperCursor()
        {
            if (_eyedropperCursor != null)
                return _eyedropperCursor;

            var uri = new Uri("pack://application:,,,/Assets/cur/eyedropper.cur", UriKind.Absolute);

            var resource = Application.GetResourceStream(uri);
            if (resource?.Stream == null)
                throw new InvalidOperationException("Could not load eyedropper cursor.");

            _eyedropperCursor = new Cursor(resource.Stream);
            return _eyedropperCursor;
        }

        private void Eyedropper_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var overlay = new EyedropperOverlayWindow(GetEyedropperCursor())
                {
                    Owner = this
                };

                overlay.ColorPicked += color =>
                {
                    if (DataContext is ColorSelectionViewModel vm)
                    {
                        vm.CurrentColor = color;
                    }

                    this.Visibility = Visibility.Visible;
                    this.Activate();
                };

                overlay.Cancelled += () =>
                {
                    this.Visibility = Visibility.Visible;
                    this.Activate();
                };

                overlay.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start eyedropper tool: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Show();
                this.Activate();
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            ColorSelected?.Invoke(SelectedColor);
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HexTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                tb.Dispatcher.BeginInvoke(new Action(() =>
                {
                    tb.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }

            if (DataContext is ColorSelectionViewModel vm)
            {
                vm.IsEditingHex = true;
                // DO NOT clear here if you're using SelectAll instead
            }
        }

        private void HexTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is ColorSelectionViewModel vm)
            {
                vm.IsEditingHex = false;
                vm.CommitHex();
            }
        }

        private void HexTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (DataContext is ColorSelectionViewModel vm)
                    vm.CommitHex();

                e.Handled = true;
            }
        }

        private void ColorNameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                tb.Dispatcher.BeginInvoke(new Action(() =>
                {
                    tb.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }

            if (DataContext is ColorSelectionViewModel vm)
            {
                vm.IsEditingColorName = true;
                // DO NOT clear here if you're using SelectAll instead
            }
        }

        private void ColorNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is ColorSelectionViewModel vm)
            {
                vm.IsEditingColorName = false;
                vm.CommitColorName();
            }
        }

        private void ColorNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is ColorSelectionViewModel vm)
                {
                    vm.IsEditingColorName = false;
                    vm.CommitColorName();
                }
                e.Handled = true;
            }
        }

        private void TextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                // If already focused, manually select all
                if (tb.IsKeyboardFocusWithin)
                {
                    tb.SelectAll();
                    e.Handled = true; // prevent default caret placement
                }
                else
                {
                    // Not focused yet → focus and select
                    e.Handled = true;
                    tb.Focus();

                    tb.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        tb.SelectAll();
                    }), System.Windows.Threading.DispatcherPriority.Input);
                }
            }
        }

        private void RgbTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                tb.Dispatcher.BeginInvoke(new Action(() =>
                {
                    tb.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }

            if (DataContext is ColorSelectionViewModel vm)
            {
                vm.IsEditingRgb = true;
                // DO NOT clear here if you're using SelectAll instead
            }
        }

        private void RgbTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is ColorSelectionViewModel vm)
                    vm.CommitRgb();

                e.Handled = true;
            }
        }

        private void RgbTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is ColorSelectionViewModel vm)
            {
                vm.IsEditingRgb = false;
                vm.CommitRgb();
            }
        }

        private void HsvTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                tb.Dispatcher.BeginInvoke(new Action(() =>
                {
                    tb.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }

            if (DataContext is ColorSelectionViewModel vm)
            {
                vm.IsEditingHsv = true;
                // DO NOT clear here if you're using SelectAll instead
            }
        }

        private void HsvTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is ColorSelectionViewModel vm)
                    vm.CommitHsv();

                e.Handled = true;
            }
        }

        private void HsvTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is ColorSelectionViewModel vm)
            {
                vm.IsEditingHsv = false;
                vm.CommitHsv();
            }
        }
    }
}
