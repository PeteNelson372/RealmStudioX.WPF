using System.Windows;

namespace RealmStudioX.WPF.Views.Controls
{
    /// <summary>
    /// Interaction logic for MainTitleBar.xaml
    /// </summary>
    public partial class MainTitleBar : System.Windows.Controls.UserControl
    {
        public event EventHandler? MinimizeClicked;
        public event EventHandler? MaximizeClicked;
        public event EventHandler? ExitClicked;

        public MainTitleBar()
        {
            InitializeComponent();
        }

        private void OnExit(object sender, RoutedEventArgs e)
            => ExitClicked?.Invoke(this, EventArgs.Empty);

        private void OnMinimize(object sender, RoutedEventArgs e)
            => MinimizeClicked?.Invoke(this, EventArgs.Empty);

        private void OnMaximize(object sender, RoutedEventArgs e)
            => MaximizeClicked?.Invoke(this, EventArgs.Empty);
    }
}
