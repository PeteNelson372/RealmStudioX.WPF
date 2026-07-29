using RealmStudioX.WPF.ViewModels.Infrastructure;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    public class ObjectCharacteristicsViewModel : ViewModelBase, IObjectCharacteristics
    {
        public event Action? RequestClose;

        public ObjectCharacteristicsViewModel()
        {
        }

        private string _selectedObjectType = string.Empty;

        public string SelectedObjectType
        {
            get => _selectedObjectType;
            set => _selectedObjectType = value;
        }

        private string _selectedCharacteristic = string.Empty;

        public string SelectedCharacteristic
        {
            get => _selectedCharacteristic;
            set => _selectedCharacteristic = value;
        }

        private ObservableCollection<string> _objectCharacteristicsList = [];

        public ObservableCollection<string> ObjectCharacteristicsList
        {
            get { return _objectCharacteristicsList; }
            set { _objectCharacteristicsList = value; }
        }

        public ICommand CloseObjectCharacteristicsCommand => new RelayCommand(() =>
        {
            RequestClose?.Invoke();
        });
    }

    public interface IObjectCharacteristics
    {
        public string SelectedObjectType {  get; }
        public ObservableCollection<string> ObjectCharacteristicsList { get; }
    }
}
