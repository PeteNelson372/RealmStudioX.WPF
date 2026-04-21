using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using System.Windows.Input;

namespace RealmStudioX.WPF.ViewModels.Main
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly EditorController _editor;

        public MainWindowViewModel(EditorController editor)
        {
            _editor = editor;

            SelectLandformToolCommand = new RelayCommand(SelectLandformTool);
            SelectBackgroundToolCommand = new RelayCommand(SelectBackgroundTool);

            // Example UI state
            MapName = "Default";
        }

        // -------------------------
        // UI State
        // -------------------------

        private string _mapName = "";
        public string MapName
        {
            get => _mapName;
            set => SetProperty(ref _mapName, value);
        }

        // You can add more later:
        // public double Zoom { get; set; }
        // public string SelectedLayer { get; set; }

        // -------------------------
        // Commands (buttons)
        // -------------------------

        public ICommand SelectLandformToolCommand { get; }
        public ICommand SelectBackgroundToolCommand { get; }

        private void SelectLandformTool()
        {
            //_editor.SetTool(new PaintLandformTool());
        }

        private void SelectBackgroundTool()
        {
            //_editor.SetTool(new EraseTool());
        }
    }
}
