using RealmStudioX.WPF.ViewModels.Panels;
using System.Windows.Controls;
using System.Windows.Input;

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
    }
}
