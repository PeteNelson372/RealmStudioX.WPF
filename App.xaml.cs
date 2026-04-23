using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Views.Dialogs;
using System.IO;
using System.Windows;
using Application = System.Windows.Application;

namespace RealmStudioX.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var splash = new SplashWindow();
            splash.Show();

            var assetManager = new AssetManager();
            AssetManager.RootRealmStudioXDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RealmStudioX");

            // Start both tasks
            var loadTask = assetManager.LoadAsync();
            var splashTask = splash.WaitForCloseAsync();

            // Wait for BOTH to complete
            await Task.WhenAll(loadTask, splashTask);

            // Ensure splash is closed (in case load finished last)
            if (splash.IsVisible)
                splash.Close();

            // Continue startup
            var dialog = new StartupDialog();
            var result = dialog.ShowDialog();

            if (result != true || dialog.ViewModel.Result == null)
            {
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow(dialog.ViewModel.Result, assetManager);

            MainWindow = mainWindow;
            mainWindow.Show();

            Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
    }

}
