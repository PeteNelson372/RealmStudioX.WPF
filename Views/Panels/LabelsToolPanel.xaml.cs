using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using System.Windows.Input;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for LabelsToolPanel.xaml
    /// </summary>
    public partial class LabelsToolPanel : System.Windows.Controls.UserControl
    {
        public LabelsToolPanel()
        {
            InitializeComponent();
        }

        private void BoxTint_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LabelsPanelViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog()
            {
                Owner = Window.GetWindow(this),
                InitialColor = vm.BoxTint
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.BoxTint = color;
            };

            colorSelectionWindow.Show();
        }

        private void BoxTint_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LabelsPanelViewModel vm)
                return;

            var dialog = new ColorQuickPick()
            {
                InitialColor = vm.BoxTint
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
                    vm.BoxTint = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }
    }
}
