using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using System.Windows.Input;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for RegionsPanel.xaml
    /// </summary>
    public partial class RegionsPanel : System.Windows.Controls.UserControl
    {
        public RegionsPanel()
        {
            InitializeComponent();
        }

        private void RegionColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not RegionPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog(vm.RegionColor)
            {
                Owner = Window.GetWindow(this)
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.RegionColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void RegionColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not RegionPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick(vm.RegionColor);

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
                    vm.RegionColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }
    }
}
