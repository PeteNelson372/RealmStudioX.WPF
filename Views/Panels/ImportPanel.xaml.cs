using RealmStudioX.WPF.ViewModels.Panels;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for ImportPanel.xaml
    /// </summary>
    public partial class ImportPanel : System.Windows.Controls.UserControl
    {
        private ImportPanelViewModel? ViewModel { get; }

        public ImportPanel()
        {
            InitializeComponent();
        }
    }
}
