using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using Application = System.Windows.Application;

namespace RealmStudioX.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Prevent app from closing when splash closes
            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 1. Show splash
            var splash = new SplashWindow();
            splash.ShowDialog();

            // 2. Show startup dialog (New / Open)
            var dialog = new StartupDialog();
            var result = dialog.ShowDialog();

            if (result != true || dialog.Result == null)
            {
                Shutdown();
                return;
            }

            // 3. Create MainWindow and pass result
            var mainWindow = new MainWindow(dialog.Result);

            MainWindow = mainWindow;
            mainWindow.Show();

            // Restore normal shutdown behavior
            Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
    }

}
