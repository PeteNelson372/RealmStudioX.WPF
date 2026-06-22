using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using System.IO;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace RealmStudioX.WPF.Editor.Services
{
    public class AutosaveService : ViewModelBase
    {
        private readonly DispatcherTimer _saveTimer = new();

        private readonly string autosaveRoot = string.Empty;

        private string autosavePath = string.Empty;

        private bool _hasUnsavedChanges = false;

        private readonly int _minimumAutosaveMinutes = 1;
        private readonly int _maximumAutosaveMinutes = 10;

        public event EventHandler? AutosaveCompleted;

        public AutosaveService(string autosaveRootDirectory)
        {
            autosaveRoot = autosaveRootDirectory;
            _saveTimer.Interval = new TimeSpan(0, _saveInterval, 0);
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
                var files = Directory.EnumerateFiles(autosaveRoot, "*" + RealmStudioFileFormat.MapRecoveryFileExtension, SearchOption.TopDirectoryOnly).ToList();

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

            try
            {
                var files = Directory.EnumerateFiles(autosaveRoot, "*" + RealmStudioFileFormat.MapRecoveryFileExtension, SearchOption.TopDirectoryOnly).ToList();

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
            if (_selectedMap != null && !string.IsNullOrEmpty(autosaveRoot) && _selectedProject != null)
            {
                try
                {
                    if (_hasUnsavedChanges)
                    {
                        if (!Path.Exists(autosaveRoot))
                        {
                            Directory.CreateDirectory(autosaveRoot);
                        }

                        RecoveryPackage package = BuildRecoveryPackage(_selectedProject, _selectedMap);
                        string xml = MapFileMethods.SerializeObject(package);

                        // atomic write of recovery package file
                        string tempFile = autosavePath + ".tmp";

                        File.WriteAllText(tempFile, xml);

                        File.Move(tempFile, autosavePath, true);

                        AutosaveCompleted?.Invoke(this, EventArgs.Empty);

                        RealmStudioXLogger.Info($"Realm Project {_selectedProject.Metadata!.ProjectId} Map {_selectedMap.MapId} autosaved at {DateTime.Now.ToString()}");
                    }
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception($"Realm Project {_selectedProject.Metadata!.ProjectId} Map {_selectedMap.MapId} autosave failed", ex);
                }
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
                    autosavePath = Path.Combine(autosaveRoot, _selectedProject.Metadata!.ProjectId + "_"
                        + _selectedMap.MapId
                        + RealmStudioFileFormat.MapRecoveryFileExtension);

                    if (_autosaveEnabled)
                    {
                        Stop();
                        Start();
                    }
                }
            }
        }

        private int _saveInterval = 2;  // default is 5 minutes between autosaves
        
        public int SaveInterval
        {
            get
            {
                return _saveInterval;
            }
            set
            {
                if (_saveInterval != value && value >= _minimumAutosaveMinutes && value <= _maximumAutosaveMinutes)
                {
                    _saveInterval = value;
                    _saveTimer.Interval = new TimeSpan(0, _saveInterval, 0);

                    OnPropertyChanged(nameof(SaveInterval));
                }
            }
        }

        private bool _autosaveEnabled;

        public bool AutoSaveEnabled
        {
            get
            {
                return _autosaveEnabled;
            }
            set
            {
                _autosaveEnabled = value;

                if (_autosaveEnabled && _selectedMap != null)
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
