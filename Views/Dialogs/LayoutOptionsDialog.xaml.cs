using RealmStudioX.WPF.Editor.UserInterface;
using System.Windows;
using Application = System.Windows.Application;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for LayoutOptionsDialog.xaml
    /// </summary>
    public partial class LayoutOptionsDialog : FloatingToolbar
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public LayoutOptionsDialog()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            WindowManager wm = ((App)Application.Current).WindowManager;
            wm.Close(this);
        }

    }
}
