using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using Button = System.Windows.Controls.Button;
using UserControl = System.Windows.Controls.UserControl;

namespace RealmStudioX.WPF.Views.Panels.WorldGlobeViewerPanels
{
    /// <summary>
    /// Interaction logic for LightingTools.xaml
    /// </summary>
    public partial class LightingTools : UserControl
    {
        public LightingTools()
        {
            InitializeComponent();
        }

        private void AmbientLightColor_LeftClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is not WorldGlobeSceneViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(
                vm.LightingModel.AmbientLightColor, Window.GetWindow(this));

            dialog.ColorSelected += color => vm.LightingModel.AmbientLightColor = color;
            dialog.ShowDialog();
        }

        private void AmbientLightColor_RightClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is not WorldGlobeSceneViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(
                vm.LightingModel.AmbientLightColor, Window.GetWindow(this), (Button)sender);

            dialog.Closed += (_, _) =>
            {
                if (dialog.ColorWasSelected)
                    vm.LightingModel.AmbientLightColor = dialog.SelectedColor;
            };

            dialog.ShowDialog();
        }
    }
}
