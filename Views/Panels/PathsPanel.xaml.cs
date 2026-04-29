using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using System.Windows.Input;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for PathPanel.xaml
    /// </summary>
    public partial class PathsPanel : System.Windows.Controls.UserControl
    {
        public PathsPanel()
        {
            InitializeComponent();
        }

        private void PathColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapPathViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog(vm.PathColor)
            {
                Owner = Window.GetWindow(this)
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.PathColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void PathColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapPathViewModel vm)
                return;

            var dialog = new ColorQuickPick(vm.PathColor);

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
                    vm.PathColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }

        private void PathBorderColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapPathViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog(vm.PathBorderColor)
            {
                Owner = Window.GetWindow(this)
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.PathBorderColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void PathBorderColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapPathViewModel vm)
                return;

            var dialog = new ColorQuickPick(vm.PathBorderColor);

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
                    vm.PathBorderColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }
    }
}
