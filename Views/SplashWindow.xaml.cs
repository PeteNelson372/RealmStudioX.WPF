using System.Windows;
using System.Windows.Threading;

namespace RealmStudioX.WPF
{
    /// <summary>
    /// Interaction logic for SplashWindow.xaml
    /// </summary>
    public partial class SplashWindow : Window
    {
        private readonly TaskCompletionSource _tcs = new();

        public Task WaitForCloseAsync() => _tcs.Task;

        public SplashWindow()
        {
            InitializeComponent();

            // Auto-close after 6 seconds
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(6)
            };

            timer.Tick += (s, e) =>
            {
                timer.Stop();
                Close();
            };

            timer.Start();

            // Close on click
            MouseDown += (_, _) => Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);            
            _tcs.TrySetResult();
        }
    }
}
