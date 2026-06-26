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

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.FrameColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.FrameColor = color;
            };

            wm.Show(dialog);
        }

        private void FrameColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not OverlaysPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.FrameColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.FrameColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }

        private void GridColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not OverlaysPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.GridColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.GridColor = color;
            };

            wm.Show(dialog);
        }

        private void GridColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not OverlaysPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.GridColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.GridColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }

        private void MeasureColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not OverlaysPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.MeasureColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.MeasureColor = color;
            };

            wm.Show(dialog);
        }

        private void MeasureColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not OverlaysPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.MeasureColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.MeasureColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }
    }
}
