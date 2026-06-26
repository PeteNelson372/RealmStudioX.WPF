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
    /// Interaction logic for PathPanel.xaml
    /// </summary>
    public partial class PathsPanel : System.Windows.Controls.UserControl
    {
        public PathsPanel()
        {
            InitializeComponent();
        }

        private void PathColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not PathPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.PathColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.PathColor = color;
            };

            wm.Show(dialog);
        }

        private void PathColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not PathPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.PathColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.PathColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }

        private void PathBorderColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not PathPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.PathBorderColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.PathBorderColor = color;
            };

            wm.Show(dialog);
        }

        private void PathBorderColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not PathPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.PathBorderColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.PathBorderColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }
    }
}
