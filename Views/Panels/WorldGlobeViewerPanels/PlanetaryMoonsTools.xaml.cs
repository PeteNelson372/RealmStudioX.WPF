using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using Button = System.Windows.Controls.Button;
using UserControl = System.Windows.Controls.UserControl;

namespace RealmStudioX.WPF.Views.Panels.WorldGlobeViewerPanels;

public partial class PlanetaryMoonsTools : UserControl
{
    public PlanetaryMoonsTools()
    {
        InitializeComponent();
    }

    private void MoonColor_LeftClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not WorldGlobeSceneViewModel vm)
            return;

        ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(
            vm.CreateMoonModel.MoonColor, Window.GetWindow(this));

        dialog.ColorSelected += color => vm.CreateMoonModel.MoonColor = color;
        dialog.ShowDialog();
    }

    private void MoonColor_RightClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not WorldGlobeSceneViewModel vm)
            return;

        ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(
            vm.CreateMoonModel.MoonColor, Window.GetWindow(this), (Button)sender);

        dialog.Closed += (_, _) =>
        {
            if (dialog.ColorWasSelected)
                vm.CreateMoonModel.MoonColor = dialog.SelectedColor;
        };

        dialog.ShowDialog();
    }
}
