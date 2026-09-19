using RealmStudioShapeRenderingLib;
using RealmStudioX._3D.Models;
using RealmStudioX.WPF.Editor.Tools;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.ViewModels.Main;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for HeightMapDXModelViewer.xaml
    /// </summary>
    public partial class HeightMapDXModelViewer : ModelessDialog, INotifyPropertyChanged
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public MainWindowViewModel MainViewModel;
        public HeightMapDXViewModel ViewModel { get; private set; }

        public event EventHandler? OpenClicked;
        public event EventHandler? SaveClicked;
        public event EventHandler? MinimizeClicked;
        public event EventHandler? MaximizeClicked;
        public event EventHandler? ExitClicked;

        private HeightMapTerrain3D? _terrain;

        public HeightMapTerrain3D? Terrain => _terrain;

        public HeightMapDXModelViewer(MainWindowViewModel mainViewModel, MapHeightMap heightMap, float minimumElevation, float maximumElevation, float elevationScale)
        {
            InitializeComponent();

            MainViewModel = mainViewModel;
            ViewModel = new HeightMapDXViewModel(this, MainViewModel.Editor.Scene!.Map);

            DataContext = ViewModel;

            HeightMapDXTitleBar.DataContext = ViewModel;

            HeightMapDXMenu.DataContext = ViewModel;

            SizeChanged += (s, e) => OnWindowSizeChanged(ActualWidth, ActualHeight);

            Loaded += (s, e) =>
            {
                HeightMapDXTitleBar.MinimizeClicked += (s, e) => MinimizeHandler();
                HeightMapDXTitleBar.MaximizeClicked += (s, e) => MaximizeHandler();
                HeightMapDXTitleBar.ExitClicked += (s, e) => ExitHandler();

                HeightMapDXMenu.ExitClicked += (s, e) => ExitHandler();

                if (heightMap.HeightMap != null)
                {
                    Func<int, int, bool> _isInsideLandform;

                    if (MainViewModel.Editor.ActiveEditorTool is HeightMapTool hmt)
                    {
                        _isInsideLandform = hmt.IsInsideLandform;

                        _terrain = new HeightMapTerrain3D();

                        _terrain.Create(heightMap, minimumElevation, maximumElevation, elevationScale, _isInsideLandform);

                        if (_terrain.Model != null)
                        {
                                hmt.ActiveTerrain = _terrain;

                            ModelViewer.Viewport3D.Items.Add(_terrain.Model);

                            ModelViewer.ModelBounds = _terrain.Bounds;

                            ModelViewer.FitModel(_terrain.Bounds);
                        }
                    }
                }

                OnWindowSizeChanged(ActualWidth, ActualHeight);
            };
        }

        public void OnWindowSizeChanged(double width, double height)
        {
            ModelViewer.Width = width - 20;  // account for the width of the margins
            ModelViewer.Height = height - 68; // account for the height of the title bar, menu, and margins
        }

        private void ExitHandler()
        {
            Close();
        }

        private void MaximizeHandler()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void MinimizeHandler()
        {
            WindowState = WindowState.Minimized;
        }


        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
