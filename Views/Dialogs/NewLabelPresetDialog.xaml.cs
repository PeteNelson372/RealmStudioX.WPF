using RealmStudioX.WPF.Editor.UserInterface;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for NewLabelPresetDialog.xaml
    /// </summary>
    public partial class NewLabelPresetDialog : ModalDialog
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public NewLabelPresetDialog()
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
