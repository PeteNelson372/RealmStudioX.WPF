using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Dialogs;
using System.Windows;
using RealmStudioX.WPF.Views.Controls;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ThreeDViewer.xaml
    /// </summary>
    public partial class ThreeDViewer :  ModelessDialog
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public ThreeDViewModel ViewModel { get; private set; }

        public event EventHandler? OpenClicked;
        public event EventHandler? SaveClicked;
        public event EventHandler? MinimizeClicked;
        public event EventHandler? MaximizeClicked;
        public event EventHandler? ExitClicked;

        public ThreeDViewer()
        {
            InitializeComponent();

            ViewModel = new ThreeDViewModel(this);

            DataContext = ViewModel;

            ThreeDTitleBar.DataContext = ViewModel;

            ThreeDMenu.DataContext = ViewModel;

            Loaded += (s, e) =>
            {
                ThreeDTitleBar.MinimizeClicked += (s, e) => MinimizeHandler();
                ThreeDTitleBar.MaximizeClicked += (s, e) => MaximizeHandler();
                ThreeDTitleBar.ExitClicked += (s, e) => ExitHandler();

                ThreeDMenu.ExitClicked += (s, e) => ExitHandler();
            };
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

    }
}
