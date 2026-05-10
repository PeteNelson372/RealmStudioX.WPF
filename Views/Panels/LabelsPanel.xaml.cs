using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using System.Windows.Input;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for LabelsPanel.xaml
    /// </summary>
    public partial class LabelsPanel : System.Windows.Controls.UserControl
    {
        public LabelsPanel()
        {
            InitializeComponent();
        }

        private void LabelColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LabelsPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog(vm.LabelColor)
            {
                Owner = Window.GetWindow(this)
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.LabelColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void LabelColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LabelsPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick(vm.LabelColor);

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
                    vm.LabelColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }

        private void OutlineColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LabelsPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog(vm.OutlineColor)
            {
                Owner = Window.GetWindow(this)
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.OutlineColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void OutlineColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LabelsPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick(vm.OutlineColor);

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
                    vm.OutlineColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }

        private void GlowColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LabelsPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog(vm.GlowColor)
            {
                Owner = Window.GetWindow(this)
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.GlowColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void GlowColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LabelsPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick(vm.GlowColor);

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
                    vm.GlowColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }

        private void GenerateNameButton_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is LabelsPanelViewModel vm)
            {
                var cmd = vm.MainViewModel.OpenNameGeneratorConfigCommand;
                if (cmd != null && cmd.CanExecute(null))
                {
                    cmd.Execute(null);
                }
            }

            e.Handled = true;
        }
    }
}
