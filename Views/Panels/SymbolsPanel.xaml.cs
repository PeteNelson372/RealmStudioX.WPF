using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using System.Windows.Input;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for SymbolsPanel.xaml
    /// </summary>
    public partial class SymbolsPanel : System.Windows.Controls.UserControl
    {
        public SymbolsPanel()
        {
            InitializeComponent();
        }

        private void OnSymbolScaleLock(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SymbolsPanelViewModel vm)
                return;

            vm.SymbolScaleLocked = !vm.SymbolScaleLocked;
        }

        private void SymbolColor1_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not SymbolsPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog(vm.SymbolColor1)
            {
                Owner = Window.GetWindow(this)
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.SymbolColor1 = color;
            };

            colorSelectionWindow.Show();
        }

        private void SymbolColor1_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not SymbolsPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick(vm.SymbolColor1);

            // Position near button
            var button = (FrameworkElement)sender;
            var pos = button.PointToScreen(new Point(0, button.ActualHeight));

            dialog.WindowStartupLocation = WindowStartupLocation.Manual;
            dialog.Left = pos.X;
            dialog.Top = pos.Y;

            dialog.Owner = Window.GetWindow(this);

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.SymbolColor1 = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }

        private void SymbolColor2_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not SymbolsPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog(vm.SymbolColor2)
            {
                Owner = Window.GetWindow(this)
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.SymbolColor2 = color;
            };

            colorSelectionWindow.Show();
        }

        private void SymbolColor2_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not SymbolsPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick(vm.SymbolColor2);

            // Position near button
            var button = (FrameworkElement)sender;
            var pos = button.PointToScreen(new Point(0, button.ActualHeight));

            dialog.WindowStartupLocation = WindowStartupLocation.Manual;
            dialog.Left = pos.X;
            dialog.Top = pos.Y;

            dialog.Owner = Window.GetWindow(this);

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.SymbolColor2 = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }

        private void SymbolColor3_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not SymbolsPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog(vm.SymbolColor3)
            {
                Owner = Window.GetWindow(this)
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.SymbolColor3 = color;
            };

            colorSelectionWindow.Show();
        }

        private void SymbolColor3_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not SymbolsPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick(vm.SymbolColor3);

            // Position near button
            var button = (FrameworkElement)sender;
            var pos = button.PointToScreen(new Point(0, button.ActualHeight));

            dialog.WindowStartupLocation = WindowStartupLocation.Manual;
            dialog.Left = pos.X;
            dialog.Top = pos.Y;

            dialog.Owner = Window.GetWindow(this);

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.SymbolColor3 = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }
    }
}
