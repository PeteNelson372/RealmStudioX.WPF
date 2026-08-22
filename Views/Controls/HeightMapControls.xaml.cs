using RealmStudioX.WPF.ViewModels.Panels;

namespace RealmStudioX.WPF.Views.Controls
{
    /// <summary>
    /// Interaction logic for HeightMapControls.xaml
    /// </summary>
    public partial class HeightMapControls : System.Windows.Controls.UserControl
    {
        private LandformPanelViewModel ViewModel => (LandformPanelViewModel)DataContext;

        public HeightMapControls()
        {
            InitializeComponent();
        }
    }
}
