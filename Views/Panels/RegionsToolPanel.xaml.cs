using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Panels;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for RegionsToolPanel.xaml
    /// </summary>
    public partial class RegionsToolPanel : System.Windows.Controls.UserControl
    {
        public RegionsToolPanel()
        {
            InitializeComponent();
        }

        private void RegionStyle_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not RegionPanelViewModel vm)
                return;

            if (sender is not System.Windows.Controls.RadioButton btn)
                return;

            if (btn.Tag is not PathType pathType)
                return;

            vm.RegionStyle = pathType;
        }
    }
}
