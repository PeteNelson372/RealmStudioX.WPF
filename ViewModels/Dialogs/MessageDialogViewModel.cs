namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    using MaterialDesignThemes.Wpf;
    using RealmStudioShapeRenderingLib;
    using RealmStudioX.WPF.ViewModels.Infrastructure;
    using System.Windows;
    using System.Windows.Input;

    public sealed class MessageDialogViewModel : ViewModelBase
    {
        public MessageDialogResult Result { get; private set; }

        private string _dialogTitle = string.Empty;
        public string DialogTitle
        {
            get => _dialogTitle;
            set => SetProperty(ref _dialogTitle, value);
        }

        private string _message = string.Empty;
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        private PackIconKind _dialogIcon = PackIconKind.Information;
        public PackIconKind DialogIcon
        {
            get => _dialogIcon;
            set => SetProperty(ref _dialogIcon, value);
        }

        private Brush _iconBrush = Brushes.SteelBlue;
        public Brush IconBrush
        {
            get => _iconBrush;
            set => SetProperty(ref _iconBrush, value);
        }

        private string _primaryButtonText = "OK";
        public string PrimaryButtonText
        {
            get => _primaryButtonText;
            set => SetProperty(ref _primaryButtonText, value);
        }

        private MessageDialogResult _primaryResult = MessageDialogResult.OK;

        public MessageDialogResult PrimaryResult
        {
            get => _primaryResult;
            set => SetProperty(ref _primaryResult, value);
        }

        private string _secondaryButtonText = "Cancel";
        public string SecondaryButtonText
        {
            get => _secondaryButtonText;
            set => SetProperty(ref _secondaryButtonText, value);
        }

        private MessageDialogResult _secondaryResult = MessageDialogResult.Cancel;

        public MessageDialogResult SecondaryResult
        {
            get => _secondaryResult;
            set => SetProperty(ref _secondaryResult, value);
        }

        private Visibility _secondaryButtonVisibility = Visibility.Collapsed;

        public Visibility SecondaryButtonVisibility
        {
            get => _secondaryButtonVisibility;
            set => SetProperty(ref _secondaryButtonVisibility, value);
        }

        private string _tertiaryButtonText = "";
        public string TertiaryButtonText
        {
            get => _tertiaryButtonText;
            set => SetProperty(ref _tertiaryButtonText, value);
        }

        private MessageDialogResult _tertiaryResult = MessageDialogResult.Abort;

        public MessageDialogResult TertiaryResult
        {
            get => _tertiaryResult;
            set => SetProperty(ref _tertiaryResult, value);
        }

        private Visibility _tertiaryButtonVisibility = Visibility.Collapsed;

        public Visibility TertiaryButtonVisibility
        {
            get => _tertiaryButtonVisibility;
            set => SetProperty(ref _tertiaryButtonVisibility, value);
        }

        private bool _isDestructive;
        public bool IsDestructive
        {
            get => _isDestructive;
            set => SetProperty(ref _isDestructive, value);
        }

        public ICommand PrimaryCommand { get; }

        public ICommand SecondaryCommand { get; }

        public ICommand TertiaryCommand { get; }

        public MessageDialogViewModel(Window owner)
        {
            PrimaryCommand = new RelayCommand(() =>
            {
                Result = PrimaryResult;
                owner.Close();
            });

            SecondaryCommand = new RelayCommand(() =>
            {
                Result = SecondaryResult;
                owner.Close();
            });

            TertiaryCommand = new RelayCommand(() =>
            {
                Result = TertiaryResult;
                owner.Close();
            });
        }
    }
}
