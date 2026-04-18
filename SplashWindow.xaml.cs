using System.Windows;
using System.Windows.Threading;

namespace RealmStudioX.WPF
{
    /// <summary>
    /// Interaction logic for SplashWindow.xaml
    /// </summary>
    public partial class SplashWindow : Window
    {
        private readonly DispatcherTimer _timer;

        public SplashWindow()
        {
            InitializeComponent();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(6)
            };

            _timer.Tick += (s, e) => CloseSplash();
            _timer.Start();

            MouseDown += (_, __) => CloseSplash();
        }

        private void CloseSplash()
        {
            _timer.Stop();
            DialogResult = true; // signals completion
            Close();
        }
    }
}
