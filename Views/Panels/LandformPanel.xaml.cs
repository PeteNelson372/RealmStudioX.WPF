using RealmStudioShapeRenderingLib;
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
    /// Interaction logic for LandformPanel.xaml
    /// </summary>
    public partial class LandformPanel : System.Windows.Controls.UserControl
    {
        private LandformPanelViewModel ViewModel =>
            (LandformPanelViewModel)DataContext;

        public LandformPanel()
        {
            InitializeComponent();
        }

        private void SelectGeneratedLandformType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag != null && btn.Tag is GeneratedLandformType type)
            {
                ViewModel.SelectedLandformType = type;
            }

            DropDownButton.IsChecked = false;
        }

        private void OutlineColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LandformPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.LandformOutlineColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.LandformOutlineColor = color;
            };

            wm.Show(dialog);
        }

        private void OutlineColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LandformPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.LandformOutlineColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.LandformOutlineColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }

        private void BackgroundColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LandformPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.LandformBackgroundColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.LandformBackgroundColor = color;
            };

            wm.Show(dialog);
        }

        private void BackgroundColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LandformPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.LandformBackgroundColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.LandformBackgroundColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }

        private void CoastlineColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LandformPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.CoastlineColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.CoastlineColor = color;
            };

            wm.Show(dialog);
        }

        private void CoastlineColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LandformPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.CoastlineColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.CoastlineColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }

        private void SelectCoastlineStyle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag != null && btn.Tag is LandformCoastlineStyle style)
            {
                ViewModel.SelectedCoastlineStyle = style;
            }

            DropDownButton.IsChecked = false;
        }
    }
}
