using RealmStudioX.WPF.ViewModels.Panels;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for WaterPanel.xaml
    /// </summary>
    public partial class WaterPanel : System.Windows.Controls.UserControl
    {
        private WaterPanelViewModel ViewModel =>
            (WaterPanelViewModel)DataContext;

        public WaterPanel()
        {
            InitializeComponent();


        }
    }
}
