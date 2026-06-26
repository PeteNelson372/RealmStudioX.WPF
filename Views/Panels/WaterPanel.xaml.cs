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
    /// Interaction logic for WaterPanel.xaml
    /// </summary>
    public partial class WaterPanel : System.Windows.Controls.UserControl
    {
        public WaterPanel()
        {
            InitializeComponent();
        }

        private void ShallowWaterColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not WaterPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.ShallowWaterColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.ShallowWaterColor = color;
            };

            wm.Show(dialog);
        }

        private void ShallowWaterColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not WaterPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.ShallowWaterColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.ShallowWaterColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }

        private void DeepWaterColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not WaterPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.DeepWaterColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.DeepWaterColor = color;
            };

            wm.Show(dialog);
        }

        private void DeepWaterColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not WaterPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.DeepWaterColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.DeepWaterColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }

        private void ShorelineColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not WaterPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.ShorelineColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.ShorelineColor = color;
            };

            wm.Show(dialog);
        }

        private void ShorelineColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not WaterPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.ShorelineColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.ShorelineColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }
    }
}
