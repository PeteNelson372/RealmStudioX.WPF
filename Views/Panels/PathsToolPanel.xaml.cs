using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Panels;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for PathsToolPanel.xaml
    /// </summary>
    public partial class PathsToolPanel : System.Windows.Controls.UserControl
    {
        public PathsToolPanel()
        {
            InitializeComponent();
        }

        private void PathStyle_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not MapPathViewModel vm)
                return;

            if (sender is not System.Windows.Controls.RadioButton btn)
                return;

            if (btn.Tag is not PathType pathType)
                return;

            vm.PathStyle = pathType;
        }
    }
}
