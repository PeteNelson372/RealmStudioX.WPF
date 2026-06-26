using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using System.Windows.Input;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;

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

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.OceanColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.OceanColor = color;
            };

            wm.Show(dialog);
        }

        private void OceanColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not OceanPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.OceanColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.OceanColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }

        private void WindroseColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not OceanPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.WindroseColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.WindroseColor = color;
            };

            wm.Show(dialog);
        }

        private void WindroseColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not OceanPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.WindroseColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.WindroseColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }
    }
}
