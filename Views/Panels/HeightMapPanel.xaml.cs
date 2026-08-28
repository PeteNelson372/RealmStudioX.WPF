using RealmStudioX.WPF.ViewModels.Panels;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for HeightMapPanel.xaml
    /// </summary>
    public partial class HeightMapPanel : System.Windows.Controls.UserControl
    {
        private HeightMapPanelViewModel? ViewModel { get; }

        public HeightMapPanel()
        {
            InitializeComponent();
        }
    }
}
