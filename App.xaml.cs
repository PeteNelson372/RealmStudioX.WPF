using log4net;
using log4net.Config;
using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor.Services;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.Views;
using RealmStudioX.WPF.Views.Dialogs;
using System.Diagnostics;
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
        public RecoveryService? RecoveryService { get; set; } = null;
        public WindowManager WindowManager { get; } = new();

        private readonly Assembly _assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        public string ApplicationName => _assembly.GetName().Name ?? "RealmStudioX";

        public string CompanyName => GetAttribute<AssemblyCompanyAttribute>()?.Company ?? "Pete Nelson";

        public string ProductName => GetAttribute<AssemblyProductAttribute>()?.Product ?? ApplicationName;

        public string Copyright => GetAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";

        public string Description => GetAttribute<AssemblyDescriptionAttribute>()?.Description ?? "";

        /// <summary>
        /// Semantic version string (preferred for display).
        /// </summary>
        public static string Version
        {
            get
            {
                Assembly assembly = Assembly.GetEntryAssembly()!;

                AssemblyInformationalVersionAttribute? info =
                    assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

                if (info != null)
                {
                    return info.InformationalVersion;
                }

                Version? version = assembly.GetName().Version;

                return version?.ToString() ?? string.Empty;
            }
        }

        /// <summary>
        /// Assembly version (e.g. 1.2.0.0).
        /// </summary>
        public string AssemblyVersion => _assembly.GetName().Version?.ToString() ?? "";

        /// <summary>
        /// File version.
        /// </summary>
        public string FileVersion =>
            FileVersionInfo
                .GetVersionInfo(_assembly.Location)
                .FileVersion ?? "";

        /// <summary>
        /// Executable build date.
        /// </summary>
        public DateTime BuildDate =>
            File.GetLastWriteTime(_assembly.Location);

        public string BuildDateString =>
            BuildDate.ToString("MMMM d, yyyy");

        public string ExecutablePath =>
            _assembly.Location;

        public string ExecutableDirectory =>
            Path.GetDirectoryName(_assembly.Location) ?? "";

        private T? GetAttribute<T>()
            where T : Attribute
        {
            return _assembly.GetCustomAttribute<T>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // set up and configure the RealmStudioXLogger
            string logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RealmStudioX", "Logs");

            Directory.CreateDirectory(logFolder);

            GlobalContext.Properties["LogFileName"] = Path.Combine(logFolder, "RealmStudioX");

            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Properties", "Logging", "log4net.config");

            Assembly? assembly = Assembly.GetEntryAssembly();

            if (assembly == null)
            {
                Application.Current.Shutdown(-1);
                return;
            }

            // set up log4net configuration
            XmlConfigurator.Configure(LogManager.GetRepository(assembly), new FileInfo(configPath));

            SetupExceptionHandling();

            RealmStudioXLogger.Info("===========================================================");
            RealmStudioXLogger.Info($"Starting RealmStudioX at {DateTime.Now}; Version={ApplicationInfo.Version}");
            RealmStudioXLogger.Info("===========================================================");

            base.OnStartup(e);

            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            LoadingWindow loading = WindowManager.Create<LoadingWindow>();
            loading.ApplicationVersion = $"Version {AssemblyVersion}";
            loading.LoadingStatus = $"Loading Assets...";

            WindowManager.Show(loading);

            await Task.Delay(100); // Allow the loading window to render

            var assetManager = new AssetManager();
            AssetManager.RootRealmStudioXDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RealmStudioX");

            var fontManager = new FontManager();

            // Start tasks
            var assetTask = assetManager.LoadAsync();
            var loadingTask = loading.WaitForCompleteAsync();
            var fontTask = fontManager.InitializeAsync(Assembly.GetExecutingAssembly());

            // Wait for tasks to complete
            await Task.WhenAll(assetTask, loadingTask, fontTask);

            // Continue startup - open the CreateOpenMapDialog

            CreateOpenMapDialog createOpenDialog = WindowManager.Create<CreateOpenMapDialog>();

            var result = WindowManager.ShowDialog(createOpenDialog);

            if (result != true || createOpenDialog.ViewModel.Result == null)
            {
                Shutdown();
                return;
            }

            loading.LoadingStatus = $"Loading Main Window...";
            await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Render);
            await Task.Delay(100); // Allow the loading window to render

            var mainWindow = new MainWindow(createOpenDialog.ViewModel.Result, assetManager, fontManager);

            Current.MainWindow = mainWindow;
            MainWindow = mainWindow;
            
            mainWindow.Show();
            await mainWindow.RefreshTaskbarIconAsync();

            WindowManager.Close(loading);

            WindowManager.Close(createOpenDialog);

            Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                RealmStudioXLogger.Info("===========================================================");
                RealmStudioXLogger.Info($"RealmStudioX shutting down at {DateTime.Now}. Exit Code: {e.ApplicationExitCode}");
                RealmStudioXLogger.Info("===========================================================");
            }
            catch (Exception ex)
            {
                RealmStudioXLogger.Exception("Application Shutdown", ex);
            }

            base.OnExit(e);
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                HandleUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            DispatcherUnhandledException += (s, e) =>
            {
                HandleUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                HandleUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        private static void HandleUnhandledException(Exception exception, string source)
        {
            string message = $"Unhandled exception ({source})";
            try
            {
                System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
            }
            catch (Exception ex)
            {
                RealmStudioXLogger.Exception("Exception in HandleUnhandledException", ex);
            }
            finally
            {
                RealmStudioXLogger.Exception(message, exception);
            }

            try
            {
                ((App)Application.Current).RecoveryService?.WriteCrashPackage();
            }
            catch (Exception ex)
            {
                RealmStudioXLogger.Exception("Exception in HandleUnhandledException", ex);
            }

            try
            {
                System.Windows.MessageBox.Show(
                    "RealmStudioX encountered an unexpected error.\n\n" +
                    "A recovery package has been written.\n\n" +
                    "The application will now close.",
                    "Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
            }

            Environment.Exit(-1);
        }
    }

}
