using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.Properties;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Dialogs;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    public class AtmosphereViewModel(MainWindowViewModel mainViewModel, WorldGlobeViewer globeViewer) : ViewModelBase
    {
        private MainWindowViewModel _mainViewModel = mainViewModel;
        private WorldGlobeViewer _globeViewer = globeViewer;

        private string? _customCloudTexturePath;

        private bool _enableAtmosphere = false;
        public bool EnableAtmosphere
        {
            get => _enableAtmosphere;
            set
            {
                SetProperty(ref _enableAtmosphere, value);
            }
        }

        private bool _enableClouds = true;
        public bool EnableClouds
        {
            get => _enableClouds;
            set
            {
                if (SetProperty(ref _enableClouds, value))
                {
                    _globeViewer.RefreshCloudLayer();
                }
            }
        }

        public ICommand ToggleAtmosphere => new RelayCommand(() =>
        {
            _globeViewer.RefreshAtmosphere();
        });

        private Color _atmosphereColor = Colors.Transparent;
        public Color AtmosphereColor
        {
            get => _atmosphereColor;
            set
            {
                if (SetProperty(ref _atmosphereColor, value))
                {
                    _atmosphereColorBrush.Color = value;
                    _globeViewer.RefreshAtmosphere();
                }
            }
        }

        private readonly SolidColorBrush _atmosphereColorBrush = new(Colors.Transparent);
        public Brush AtmosphereColorBrush => _atmosphereColorBrush;

        private double _atmosphereDensity = 0.01;

        public double AtmosphereDensity
        {
            get => _atmosphereDensity * 100.0;
            set
            {
                double density = Math.Clamp(value, 0.0, 100.0) / 100.0;

                if (SetProperty(ref _atmosphereDensity, density))
                {
                    _globeViewer.RefreshAtmosphere();
                }
            }
        }

        private double _atmosphereAltitude = 0.001;
        public double AtmosphereAltitude
        {
            get => _atmosphereAltitude * 1000.0;
            set
            {
                double altitude = value / 1000.0;
                if (SetProperty(ref _atmosphereAltitude, altitude))
                {
                    _globeViewer.RefreshAtmosphere();
                }
            }
        }

        private bool _useDefaultCloudTexture = false;

        public bool UseDefaultCloudTexture
        {
            get => _useDefaultCloudTexture;
            set
            {
                if (SetProperty(ref _useDefaultCloudTexture, value) && value)
                {
                    _globeViewer.LoadDefaultCloudTexture();
                    _globeViewer.RefreshCloudLayer();
                }
            }
        }

        private bool _useCustomCloudTexture;

        public bool UseCustomCloudTexture
        {
            get => _useCustomCloudTexture;
            set
            {
                if (SetProperty(ref _useCustomCloudTexture, value) && value)
                {
                    LoadCustomCloudTexture();
                    _globeViewer.RefreshCloudLayer();
                }
            }
        }

        private Color _cloudColor = Colors.Transparent;
        public Color CloudColor
        {
            get => _cloudColor;
            set
            {
                if (SetProperty(ref _cloudColor, value))
                {
                    _cloudColorBrush.Color = value;
                    _globeViewer.RefreshCloudLayer();
                }
            }
        }

        private readonly SolidColorBrush _cloudColorBrush = new(Colors.Transparent);
        public Brush CloudColorBrush => _cloudColorBrush;

        private double _cloudOpacity = 0.4;
        public double CloudOpacity
        {
            get => _cloudOpacity;
            set
            {
                if (SetProperty(ref _cloudOpacity, value))
                {
                    _globeViewer.RefreshCloudLayer();
                }
            }
        }

        private double _cloudRotationRate = 1.0;
        public double CloudRotationRate
        {
            get => _cloudRotationRate;
            set
            {
                if (SetProperty(ref _cloudRotationRate, value))
                {
                    _globeViewer.RefreshCloudLayer();
                }
            }
        }

        public ICommand SelectCloudTextureCommand => new RelayCommand(() =>
        {
            string cloudsDirectory = Path.Combine(ApplicationPreferences.Current.RootAssetDirectory, "Textures", "Clouds");

            OpenFileDialog dialog = new()
            {
                Title = "Select Cloud Layer Image File",
                Filter = UserInterfaceUtilities.GetCommonImageFilter(),
                Multiselect = false,
                InitialDirectory = cloudsDirectory,
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                _customCloudTexturePath = dialog.FileName;
                UseCustomCloudTexture = true;

                string? directory = Path.GetDirectoryName(dialog.FileName);

                if (!string.IsNullOrEmpty(directory))
                {
                    Settings.Default.LastFileDirectory = directory;
                }
            }
            catch (Exception ex)
            {
                RealmStudioXLogger.Exception("Error loading cloud texture", ex);
                MessageDialog dlg = MessageDialogFactory.ErrorDialog("Cloud Texture", $"Unable to load the cloud texture.\n\n{ex.Message}");
                dlg.ShowDialog();
            }
        });

        private void LoadCustomCloudTexture()
        {
            if (!string.IsNullOrWhiteSpace(_customCloudTexturePath) && File.Exists(_customCloudTexturePath))
            {
                _globeViewer.LoadCloudTexture(_customCloudTexturePath);
            }
        }
    }
}
