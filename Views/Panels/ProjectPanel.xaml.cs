using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows.Controls;
using System.Windows.Input;
using TextBox = System.Windows.Controls.TextBox;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for ProjectPanel.xaml
    /// </summary>
    public partial class ProjectPanel : System.Windows.Controls.UserControl
    {
        public ProjectPanel()
        {
            InitializeComponent();
        }

        private void MapListItem_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
           if (sender is ListBoxItem item &&
                DataContext is ProjectPanelViewModel vm &&
                item.DataContext is ProjectMapTileViewModel tile
                && vm.Project != null)
            {
                vm.MainViewModel.OpenMap(vm.Project, tile.MapProjectEntry.Map);
            }
        }

        private void ProjectNameTextBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ProjectPanelViewModel viewModel)
            {
                if (sender is TextBox tb
                    && tb.Name == "ProjectNameTextBox"
                    && !string.IsNullOrEmpty(tb.Text)
                    && viewModel.Project != null
                    && tb.Text != viewModel.Project.Metadata.ProjectName
                    && UserInterfaceUtilities.IsValidFileName(tb.Text))
                {
                    string oldProjectName = viewModel.Project.Metadata.ProjectName;

                    MessageDialog dlg = MessageDialogFactory.ConfirmationDialog("Rename Project", "Are you sure you want to rename the project? Renaming the project will create a copy of the project.");

                    dlg.ShowDialog();

                    switch (((MessageDialogViewModel)dlg.DataContext).Result)
                    {
                        case MessageDialogResult.Yes:
                            viewModel.Project.Metadata.ProjectName = tb.Text;
                            viewModel.MainViewModel.CommandService.MarkProjectDataModified();
                            break;
                        case MessageDialogResult.No:
                            tb.Text = oldProjectName;
                            break;

                    }
                }
            }
        }

        private void ProjectDescriptionTextBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ProjectPanelViewModel viewModel)
            {
                if (sender is TextBox tb
                    && tb.Name == "ProjectDescriptionTextBox"
                    && !string.IsNullOrEmpty(tb.Text)
                    && viewModel.Project != null)
                {
                    viewModel.Project.Metadata.Description = tb.Text;
                    viewModel.MainViewModel.CommandService.MarkProjectDataModified();
                }
            }
        }
    }
}
