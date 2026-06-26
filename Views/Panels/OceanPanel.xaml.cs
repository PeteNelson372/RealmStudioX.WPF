using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using System.Windows.Input;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for OceanPanel.xaml
    /// </summary>
    public partial class OceanPanel : System.Windows.Controls.UserControl
    {
        public OceanPanel()
        {
            InitializeComponent();
        }

        private void OceanColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not OceanPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog()
            {
                Owner = Window.GetWindow(this),
                InitialColor = vm.OceanColor
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.OceanColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void OceanColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not OceanPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick()
            {
                InitialColor = vm.OceanColor
            };

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
                    vm.OceanColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }

        private void WindroseColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not OceanPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog()
            {
                Owner = Window.GetWindow(this),
                InitialColor = vm.WindroseColor
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.WindroseColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void WindroseColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not OceanPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick()
            {
                InitialColor = vm.WindroseColor
            };

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
                    vm.WindroseColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }
    }
}
