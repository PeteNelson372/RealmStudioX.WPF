using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using UserControl = System.Windows.Controls.UserControl;

namespace RealmStudioX.WPF.Views.Panels.WorldGlobeViewerPanels
{
    /// <summary>
    /// Interaction logic for LocalStarTools.xaml
    /// </summary>
    public partial class LocalStarTools : UserControl
    {
        public LocalStarTools()
        {
            InitializeComponent();
        }

        private void StarlightColor_LeftClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not WorldGlobeSceneViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.StarViewModel.StarlightColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.StarViewModel.StarlightColor = color;
            };

            dialog.ShowDialog();
        }

        private void StarlightColor_RightClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not WorldGlobeSceneViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.StarViewModel.StarlightColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.StarViewModel.StarlightColor = dialog.SelectedColor;
                }
            };

            dialog.ShowDialog();
        }
    }
}
