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
    /// Interaction logic for LabelsPanel.xaml
    /// </summary>
    public partial class LabelsPanel : System.Windows.Controls.UserControl
    {
        public LabelsPanel()
        {
            InitializeComponent();
        }

        private void LabelColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LabelsPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.LabelColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.LabelColor = color;
            };

            wm.Show(dialog);
        }

        private void LabelColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LabelsPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.LabelColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.LabelColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }

        private void OutlineColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LabelsPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.OutlineColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.OutlineColor = color;
            };

            wm.Show(dialog);
        }

        private void OutlineColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LabelsPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.OutlineColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.OutlineColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }

        private void GlowColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LabelsPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.GlowColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.GlowColor = color;
            };

            wm.Show(dialog);
        }

        private void GlowColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LabelsPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.GlowColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.GlowColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }

        private void GenerateNameButton_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is LabelsPanelViewModel vm)
            {
                var cmd = vm.MainViewModel.OpenNameGeneratorConfigCommand;
                if (cmd != null && cmd.CanExecute(null))
                {
                    cmd.Execute(null);
                }
            }

            e.Handled = true;
        }
    }
}
