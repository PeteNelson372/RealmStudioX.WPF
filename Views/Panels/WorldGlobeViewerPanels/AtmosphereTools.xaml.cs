using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.ViewModels.Panels;
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
            if (DataContext is not WorldGlobeViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.AtmosphereColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.AtmosphereColor = color;
            };

            dialog.ShowDialog();
        }

        private void AtmosphereColor_RightClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not WorldGlobeViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.AtmosphereColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.AtmosphereColor = dialog.SelectedColor;
                }
            };

            dialog.ShowDialog();
        }


        private void CloudColor_LeftClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not WorldGlobeViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.CloudColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.CloudColor = color;
            };

            dialog.ShowDialog();
        }

        private void CloudColor_RightClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not WorldGlobeViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.CloudColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.CloudColor = dialog.SelectedColor;
                }
            };

            dialog.ShowDialog();
        }
    }
}
