using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.Views.Dialogs;
using Button = System.Windows.Controls.Button;
using Point = System.Windows.Point;
using Application = System.Windows.Application;

namespace RealmStudioX.WPF.Views.Controls
{
    /// <summary>
    /// Interaction logic for MenuIconBar.xaml
    /// </summary>
    public partial class MenuIconBar : System.Windows.Controls.UserControl
    {
        public MenuIconBar()
        {
            InitializeComponent();
        }

        private void FilterButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            WindowManager wm = ((App)Application.Current).WindowManager;

            SelectionFilterDialog dlg = wm.Toggle<SelectionFilterDialog>();

            if (wm.IsVisible<SelectionFilterDialog>())
            {
                UserInterfaceUtilities.PositionWindowRelativeToControl(
                    dlg,
                    (Button)sender,
                    new Point(0, (int)((Button)sender).ActualHeight),
                    8,
                    22);
            }
        }
    }
}
