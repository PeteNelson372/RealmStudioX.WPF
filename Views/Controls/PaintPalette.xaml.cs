using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Windows;
using System.Windows.Controls;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;

namespace RealmStudioX.WPF.Views.Controls
{
    /// <summary>
    /// Interaction logic for PaintPalette.xaml
    /// </summary>
    public partial class PaintPalette : System.Windows.Controls.UserControl
    {
        private IPaintToolViewModel? viewModel => DataContext as IPaintToolViewModel;

        public PaintPalette()
        {
            InitializeComponent();
        }

        private void PaintColor_LeftClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not IPaintToolViewModel vm)
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
            if (DataContext is not IPaintToolViewModel vm)
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
            if (DataContext is not IPaintToolViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.PaintingColor.ToColor(), Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                if (vm.PaintPalette == null)
                    return;

                if (!vm.PaintPalette.ContainsColor(color.ToSKColor()))
                {
                    vm.PaintPalette.AddColor(color.ToSKColor());
                }
            };

            wm.Show(dialog);
        }

        private void AddColor_RightClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not IPaintToolViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.PaintingColor.ToColor(), Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    if (vm.PaintPalette == null)
                        return;

                    if (!vm.PaintPalette.ContainsColor(dialog.SelectedColor.ToSKColor()))
                    {
                        vm.PaintPalette.AddColor(dialog.SelectedColor.ToSKColor());
                    }
                }
            };

            wm.Show(dialog);
        }

        private void DeleteColor_LeftClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
                return;

            if (menuItem.DataContext is not ColorPaletteEntry paletteEntry)
                return;

            if (viewModel == null || viewModel.PaintPalette == null)
                return;

            SKColor removeColor = paletteEntry.Color;
            viewModel.PaintPalette.RemoveColor(removeColor);
        }
    }
}
