using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using System.Windows.Input;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for OverlaysPanel.xaml
    /// </summary>
    public partial class OverlaysPanel : System.Windows.Controls.UserControl
    {
        public OverlaysPanel()
        {
            InitializeComponent();
        }

        private void FrameColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not OverlaysPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog()
            {
                Owner = Window.GetWindow(this),
                InitialColor = vm.FrameColor
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.FrameColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void FrameColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not OverlaysPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick()
            {
                InitialColor = vm.FrameColor
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
                    vm.FrameColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }

        private void GridColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not OverlaysPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog()
            {
                Owner = Window.GetWindow(this),
                InitialColor = vm.GridColor
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.GridColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void GridColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not OverlaysPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick()
            {
                InitialColor = vm.GridColor
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
                    vm.GridColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }

        private void MeasureColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not OverlaysPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog()
            {
                Owner = Window.GetWindow(this),
                InitialColor = vm.MeasureColor
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.MeasureColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void MeasureColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not OverlaysPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick()
            {
                InitialColor = vm.MeasureColor
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
                    vm.MeasureColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }
    }
}
