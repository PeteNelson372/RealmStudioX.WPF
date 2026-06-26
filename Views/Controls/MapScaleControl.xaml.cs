using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Controls;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using System.Windows.Input;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;

namespace RealmStudioX.WPF.Views.Controls
{
    /// <summary>
    /// Interaction logic for MapScaleControl.xaml
    /// </summary>
    public partial class MapScaleControl : System.Windows.Controls.UserControl
    {
        public MapScaleControl()
        {
            InitializeComponent();
        }

        private void SegmentColor1_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.SegmentColor1, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.SegmentColor1 = color;
            };

            wm.Show(dialog);
        }

        private void SegmentColor1_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.SegmentColor1, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.SegmentColor1 = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }

        private void SegmentColor2_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.SegmentColor2, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.SegmentColor2 = color;
            };

            wm.Show(dialog);
        }

        private void SegmentColor2_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.SegmentColor2, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.SegmentColor2 = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }


        private void SegmentColor3_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.SegmentColor3, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.SegmentColor3 = color;
            };

            wm.Show(dialog);
        }

        private void SegmentColor3_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.SegmentColor3, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.SegmentColor3 = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }

        private void FontColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.FontColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.FontColor = color;
            };

            wm.Show(dialog);
        }

        private void FontColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.FontColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.FontColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);

        }

        private void NumbersOutlineColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.NumbersOutlineColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.NumbersOutlineColor = color;
            };

            wm.Show(dialog);
        }

        private void NumbersOutlineColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.NumbersOutlineColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.NumbersOutlineColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }
    }
}
