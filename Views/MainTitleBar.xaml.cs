using System.Windows;

namespace RealmStudioX.WPF.Views
{
    /// <summary>
    /// Interaction logic for MainTitleBar.xaml
    /// </summary>
    public partial class MainTitleBar : System.Windows.Controls.UserControl
    {
        public event EventHandler? OpenClicked;
        public event EventHandler? SaveClicked;
        public event EventHandler? MinimizeClicked;
        public event EventHandler? MaximizeClicked;
        public event EventHandler? ExitClicked;
        public event EventHandler? UpdateAvailableClicked;

        public MainTitleBar()
        {
            InitializeComponent();
        }

        private void OnOpen(object sender, RoutedEventArgs e)
            => OpenClicked?.Invoke(this, EventArgs.Empty);

        private void OnSave(object sender, RoutedEventArgs e)
            => SaveClicked?.Invoke(this, EventArgs.Empty);

        private void OnExit(object sender, RoutedEventArgs e)
            => ExitClicked?.Invoke(this, EventArgs.Empty);

        private void OnMinimize(object sender, RoutedEventArgs e)
            => MinimizeClicked?.Invoke(this, EventArgs.Empty);

        private void OnMaximize(object sender, RoutedEventArgs e)
            => MaximizeClicked?.Invoke(this, EventArgs.Empty);

        private void UpdateAvailable_Click(object sender, RoutedEventArgs e)
            => UpdateAvailableClicked?.Invoke(this, EventArgs.Empty);
    }
}
