using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using System.Windows.Input;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for LandformPanel.xaml
    /// </summary>
    public partial class LandformPanel : System.Windows.Controls.UserControl
    {
        private LandformPanelViewModel ViewModel =>
            (LandformPanelViewModel)DataContext;

        public LandformPanel()
        {
            InitializeComponent();
        }

        private void SelectGeneratedLandformType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag != null && btn.Tag is GeneratedLandformType type)
            {
                ViewModel.SelectedLandformType = type;
            }

            DropDownButton.IsChecked = false;
        }

        private void OutlineColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LandformPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog(vm.LandformOutlineColor)
            {
                Owner = Window.GetWindow(this)
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.LandformOutlineColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void OutlineColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LandformPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick(vm.LandformOutlineColor);

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
                    vm.LandformOutlineColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }

        private void BackgroundColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LandformPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog(vm.LandformBackgroundColor)
            {
                Owner = Window.GetWindow(this)
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.LandformBackgroundColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void BackgroundColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LandformPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick(vm.LandformBackgroundColor);

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
                    vm.LandformBackgroundColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }

        private void CoastlineColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LandformPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog(vm.CoastlineColor)
            {
                Owner = Window.GetWindow(this)
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.CoastlineColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void CoastlineColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LandformPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick(vm.CoastlineColor);

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
                    vm.CoastlineColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }

        private void SelectCoastlineStyle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag != null && btn.Tag is LandformCoastlineStyle style)
            {
                ViewModel.SelectedCoastlineStyle = style;
            }

            DropDownButton.IsChecked = false;
        }
    }
}
