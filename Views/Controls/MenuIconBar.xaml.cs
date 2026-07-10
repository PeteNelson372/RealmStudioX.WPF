using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Controls
{
    /// <summary>
    /// Interaction logic for MenuIconBar.xaml
    /// </summary>
    public partial class MenuIconBar : System.Windows.Controls.UserControl
    {
        SelectionFilterDialog? filterDialog = null;

        public MenuIconBar()
        {
            InitializeComponent();
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (filterDialog == null)
            {
                App app = (App)Application.Current;

                WindowManager wm = app.WindowManager;

                Button button = (Button)sender;

                filterDialog = wm.Create<SelectionFilterDialog>();

                filterDialog.Closed += (_, _) =>
                {
                    filterDialog = null;
                };

                if (filterDialog.DataContext == null)
                {
                    filterDialog.DataContext =
                        ((MainWindowViewModel)app.MainWindow.DataContext).SelectionService;

                    wm.AttachToControl(
                        filterDialog,
                        app.MainWindow,
                        button,
                        new Point(0, button.ActualHeight),
                        8,
                        22);
                }

                wm.Show(filterDialog);
            }
            else
            {
                filterDialog.Close();
            }
        }
    }
}
