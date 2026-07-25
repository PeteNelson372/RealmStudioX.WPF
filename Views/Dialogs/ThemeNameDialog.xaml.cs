using RealmStudioX.WPF.Editor.UserInterface;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ThemeNameDialog.xaml
    /// </summary>
    public partial class ThemeNameDialog : ModalDialog
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public ThemeNameDialog()
        {
            InitializeComponent();
        }

        private void CreateButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
