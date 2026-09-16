using System.Windows;

namespace RealmStudioX.WPF.Views.Controls
{
    /// <summary>
    /// Interaction logic for ThreeDXMenu.xaml
    /// </summary>
    public partial class ThreeDXMenu : System.Windows.Controls.UserControl
    {
        public event EventHandler? OpenClicked;
        public event EventHandler? SaveClicked;
        public event EventHandler? ExitClicked;

        public ThreeDXMenu()
        {
            InitializeComponent();
        }

        private void OnOpen(object sender, RoutedEventArgs e)
            => OpenClicked?.Invoke(this, EventArgs.Empty);

        private void OnSave(object sender, RoutedEventArgs e)
            => SaveClicked?.Invoke(this, EventArgs.Empty);

        private void OnExit(object sender, RoutedEventArgs e)
            => ExitClicked?.Invoke(this, EventArgs.Empty);

    }
}
