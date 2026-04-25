using RealmStudioX.WPF.ViewModels.Controls;
using System.Windows;
using System.Windows.Input;
using Color = System.Windows.Media.Color;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ColorSelectionDialog.xaml
    /// </summary>
    public partial class ColorSelectionDialog : Window
    {
        public ColorSelectionViewModel ViewModel { get; }

        public Color SelectedColor => ViewModel.CurrentColor;

        public ColorSelectionDialog(Color initialColor)
        {
            InitializeComponent();

            ViewModel = new ColorSelectionViewModel
            {
                CurrentColor = initialColor
            };

            DataContext = ViewModel;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
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

        private void HexTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
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
