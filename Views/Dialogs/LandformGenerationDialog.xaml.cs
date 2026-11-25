using RealmStudioShapeRenderingLib;
using RealmStudioX.WorldGeneration.LandformGeneration;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Dialogs;
using SkiaSharp;

namespace RealmStudioX.WPF.Views.Dialogs
{
    public partial class LandformGenerationDialog : ModalDialog
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public LandformGenerationViewModel ViewModel { get; }

        public LandformGenerationResult? Result => ViewModel.Result;

        public LandformGenerationDialog(
            EditorController editor,
            RealmStudioMap map,
            SKRect selectedArea)
        {
            InitializeComponent();

            ViewModel = new LandformGenerationViewModel(
                editor,
                map,
                selectedArea);

            ViewModel.RequestClose += OnRequestClose;

            DataContext = ViewModel;
        }

        private void OnRequestClose(bool? dialogResult)
        {
            DialogResult = dialogResult;
            Close();
        }
    }
}