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
    /// Interaction logic for MenuIconBar.xaml
    /// </summary>
    public partial class AlignmentIconBar : System.Windows.Controls.UserControl
    {
        LayoutOptionsDialog? optionsDialog = null;

        public AlignmentIconBar()
        {
            InitializeComponent();
        }

        private void OpenLayoutOptionsButton_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm)
                return;

            if (optionsDialog == null)
            {
                WindowManager wm = ((App)Application.Current).WindowManager;
                App app = (App)Application.Current;

                optionsDialog = wm.Create<LayoutOptionsDialog>();
                optionsDialog.Owner = app.MainWindow;
                optionsDialog.DataContext = vm.Layout;

                optionsDialog.Closed += (_, _) =>
                {
                    optionsDialog = null;
                };

                wm.AttachToControl(
                    optionsDialog,
                    app.MainWindow,
                    OpenLayoutOptionsButton,
                    new Point(0, OpenLayoutOptionsButton.ActualHeight),
                    -optionsDialog.Width / 2,
                    0);

                wm.Show(optionsDialog);
            }
            else
            {
                optionsDialog.Close();
            }
        }
    }
}
