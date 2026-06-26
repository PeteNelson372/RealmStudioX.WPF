using RealmStudioX.WPF.Editor.UserInterface;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for SelectionFilterDialog.xaml
    /// </summary>
    public partial class SelectionFilterDialog : FloatingToolbar
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public SelectionFilterDialog()
        {
            InitializeComponent();
        }
    }
}
