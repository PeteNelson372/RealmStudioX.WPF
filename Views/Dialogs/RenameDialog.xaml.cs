using RealmStudioX.WPF.Editor.UserInterface;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for RenameDialog.xaml
    /// </summary>
    public partial class RenameDialog : ModalDialog
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public RenameDialog()
        {
            InitializeComponent();
        }

        private void RenameButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
