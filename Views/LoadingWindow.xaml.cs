using RealmStudioX.WPF.Editor.UserInterface;
using System.Windows.Threading;

namespace RealmStudioX.WPF.Views
{
    /// <summary>
    /// Interaction logic for LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : ModelessDialog
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public override WindowAnimationProfile AnimationProfile => WindowAnimationProfiles.None;

        private readonly TaskCompletionSource _tcs = new();

        public Task WaitForCompleteAsync() => _tcs.Task;

        private string _loadingStatus = "";

        public string LoadingStatus
        {
            get => _loadingStatus;
            set
            {
                _loadingStatus = value;

                if (LoadingStatusText != null)
                {
                    LoadingStatusText.Text = value;
                }

                UpdateLayout();
            }
        }

        private string _applicationVersion = "";

        public string ApplicationVersion
        {
            get => _applicationVersion;
            set
            {
                _applicationVersion = value;

                if (VersionText != null)
                {
                    VersionText.Text = value;
                }

                UpdateLayout();
            }
        }

        public LoadingWindow()
        {
            InitializeComponent();

            // Continue after 6 seconds
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(6)
            };

            timer.Tick += (s, e) =>
            {
                timer.Stop();
                _tcs.TrySetResult();
            };

            timer.Start();

            // Close on click
            //MouseDown += (_, _) => Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _tcs.TrySetResult();
        }
    }
}
