using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Dialogs;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ThreeDXModelViewer.xaml
    /// </summary>
    public partial class ThreeDXModelViewer : ModelessDialog, INotifyPropertyChanged
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public ThreeDXViewModel ViewModel { get; private set; }

        public event EventHandler? OpenClicked;
        public event EventHandler? SaveClicked;
        public event EventHandler? MinimizeClicked;
        public event EventHandler? MaximizeClicked;
        public event EventHandler? ExitClicked;

        public ThreeDXModelViewer()
        {
            InitializeComponent();

            ViewModel = new ThreeDXViewModel(this);

            DataContext = ViewModel;

            ThreeDXTitleBar.DataContext = ViewModel;

            ThreeDXMenu.DataContext = ViewModel;

            SizeChanged += (s, e) => OnWindowSizeChanged(ActualWidth, ActualHeight);

            Loaded += (s, e) =>
            {
                ThreeDXTitleBar.MinimizeClicked += (s, e) => MinimizeHandler();
                ThreeDXTitleBar.MaximizeClicked += (s, e) => MaximizeHandler();
                ThreeDXTitleBar.ExitClicked += (s, e) => ExitHandler();

                ThreeDXMenu.ExitClicked += (s, e) => ExitHandler();

                OnWindowSizeChanged(ActualWidth, ActualHeight);
            };
        }

        public void OnWindowSizeChanged(double width, double height)
        {
            ModelViewer.Width = width - 20;  // account for the width of the margins
            ModelViewer.Height = height - 100; // account for the height of the title bar, menu, and margins
        }

        private void ExitHandler()
        {
            Close();
        }

        private void MaximizeHandler()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void MinimizeHandler()
        {
            WindowState = WindowState.Minimized;
        }


        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
