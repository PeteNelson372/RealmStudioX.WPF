using System.Windows;

namespace RealmStudioX.WPF.Views
{
    /// <summary>
    /// Interaction logic for MainMenu.xaml
    /// </summary>
    public partial class MainMenu : System.Windows.Controls.UserControl
    {
        public event EventHandler? NewClicked;
        public event EventHandler? OpenClicked;
        public event EventHandler? SaveClicked;
        public event EventHandler? ExitClicked;
        public event EventHandler? UndoClicked;
        public event EventHandler? RedoClicked;

        public MainMenu()
        {
            InitializeComponent();
        }

        private void OnNew(object sender, RoutedEventArgs e)
            => NewClicked?.Invoke(this, EventArgs.Empty);

        private void OnOpen(object sender, RoutedEventArgs e)
            => OpenClicked?.Invoke(this, EventArgs.Empty);

        private void OnSave(object sender, RoutedEventArgs e)
            => SaveClicked?.Invoke(this, EventArgs.Empty);

        private void OnExit(object sender, RoutedEventArgs e)
            => ExitClicked?.Invoke(this, EventArgs.Empty);

        private void OnUndo(object sender, RoutedEventArgs e)
            => UndoClicked?.Invoke(this, EventArgs.Empty);

        private void OnRedo(object sender, RoutedEventArgs e)
            => RedoClicked?.Invoke(this, EventArgs.Empty);
    }
}
