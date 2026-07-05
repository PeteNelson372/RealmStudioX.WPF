using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using SkiaSharp.Views.WPF;
using System.Windows;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for OceanToolPanel.xaml
    /// </summary>
    public partial class OceanToolPanel : System.Windows.Controls.UserControl
    {
        public OceanToolPanel()
        {
            InitializeComponent();
        }

        private void PaintColor_LeftClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not OceanPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.PaintingColor.ToColor(), Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.PaintingColor = color.ToSKColor();
            };

            wm.Show(dialog);
        }

        private void PaintColor_RightClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not OceanPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.PaintingColor.ToColor(), Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.PaintingColor = dialog.SelectedColor.ToSKColor();
                }
            };

            wm.Show(dialog);
        }

        private void AddColor_LeftClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not OceanPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.PaintingColor.ToColor(), Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.OceanPalette?.AddColor(color.ToSKColor());
            };

            wm.Show(dialog);
        }

        private void AddColor_RightClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not OceanPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.PaintingColor.ToColor(), Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.OceanPalette?.AddColor(dialog.SelectedColor.ToSKColor());
                }
            };

            wm.Show(dialog);
        }
    }
}
