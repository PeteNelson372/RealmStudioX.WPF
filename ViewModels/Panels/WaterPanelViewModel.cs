using RealmStudioShapeRenderingLib;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using System.Windows.Input;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class WaterPanelViewModel : ViewModelBase, IWaterBodySettings
    {
        private readonly EditorController _editor;
        private readonly AssetManager _assetManager;

        public WaterPanelViewModel(EditorController editor, AssetManager assetManager)
        {
            _editor = editor;
            _assetManager = assetManager;
        }

        // commands
        public ICommand SelectCommand => new RelayCommand(() =>
        {

        });

        public ICommand WaterPaintCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.WaterPaint);
            _editor.ActivateTool(EditorToolType.WaterBodyTool, (IWaterBodySettings)this);
        });

        public ICommand CreateLakeCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.LakePaint);
            _editor.ActivateTool(EditorToolType.WaterBodyTool, (IWaterBodySettings)this);
        });

        public ICommand DrawRiverCommand => new RelayCommand(() =>
        {
            _editor.SetDrawingMode(MapDrawingMode.RiverPaint);
            _editor.ActivateTool(EditorToolType.WaterBodyTool, (IWaterBodySettings)this);
        });

        public ICommand EraseCommand => new RelayCommand(() =>
        {

        });
    }

    public interface IWaterBodySettings
    {

    }
}
