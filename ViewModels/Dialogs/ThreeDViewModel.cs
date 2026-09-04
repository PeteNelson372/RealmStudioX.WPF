using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX._3D.Views.Controls;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows.Input;
using Cursors = System.Windows.Input.Cursors;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    public class ThreeDViewModel(ThreeDModelViewer modelViewer) : ViewModelBase
    {
        private ModelViewerControl _modelViewer = modelViewer.ModelViewer;

        public ICommand OpenModelCommand => new RelayCommand(() =>
        {
            LoadModel();
        });

        public ICommand ResetCameraCommand => new RelayCommand(() =>
        {
            _modelViewer.ResetCamera();
        });


        private void LoadModel()
        {
            try
            {
                OpenFileDialog ofd = new()
                {
                    Title = "Open 3D Model",
                    DefaultExt = "obj",
                    Filter = "3D Model files|*.obj;*.stl;*.3ds;*.lwo;*.off|OBJ files (*.obj)|*.obj|All files (*.*)|*.*",
                    CheckFileExists = true,
                    RestoreDirectory = true,
                    Multiselect = false
                };

                if (ofd.ShowDialog() == true)
                {
                    if (ofd.FileName != "")
                    {
                        Mouse.OverrideCursor = Cursors.Wait;
                        _modelViewer.LoadModel(ofd.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                RealmStudioXLogger.Error(ex.Message);
                MessageBox.Show("Error loading model: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

        }
    }
}
