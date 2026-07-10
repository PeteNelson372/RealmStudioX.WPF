using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.Models.Startup;
using RealmStudioX.WPF.ViewModels.Dialogs;
using SkiaSharp;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for CreateOpenMapDialog.xaml
    /// </summary>
    public partial class CreateOpenMapDialog : ModalDialog, INotifyPropertyChanged
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        private readonly string _themesFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RealmStudioX", "Assets", "Themes");

        private readonly string _mapsFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RealmStudioX", "Realms");

        public CreateOpenPackageResult? Result { get; private set; }

        private double _aspectRatio = 1920.0 / 1080.0;
        private bool _lockAspect = true;

        public CreateOpenMapViewModel ViewModel { get; }

        public CreateOpenMapDialog()
        {
            InitializeComponent();

            ViewModel = new CreateOpenMapViewModel(_mapsFolder, _themesFolder);
            ViewModel.RequestClose += OnRequestClose;

            DataContext = ViewModel;

            MapTypeList.ItemsSource = new List<MapTypeOption>
            {
                new() { Name="World",               Icon="Earth",        MapType = RealmMapType.World },
                new() { Name="Region",              Icon="Map",          MapType = RealmMapType.Region },
                new() { Name="City",                Icon="City",         MapType = RealmMapType.City },
                new() { Name="Interior Floor",      Icon="HomeCity",     MapType = RealmMapType.InteriorFloor },
                new() { Name="Dungeon Level",       Icon="Layers",       MapType = RealmMapType.DungeonLevel },
                new() { Name="Ship Deck",           Icon="Ferry",        MapType = RealmMapType.ShipDeck },
                new() { Name="Solar System Body",   Icon="Orbit",        MapType = RealmMapType.SolarSystemBody },
            };

            RealmTypeList.ItemsSource = new List<RealmTypeOption>
            {
                new() { Name="World",    Icon="Earth",        RealmProjectType = RealmProjectType.World },
                new() { Name="Region",   Icon="Map",          RealmProjectType = RealmProjectType.Region  },
                new() { Name="City",     Icon="City",         RealmProjectType = RealmProjectType.City  },
                new() { Name="Dungeon",  Icon="ViewList",     RealmProjectType = RealmProjectType.Dungeon },
                new() { Name="Ship",     Icon="ViewList",     RealmProjectType = RealmProjectType.Ship  },
                new() { Name="Interior", Icon="ViewList",     RealmProjectType = RealmProjectType.Interior },
            };

            PresetList.ItemsSource = new List<MapSizePreset>
            {
                new() { Width = 1024, Height = 768, Display = "1024 x 768 (XGA)" },
                new() { Width = 1280, Height = 720, Display = "1280 x 720 (720P)" },
                new() { Width = 1280, Height = 1024, Display = "1280 x 1024 (SXGA)" },
                new() { Width = 1600, Height = 1200, Display = "1600 x 1200 (UXGA)" },
                new() { Width = 1920, Height = 1080, Display = "1920 x 1080 (1080P Full HD)", IsSelected = true },
                new() { Width = 2560, Height = 1080, Display = "2560 x 1080 (2K)" },
                new() { Width = 2048, Height = 1024, Display = "2048 x 1024 (Equirectangular 2K)" },
                new() { Width = 3840, Height = 2160, Display = "3840 x 2160 (4K Ultra HD)" },
                new() { Width = 4096, Height = 2048, Display = "4096 x 2048 (Equirectangular 4K)" },
                new() { Width = 3300, Height = 2250, Display = "3300 x 2250 (US Letter 300 DPI)" },
                new() { Width = 1754, Height = 1240, Display = "1754 x 1240 (A6 300 DPI)" },
                new() { Width = 2480, Height = 1754, Display = "2480 x 1754 (A5 300 DPI)" },
                new() { Width = 3508, Height = 2480, Display = "3508 x 2480 (A4 300 DPI)" },
                new() { Width = 4960, Height = 3508, Display = "4960 x 3508 (A3 300 DPI)" },
                new() { Width = 7016, Height = 4960, Display = "7016 x 4960 (A2 300 DPI)" },
                new() { Width = 7680, Height = 4320, Display = "7680 x 4320 (8K UHD)" },
                new() { Width = 8192, Height = 4096, Display = "8192 x 4096 (Equirectangular 8K)" },
                new() { Width = 10000, Height = 10000, Display = "10000 x 10000 (Maximum)" }
            };

            AreaWidthBox.ValueChanged += AreaWidthBox_ValueChanged;

            WidthBox.ValueChanged += (s, e) =>
            {
                if (_lockAspect && HeightBox.Value > 0)
                {
                    var newHeight = Math.Round(WidthBox.Value / _aspectRatio);
                    HeightBox.SetValueSilently(newHeight);
                }

                UpdateAspect();

                ViewModel.Width = (int)WidthBox.Value;
            };

            HeightBox.ValueChanged += (s, e) =>
            {
                if (_lockAspect && WidthBox.Value > 0)
                {
                    var newWidth = Math.Round(HeightBox.Value * _aspectRatio);
                    WidthBox.SetValueSilently(newWidth);
                }

                UpdateAspect();

                ViewModel.Height = (int)HeightBox.Value;
            };

            LoadExistingMapProjects();
        }

        private void LoadExistingMapProjects()
        {
            string realmDir = _mapsFolder;

            if (!Directory.Exists(realmDir))
            {
                return;
            }

            ViewModel.ProjectListEntries.Clear();

            string searchPath = "*" + RealmStudioFileFormat.PackageExtension;

            List<RealmStudioProject> projects = [];

            foreach (string file in Directory.EnumerateFiles(
                realmDir,
                searchPath,
                SearchOption.TopDirectoryOnly))
            {
                try
                {
                    RealmStudioProject project = MapFileMethods.OpenProject(file);
                    projects.Add(project);
                }
                catch (Exception ex)
                {
                    string msg = $"An error occurred while reading the project '{Path.GetFileName(file)}'. Check the log file for details.";
                    
                    if (File.Exists(file + RealmStudioFileFormat.BackupFileExtension))
                    {
                        msg += " There is a backup file for the project. You may be able to recover the project from the backup file.";
                    }

                    RealmStudioXLogger.Exception("Error opening project.", ex);
                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error Opening Project", msg);
                    dlg.ShowDialog();
                }
            }

            foreach (var mapProject in projects
                .OrderByDescending(m => m.Metadata.Modified)
                .ToList())
            {
                // get the preview for the active map
                string activeMapId = mapProject.ActiveMapId;

                SKBitmap preview = new();

                foreach (MapProjectEntry entry in mapProject.Maps)
                {
                    if (entry.MapId == activeMapId)
                    {
                        if (entry.Preview != null)
                        {
                            preview = entry.Preview;
                        }
                        break;
                    }
                }

                // populate in sorted order
                ProjectListEntry projectListEntry = new()
                {
                    Name = mapProject.Metadata.ProjectName,
                    ProjectPath = mapProject.Metadata.ProjectFilePath,
                    RealmType = mapProject.Metadata.RealmType.GetDescription() ?? mapProject.Metadata.RealmType.ToString(),
                    LastModified = mapProject.Metadata.Modified,
                    Project = mapProject,
                    NumberMaps = mapProject.Maps.Count,
                    PreviewImage = preview.ToImageSource(),
                };

                ViewModel.ProjectListEntries.Add(projectListEntry);
            }
        }       

        private void OnRequestClose(bool? dialogResult)
        {
            DialogResult = dialogResult;
            Close();
        }

        private void ProjectListItem_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item &&
                 DataContext is CreateOpenMapViewModel vm &&
                 item.DataContext is ProjectListEntry entry)
            {
                vm.Result = new CreateOpenPackageResult
                {
                    CreationOperation = RealmCreationOperation.CreateProject,
                    IsNew = false,
                    Project = entry.Project
                };

                DialogResult = true;
                Close();
            }
        }

        private void AreaWidthBox_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (AreaWidthBox.Value > 0)
            {
                var areaHeight = AreaWidthBox.Value / _aspectRatio;
                AreaHeightText.Text = areaHeight.ToString("0");

                ViewModel.AreaWidth = (float)AreaWidthBox.Value;
                ViewModel.AreaHeight = (float)areaHeight;
            }
        }

        private void OnPresetSelected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is System.Windows.Controls.RadioButton rb && rb.DataContext is MapSizePreset preset)
            {
                WidthBox.SetValue(preset.Width, false);
                HeightBox.SetValue(preset.Height, false);

                UpdateAspect();

                ViewModel.Width = (int)WidthBox.Value;
                ViewModel.Height = (int)HeightBox.Value;
            }
        }

        private void UpdateAspect()
        {
            if (WidthBox.Value > 0 && HeightBox.Value > 0)
            {
                _aspectRatio = WidthBox.Value / HeightBox.Value;
                AspectRatioText.Text = _aspectRatio.ToString("0.00");
            }
        }

        private void OnToggleAspectLock(object sender, RoutedEventArgs e)
        {
            _lockAspect = !_lockAspect;
        }

        private void OnSwapDimensions(object sender, RoutedEventArgs e)
        {
            WidthBox.Commit();
            HeightBox.Commit();

            var w = WidthBox.Value;
            var h = HeightBox.Value;

            WidthBox.SetValue(h, false);
            HeightBox.SetValue(w, false);

            UpdateAspect();

            ViewModel.Width = (int)WidthBox.Value;
            ViewModel.Height = (int)HeightBox.Value;
        }

        private void OnBrowse(object sender, RoutedEventArgs e)
        {

        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MapTypeOption
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public RealmMapType MapType { get; set; } = RealmMapType.World;
        public bool IsSelected { get; set; }
    }

    public class RealmTypeOption
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public RealmProjectType RealmProjectType { get; set; } = RealmProjectType.World;
        public bool IsSelected { get; set; }
    }

    public class MapSizePreset
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Display { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public sealed class ProjectListEntry
    {
        public string Name { get; set; } = string.Empty;
        public string ProjectPath { get; set; } = string.Empty;
        public string RealmType { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public int NumberMaps { get; set; }
        public RealmStudioProject? Project { get; set; }
        public ImageSource? PreviewImage { get; set; }
    }
}
