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
    /// Interaction logic for AtmosphereTools.xaml
    /// </summary>
    public partial class AtmosphereTools : UserControl
    {
        public AtmosphereTools()
        {
            InitializeComponent();
        }

        private void AtmosphereColor_LeftClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not WorldGlobeSceneViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.AtmosphereModel.AtmosphereColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.AtmosphereModel.AtmosphereColor = color;
            };

            dialog.ShowDialog();
        }

        private void AtmosphereColor_RightClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not WorldGlobeSceneViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.AtmosphereModel.AtmosphereColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.AtmosphereModel.AtmosphereColor = dialog.SelectedColor;
                }
            };

            dialog.ShowDialog();
        }


        private void CloudColor_LeftClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not WorldGlobeSceneViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.AtmosphereModel.CloudColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.AtmosphereModel.CloudColor = color;
            };

            dialog.ShowDialog();
        }

        private void CloudColor_RightClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not WorldGlobeSceneViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.AtmosphereModel.CloudColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.AtmosphereModel.CloudColor = dialog.SelectedColor;
                }
            };

            dialog.ShowDialog();
        }
    }
}
