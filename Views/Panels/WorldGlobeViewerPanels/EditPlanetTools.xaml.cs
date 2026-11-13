using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using UserControl = System.Windows.Controls.UserControl;

namespace RealmStudioX.WPF.Views.Panels.WorldGlobeViewerPanels;

public partial class EditPlanetTools : UserControl
{
    public EditPlanetTools()
    {
        InitializeComponent();
    }

    private void PlanetColor_LeftClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not WorldGlobeSceneViewModel vm || vm.PlanetModel is null)
            return;

        ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(
            vm.PlanetModel.PlanetColor, Window.GetWindow(this));

        dialog.ColorSelected += color => vm.PlanetModel.PlanetColor = color;
        dialog.ShowDialog();
    }

    private void PlanetColor_RightClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not WorldGlobeSceneViewModel vm || vm.PlanetModel is null)
            return;

        ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(
            vm.PlanetModel.PlanetColor, Window.GetWindow(this), (Button)sender);

        dialog.Closed += (_, _) =>
        {
            if (dialog.ColorWasSelected)
                vm.PlanetModel.PlanetColor = dialog.SelectedColor;
        };

        dialog.ShowDialog();
    }
}
