using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using Button = System.Windows.Controls.Button;
using UserControl = System.Windows.Controls.UserControl;

namespace RealmStudioX.WPF.Views.Panels.WorldGlobeViewerPanels;

public partial class PlanetsTools : UserControl
{
    public PlanetsTools()
    {
        InitializeComponent();
    }

    private void PlanetColor_LeftClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not WorldGlobeSceneViewModel vm)
            return;

        ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(
            vm.CreatePlanetModel.PlanetColor, Window.GetWindow(this));

        dialog.ColorSelected += color => vm.CreatePlanetModel.PlanetColor = color;
        dialog.ShowDialog();
    }

    private void PlanetColor_RightClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not WorldGlobeSceneViewModel vm)
            return;

        ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(
            vm.CreatePlanetModel.PlanetColor, Window.GetWindow(this), (Button)sender);

        dialog.Closed += (_, _) =>
        {
            if (dialog.ColorWasSelected)
                vm.CreatePlanetModel.PlanetColor = dialog.SelectedColor;
        };

        dialog.ShowDialog();
    }
}
