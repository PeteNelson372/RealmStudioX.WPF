using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.Views.Dialogs;
using Application = System.Windows.Application;
using Point = System.Windows.Point;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    internal static class WindowBuilder
    {
        public static ColorQuickPick BuildColorQuickPick(Color initialColor, System.Windows.Window window, Button sender)
        {
            WindowManager wm = ((App)Application.Current).WindowManager;

            ColorQuickPick dialog = wm.GetOrCreate<ColorQuickPick>();
            dialog.Owner = window;
            dialog.InitialColor = initialColor;

            UserInterfaceUtilities.PositionWindowRelativeToControl(
                dialog,
                sender,
                new Point(0, (int)((Button)sender).ActualHeight),
                0,
                0);

            return dialog;
        }

        public static ColorSelectionDialog BuildColorSelectionDialog(Color initialColor, System.Windows.Window window)
        {
            WindowManager wm = ((App)Application.Current).WindowManager;

            ColorSelectionDialog colorSelectionDialog = wm.GetOrCreate<ColorSelectionDialog>();

            colorSelectionDialog.Owner = window;
            colorSelectionDialog.InitialColor = initialColor;

            return colorSelectionDialog;
        }
    }
}
