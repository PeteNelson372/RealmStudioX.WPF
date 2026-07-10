using RealmStudioX.WPF.Views.Dialogs;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    internal static class WindowBuilder
    {
        public static ColorQuickPick BuildColorQuickPick(Color initialColor, System.Windows.Window window, Button button)
        {
            WindowManager wm = ((App)Application.Current).WindowManager;

            ColorQuickPick dialog = wm.Create<ColorQuickPick>();
            dialog.Owner = window;
            dialog.InitialColor = initialColor;

            App app = (App)Application.Current;

            wm.AttachToControl(
                dialog,
                app.MainWindow,
                button,
                new Point(0, button.ActualHeight),
                0,
                0);

            return dialog;
        }

        public static ColorSelectionDialog BuildColorSelectionDialog(Color initialColor, System.Windows.Window window)
        {
            WindowManager wm = ((App)Application.Current).WindowManager;

            ColorSelectionDialog colorSelectionDialog = wm.Create<ColorSelectionDialog>();

            colorSelectionDialog.Owner = window;
            colorSelectionDialog.InitialColor = initialColor;

            return colorSelectionDialog;
        }
    }
}
