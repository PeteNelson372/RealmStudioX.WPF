using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Controls;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ExportDialog.xaml
    /// </summary>
    public partial class ExportDialog : ModalDialog
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public ExportDialog()
        {
            InitializeComponent();

            Loaded += (_, _) =>
            {
                if (DataContext is RealmExportViewModel vm)
                {
                    vm.CloseRequested += (_, _) => Close();
                }
            };
        }
    }
}
