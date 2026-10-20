using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor.Services;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Main;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ApplicationPreferencesDialog.xaml
    /// </summary>
    public partial class ApplicationPreferencesDialog : ModalDialog, INotifyPropertyChanged
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();
        private bool _initializing;

        public ApplicationPreferencesDialog(MainWindowViewModel mainWindowViewModel)
        {
            InitializeComponent();

            DataContext = ApplicationPreferences.Current;

            InitializePresets();

            Closed += (_, _) =>  ApplicationPreferences.Current.Save();

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PresetList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing || PresetList.SelectedItem is not MapSizePreset preset)
            {
                return;
            }

            IApplicationPreferences preferences = ApplicationPreferences.Current;
            
            preferences.DefaultMapWidth = preset.Width;
            preferences.DefaultMapHeight = preset.Height;
        }

        private void ExportFormats_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing || ExportFormats.SelectedItem is not string exportFormat || string.IsNullOrEmpty(exportFormat))
            {
                return;
            }

            IApplicationPreferences preferences = ApplicationPreferences.Current;

            preferences.DefaultExportFormat = exportFormat;
        }

        private void SaveSound_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing || SaveSoundFiles.SelectedItem is not string soundFile || string.IsNullOrEmpty(soundFile))
            {
                return;
            }

            IApplicationPreferences preferences = ApplicationPreferences.Current;

            preferences.SelectedSaveSound = soundFile;
        }

        private void InitializePresets()
        {
            IApplicationPreferences preferences =  ApplicationPreferences.Current;

            _initializing = true;

            try
            {
                List<MapSizePreset> presets = CreateMapSizePresets();

                PresetList.ItemsSource = presets;

                PresetList.SelectedItem = presets.FirstOrDefault(p =>
                        p.Width == preferences.DefaultMapWidth &&
                        p.Height == preferences.DefaultMapHeight)
                    ?? presets.FirstOrDefault(p => p.IsSelected)
                    ?? presets.FirstOrDefault();

                List<string> exportFormats = CreateMapExportFormats();

                ExportFormats.ItemsSource = exportFormats;
                string defaultFormat = ApplicationPreferences.Current.DefaultExportFormat;

                ExportFormats.SelectedItem = string.IsNullOrEmpty(defaultFormat)
                    ? exportFormats.FirstOrDefault()
                    : exportFormats.FirstOrDefault(
                        format => string.Equals(
                            format,
                            defaultFormat,
                            StringComparison.OrdinalIgnoreCase))
                      ?? exportFormats.FirstOrDefault();


                List<string> saveSounds = CreateSaveSoundList();

                SaveSoundFiles.ItemsSource = saveSounds;

                string selectedSound = ApplicationPreferences.Current.SelectedSaveSound;

                SaveSoundFiles.SelectedItem = string.IsNullOrEmpty(selectedSound)
                    ? saveSounds.FirstOrDefault()
                    : saveSounds.FirstOrDefault(
                        sound => string.Equals(
                            sound,
                            selectedSound,
                            StringComparison.OrdinalIgnoreCase))
                      ?? saveSounds.FirstOrDefault();
            }
            finally
            {
                _initializing = false;
            }
        }

        private void PlaySaveSoundClick(object sender, RoutedEventArgs e)
        {
            string selectedSound = (string)SaveSoundFiles.SelectedItem;

            if (!string.IsNullOrEmpty(selectedSound) && Path.GetExtension(selectedSound).ToLowerInvariant() == ".wav")
            {
                SoundService.PlaySound(selectedSound);
            }
        }

        private static List<MapSizePreset> CreateMapSizePresets()
        {
            return
            [
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
            ];
        }

        private static List<string> CreateMapExportFormats()
        {
            return
            [
                "PNG",
                "JPG",
                "BMP",
                "WEBP"
            ];
        }

        private static List<string> CreateSaveSoundList()
        {
            string soundFolder = Path.Combine(AppContext.BaseDirectory, "Assets", "sounds");

            string?[] soundNames = [.. Directory.GetFiles(soundFolder, "*.wav").Select(Path.GetFileName)];

            List<string> sounds = [];

            if (soundNames != null)
            {
                foreach(var sound in soundNames)
                {
                    if (sound != null)
                    {
                        sounds.Add(sound);
                    }
                }

                return sounds;
            }
            else
            {
                return [];
            }

        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
