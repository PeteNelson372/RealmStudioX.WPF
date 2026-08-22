using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Controls
{
    /// <summary>
    /// Interaction logic for LandformControls.xaml
    /// </summary>
    public partial class LandformControls : System.Windows.Controls.UserControl
    {
        private LandformPanelViewModel ViewModel => (LandformPanelViewModel)DataContext;

        public LandformControls()
        {
            InitializeComponent();
        }

        private void SelectGeneratedLandformType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag != null && btn.Tag is GeneratedLandformType type)
            {
                ViewModel.SelectedLandformType = type;
            }

            DropDownButton.IsChecked = false;
        }
    }
}
