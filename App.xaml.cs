using RealmStudioShapeRenderingLib;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Views.Dialogs;
using System.IO;
using System.Reflection;
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

            var fontManager = new FontManager();

            // Start tasks
            var loadTask = assetManager.LoadAsync();
            var splashTask = splash.WaitForCloseAsync();
            var fontTask = fontManager.InitializeAsync(Assembly.GetExecutingAssembly());

            // Wait for tasks to complete
            await Task.WhenAll(loadTask, splashTask, fontTask);

            // Ensure splash is closed (in case load finished last)
            if (splash.IsVisible)
            {
                splash.Close();
            }

            // Continue startup - open the CreateOpenMapDialog
            var dialog = new CreateOpenMapDialog();
            var result = dialog.ShowDialog();

            if (result != true || dialog.ViewModel.Result == null)
            {
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow(dialog.ViewModel.Result, assetManager, fontManager);

            MainWindow = mainWindow;
            mainWindow.Show();

            Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
    }

}
