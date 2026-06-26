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
    /// Interaction logic for BackgroundPanel.xaml
    /// </summary>
    public partial class BackgroundPanel : System.Windows.Controls.UserControl
    {
        public BackgroundPanel()
        {
            InitializeComponent();
        }

        private void VignetteColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not BackgroundPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.VignetteColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.VignetteColor = color;
            };

            wm.Show(dialog);
        }

        private void VignetteColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not BackgroundPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.VignetteColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.VignetteColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }
    }
}
