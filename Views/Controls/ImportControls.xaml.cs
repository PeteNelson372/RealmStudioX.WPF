using RealmStudioX.WPF.ViewModels.Panels;

namespace RealmStudioX.WPF.Views.Controls
{
    /// <summary>
    /// Interaction logic for ImportControls.xaml
    /// </summary>
    public partial class ImportControls : System.Windows.Controls.UserControl
    {
        private ImportPanelViewModel ViewModel => (ImportPanelViewModel)DataContext;

        public ImportControls()
        {
            InitializeComponent();
        }
    }
}
