using MaterialDesignThemes.Wpf;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using RealmStudioShapeRenderingLib;
namespace RealmStudioX.WPF.Editor.UserInterface
{
    public static class MessageDialogFactory
    {
        public static MessageDialog InformationDialog(string title, string message)
        {
            MessageDialog dlg = new();

            MessageDialogViewModel vm = CreateInformationVM(dlg);
            vm.Message = message;
            vm.DialogTitle = title;

            dlg.DataContext = vm;

            return dlg;
        }

        private static MessageDialogViewModel CreateInformationVM(Window owner)
        {
            return new MessageDialogViewModel(owner)
            {
                DialogIcon = PackIconKind.Information,
                IconBrush = Brushes.SteelBlue,
                PrimaryButtonText = "OK",
                PrimaryResult = MessageDialogResult.OK
            };
        }

        public static MessageDialog WarningDialog(string title, string message)
        {
            MessageDialog dlg = new();

            MessageDialogViewModel vm = CreateWarningVM(dlg);
            vm.Message = message;
            vm.DialogTitle = title;

            dlg.DataContext = vm;

            return dlg;
        }

        private static MessageDialogViewModel CreateWarningVM(Window owner)
        {
            return new MessageDialogViewModel(owner)
            {
                DialogIcon = PackIconKind.Alert,
                IconBrush = Brushes.DarkOrange,
                PrimaryButtonText = "OK",
                PrimaryResult = MessageDialogResult.OK
            };
        }

        public static MessageDialog ErrorDialog(string title, string message)
        {
            MessageDialog dlg = new();

            MessageDialogViewModel vm = CreateErrorVM(dlg);
            vm.Message = message;
            vm.DialogTitle = title;

            dlg.DataContext = vm;

            return dlg;
        }

        private static MessageDialogViewModel CreateErrorVM(Window owner)
        {
            return new MessageDialogViewModel(owner)
            {
                DialogIcon = PackIconKind.AlertCircle,
                IconBrush = Brushes.Firebrick,
                PrimaryButtonText = "OK",
                PrimaryResult = MessageDialogResult.OK
            };
        }

        public static MessageDialog ConfirmationDialog(string title, string message)
        {
            MessageDialog dlg = new();

            MessageDialogViewModel vm = CreateConfirmationVM(dlg);
            vm.Message = message;
            vm.DialogTitle = title;

            dlg.DataContext = vm;

            return dlg;
        }

        private static MessageDialogViewModel CreateConfirmationVM(Window owner)
        {
            return new MessageDialogViewModel(owner)
            {
                DialogIcon = PackIconKind.HelpCircle,
                IconBrush = Brushes.SteelBlue,
                PrimaryButtonText = "Yes",

                PrimaryResult = MessageDialogResult.Yes,
                SecondaryButtonText = "No",
                SecondaryResult = MessageDialogResult.No,
                SecondaryButtonVisibility = Visibility.Visible
            };
        }

        public static MessageDialog DeleteConfirmationDialog(string title, string message)
        {
            MessageDialog dlg = new();

            MessageDialogViewModel vm = CreateDeleteConfirmationVM(dlg);
            vm.Message = message;
            vm.DialogTitle = title;

            dlg.DataContext = vm;

            return dlg;
        }

        private static MessageDialogViewModel CreateDeleteConfirmationVM(Window owner)
        {
            return new MessageDialogViewModel(owner)
            {
                DialogIcon = PackIconKind.Delete,
                IconBrush = Brushes.Firebrick,
                IsDestructive = true,
                PrimaryButtonText = "Delete",
                PrimaryResult = MessageDialogResult.Delete,
                SecondaryButtonText = "Cancel",
                SecondaryResult = MessageDialogResult.Cancel,
                SecondaryButtonVisibility = Visibility.Visible
            };
        }

        public static MessageDialog SaveConfirmationDialog(string title, string message)
        {
            MessageDialog dlg = new();

            MessageDialogViewModel vm = SaveConfirmationVM(dlg);
            vm.Message = message;
            vm.DialogTitle = title;

            dlg.DataContext = vm;

            return dlg;
        }

        private static MessageDialogViewModel SaveConfirmationVM(Window owner)
        {
            return new MessageDialogViewModel(owner)
            {
                DialogIcon = PackIconKind.ContentSaveCheckOutline,
                IconBrush = Brushes.DarkCyan,
                IsDestructive = false,
                PrimaryButtonText = "Yes",
                PrimaryResult = MessageDialogResult.Yes,
                SecondaryButtonText = "No",
                SecondaryResult = MessageDialogResult.No,
                SecondaryButtonVisibility = Visibility.Visible,
                TertiaryButtonText = "Cancel",
                TertiaryResult = MessageDialogResult.Cancel,
                TertiaryButtonVisibility = Visibility.Visible
            };
        }

        public static MessageDialog MapRecoveryDialog(string title, string message)
        {
            MessageDialog dlg = new();

            MessageDialogViewModel vm = MapRecoveryVM(dlg);
            vm.Message = message;
            vm.DialogTitle = title;

            dlg.DataContext = vm;

            return dlg;
        }

        private static MessageDialogViewModel MapRecoveryVM(Window owner)
        {
            return new MessageDialogViewModel(owner)
            {
                DialogIcon = PackIconKind.ContentSaveCheckOutline,
                IconBrush = Brushes.SlateBlue,
                IsDestructive = true,
                PrimaryButtonText = "Restore",
                PrimaryResult = MessageDialogResult.Restore,
                SecondaryButtonText = "Import as New",
                SecondaryResult = MessageDialogResult.Import,
                SecondaryButtonVisibility = Visibility.Visible,
                TertiaryButtonText = "Ignore",
                TertiaryResult = MessageDialogResult.Ignore,
                TertiaryButtonVisibility = Visibility.Visible
            };
        }
    }
}
