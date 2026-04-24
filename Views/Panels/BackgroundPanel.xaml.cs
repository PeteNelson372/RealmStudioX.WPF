using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using System.Windows.Input;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for BackgroundPanel.xaml
    /// </summary>
    public partial class BackgroundPanel : System.Windows.Controls.UserControl
    {
        public BackgroundPanel()
        {
            InitializeComponent();
        }

        private void VignetteColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not BackgroundPanelViewModel vm)
                return;

            var dialog = new ColorSelectionDialog(vm.VignetteColor)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                vm.VignetteColor = dialog.SelectedColor;
            }
        }

        private void VignetteColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not BackgroundPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick(vm.VignetteColor);

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
                    vm.VignetteColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }
    }
}
