using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Panels;
using System.Windows;

namespace RealmStudioX.WPF.Views.Controls
{
    /// <summary>
    /// Interaction logic for LandformControls.xaml
    /// </summary>
    public partial class LandformControls : System.Windows.Controls.UserControl
    {
        private LandformPanelViewModel ViewModel => (LandformPanelViewModel)DataContext;

        public LandformControls()
        {
            InitializeComponent();
        }

        private void SelectGeneratedLandformType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag != null && btn.Tag is GeneratedLandformType type)
            {
                ViewModel.SelectedLandformType = type;
            }
        }
    }
}
