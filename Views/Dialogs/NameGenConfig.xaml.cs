using RealmStudioX.WPF.Editor.UserInterface;
using System.Windows;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for NameGenConfig.xaml
    /// </summary>
    public partial class NameGenConfig : ModalDialog
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public NameGenConfig()
        {
            InitializeComponent();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            // TODO
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
