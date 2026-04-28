using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using System.Windows.Input;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for WaterPanel.xaml
    /// </summary>
    public partial class WaterPanel : System.Windows.Controls.UserControl
    {
        private WaterPanelViewModel ViewModel =>
            (WaterPanelViewModel)DataContext;

        public WaterPanel()
        {
            InitializeComponent();
        }

        private void ShallowWaterColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not WaterPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog(vm.ShallowWaterColor)
            {
                Owner = Window.GetWindow(this)
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.ShallowWaterColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void ShallowWaterColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not WaterPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick(vm.ShallowWaterColor);

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
                    vm.ShallowWaterColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }

        private void DeepWaterColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not WaterPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog(vm.DeepWaterColor)
            {
                Owner = Window.GetWindow(this)
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.DeepWaterColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void DeepWaterColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not WaterPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick(vm.DeepWaterColor);

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
                    vm.DeepWaterColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }

        private void ShorelineColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not WaterPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog(vm.ShorelineColor)
            {
                Owner = Window.GetWindow(this)
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.ShorelineColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void ShorelineColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not WaterPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick(vm.ShorelineColor);

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
                    vm.ShorelineColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }
    }
}
