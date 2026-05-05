using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Infrastructure;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class LabelsPanelViewModel : ViewModelBase, ILabelSettings
    {
        private readonly EditorController _editor;
        public EditorController Editor => _editor;

        private readonly AssetManager _assetManager;

        public LabelsPanelViewModel(EditorController editor, AssetManager assetManager)
        {
            _editor = editor;
            _assetManager = assetManager;


        }


    }

    public interface ILabelSettings
    {

    }
}
