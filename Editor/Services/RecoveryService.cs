using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using System.IO;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace RealmStudioX.WPF.Editor.Services
{
    public class RecoveryService : ViewModelBase
    {
        private readonly DispatcherTimer _saveTimer = new();

        private string autosavePath = string.Empty;

        private bool _hasUnsavedChanges = false;

        public event EventHandler? AutosaveCompleted;

        public RecoveryService()
        {
            _saveTimer.Interval = new TimeSpan(0, ApplicationPreferences.Current.AutosaveInterval, 0);
            _saveTimer.Tick += SaveTimer_Tick;
        }

        private static RecoveryPackage BuildRecoveryPackage(RealmStudioProject project, RealmStudioMap map)
        {
            RecoveryPackage recoveryPackage = new()
            {
                ProjectId = project.Metadata!.ProjectId,
                ProjectName = project.Metadata.ProjectName,
                ProjectPath = project.Metadata.ProjectFilePath,
                Map = map,
                Timestamp = DateTime.UtcNow
            };

            return recoveryPackage;
        }

        public void RemoveRecoveryPackages(RealmStudioProject project)
        {
            string projectId = project.Metadata.ProjectId;

            try
            {
                var files = Directory.EnumerateFiles(ApplicationPreferences.Current.AutosaveDirectory, "*" + RealmStudioFileFormat.MapCrashFileExtension, SearchOption.TopDirectoryOnly).ToList();

                foreach (var file in files)
                {
                    if (file.Contains(projectId))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                RealmStudioXLogger.Exception("Error removing crash recovery files", ex);
            }

            try
            {
                var files = Directory.EnumerateFiles(ApplicationPreferences.Current.AutosaveDirectory, "*" + RealmStudioFileFormat.MapRecoveryFileExtension, SearchOption.TopDirectoryOnly).ToList();

                foreach (var file in files)
                {
                    if (file.Contains(projectId))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                RealmStudioXLogger.Exception("Error removing recovery files", ex);
            }
        }

        internal List<RecoveryPackage> GetRecoveryPackages(RealmStudioProject project)
        {
            List<RecoveryPackage> recoveryPackages = [];

            string projectId = project.Metadata!.ProjectId;

            // get crash packages first
            try
            {
                var files = Directory.EnumerateFiles(ApplicationPreferences.Current.AutosaveDirectory, "*" + RealmStudioFileFormat.MapCrashFileExtension, SearchOption.TopDirectoryOnly).ToList();

                foreach (var file in files)
                {
                    if (file.Contains(projectId))
                    {
                        string xml = File.ReadAllText(file);
                        RecoveryPackage package = MapFileMethods.DeserializeObject<RecoveryPackage>(xml);
                        recoveryPackages.Add(package);
                    }
                }
            }
            catch (Exception ex)
            {
                RealmStudioXLogger.Exception("Error getting recovery files", ex);
            }

            // then get recovery packages
            try
            {
                var files = Directory.EnumerateFiles(ApplicationPreferences.Current.AutosaveDirectory, "*" + RealmStudioFileFormat.MapRecoveryFileExtension, SearchOption.TopDirectoryOnly).ToList();

                foreach (var file in files)
                {
                    if (file.Contains(projectId))
                    {
                        string xml = File.ReadAllText(file);
                        RecoveryPackage package = MapFileMethods.DeserializeObject<RecoveryPackage>(xml);
                        recoveryPackages.Add(package);
                    }
                }
            }
            catch (Exception ex)
            {
                RealmStudioXLogger.Exception("Error getting recovery files", ex);
            }

            return recoveryPackages;
        }

        private void SaveTimer_Tick(object? sender, EventArgs e)
        {
            if (_selectedMap != null && !string.IsNullOrEmpty(ApplicationPreferences.Current.AutosaveDirectory) && _selectedProject != null)
            {
                try
                {
                    if (_hasUnsavedChanges)
                    {
                        WriteRecoveryPackage(_selectedProject, _selectedMap);

                        AutosaveCompleted?.Invoke(this, EventArgs.Empty);

                        RealmStudioXLogger.Info($"Realm Project {_selectedProject.Metadata!.ProjectId} Map {_selectedMap.MapId} autosaved at {DateTime.Now}");
                    }
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception($"Realm Project {_selectedProject.Metadata!.ProjectId} Map {_selectedMap.MapId} autosave failed", ex);
                }
            }
        }

        public void WriteRecoveryPackage(RealmStudioProject project, RealmStudioMap map)
        {
            if (!Path.Exists(ApplicationPreferences.Current.AutosaveDirectory))
            {
                Directory.CreateDirectory(ApplicationPreferences.Current.AutosaveDirectory);
            }

            RecoveryPackage package = BuildRecoveryPackage(project, map);
            string xml = MapFileMethods.SerializeObject(package);

            // atomic write of recovery package file
            string tempFile = autosavePath + ".tmp";

            File.WriteAllText(tempFile, xml);

            File.Move(tempFile, autosavePath, true);
        }

        public void WriteCrashPackage()
        {
            try
            {
                if (!Path.Exists(ApplicationPreferences.Current.AutosaveDirectory))
                {
                    Directory.CreateDirectory(ApplicationPreferences.Current.AutosaveDirectory);
                }

                if (_selectedProject != null && _selectedMap != null)
                {
                    RecoveryPackage package = BuildRecoveryPackage(_selectedProject, _selectedMap);
                    string xml = MapFileMethods.SerializeObject(package);

                    string crashPath = Path.Combine(ApplicationPreferences.Current.AutosaveDirectory, _selectedProject.Metadata!.ProjectId + "_"
                        + _selectedMap.MapId
                        + RealmStudioFileFormat.MapCrashFileExtension);

                    // atomic write of crash package file
                    string tempFile = crashPath + ".tmp";

                    File.WriteAllText(tempFile, xml);

                    File.Move(tempFile, crashPath, true);
                }
            }
            catch (Exception ex)
            {
                RealmStudioXLogger.Exception("Crash package write failed", ex);
            }
        }

        internal void HasSavedChangesUpdate(object? sender, bool hasUnsavedChanges)
        {
            _hasUnsavedChanges = hasUnsavedChanges;
        }

        public void Start()
        {
            if (_saveTimer != null && _selectedMap != null)
            {
                _saveTimer.Start();
            }
        }

        public void Stop()
        {
            _saveTimer.Stop();
        }

        private RealmStudioProject? _selectedProject;


        private RealmStudioMap? _selectedMap = null;

        public RealmStudioProject? SelectedProject
        {
            get => _selectedProject;
            set => _selectedProject = value;
        }

        public RealmStudioMap? SelectedMap
        {
            get
            {
                return _selectedMap;
            }
            set
            {
                if (value != null && _selectedMap != value && _selectedProject != null)
                {
                    _selectedMap = value;
                    autosavePath = Path.Combine(ApplicationPreferences.Current.AutosaveDirectory, _selectedProject.Metadata!.ProjectId + "_"
                        + _selectedMap.MapId
                        + RealmStudioFileFormat.MapRecoveryFileExtension);

                    if (ApplicationPreferences.Current.AutoSaveEnabled)
                    {
                        Stop();
                        Start();
                    }
                }
            }
        }
       
        public int SaveInterval
        {
            get
            {
                return ApplicationPreferences.Current.AutosaveInterval;
            }
            set
            {
                if (ApplicationPreferences.Current.AutosaveInterval != value
                    && value >= ApplicationPreferences.Current.MinSaveInterval && value <= ApplicationPreferences.Current.MaxSaveInterval)
                {
                    ApplicationPreferences.Current.AutosaveInterval = value;
                    ApplicationPreferences.Current.Save();

                    _saveTimer.Interval = new TimeSpan(0, ApplicationPreferences.Current.AutosaveInterval, 0);

                    OnPropertyChanged(nameof(SaveInterval));
                }
            }
        }

        public bool AutoSaveEnabled
        {
            get
            {
                return ApplicationPreferences.Current.AutoSaveEnabled;
            }
            set
            {
                ApplicationPreferences.Current.AutoSaveEnabled = value;
                ApplicationPreferences.Current.Save();

                if (ApplicationPreferences.Current.AutoSaveEnabled && _selectedMap != null)
                {
                    Start();
                }
                else
                {
                    Stop();
                }

                OnPropertyChanged(nameof(AutoSaveEnabled));
            }
        }
    }

    public sealed class RecoveryPackage
    {
        [XmlAttribute]
        public int RecoveryPackageVersion { get; set; } = 1;

        public string ProjectId { get; set; } = string.Empty;

        public string ProjectName { get; set; } = string.Empty;

        public string ProjectPath { get; set; } = string.Empty;

        public RealmStudioMap Map { get; set; } = new();

        public DateTime Timestamp { get; set; }
    }
}
