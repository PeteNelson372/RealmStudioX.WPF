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
    /// Interaction logic for HeightMapToolPanel.xaml
    /// </summary>
    public partial class HeightMapToolPanel : System.Windows.Controls.UserControl
    {
        public HeightMapToolPanel()
        {
            InitializeComponent();
        }

        private void MajorLineColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not HeightMapPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.MajorLineColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.MajorLineColor = color;
            };

            wm.Show(dialog);
        }

        private void MajorLineColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not HeightMapPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.MajorLineColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.MajorLineColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }

        private void LineColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not HeightMapPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.LineColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.LineColor = color;
            };

            wm.Show(dialog);
        }

        private void LineColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not HeightMapPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.LineColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.LineColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }
    }
}
