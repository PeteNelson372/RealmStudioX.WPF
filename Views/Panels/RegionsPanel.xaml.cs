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

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.RegionColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.RegionColor = color;
            };

            wm.Show(dialog);
        }

        private void RegionColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not RegionPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.RegionColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.RegionColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }
    }
}
