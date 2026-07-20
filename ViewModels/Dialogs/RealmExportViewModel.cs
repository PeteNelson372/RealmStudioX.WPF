using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Editor.Services;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using System.Windows.Input;

namespace RealmStudioX.WPF.ViewModels.Controls
{
    public class RealmExportViewModel : ViewModelBase, IRealmExportSettings
    {
        private ExportService _exportService;

        public event EventHandler? CloseRequested;

        public RealmExportViewModel(ExportService exportService)
        {
            _exportService = exportService;
        }

        private RealmExportType _realmExportType = RealmExportType.BitmapImage;
        public RealmExportType RealmExportType
        {
            get => _realmExportType;
            set => SetProperty(ref _realmExportType, value);
        }

        private RealmMapExportFormat _realmExportFormat = RealmMapExportFormat.PNG;
        public RealmMapExportFormat RealmExportFormat
        {
            get => _realmExportFormat;
            set => SetProperty(ref _realmExportFormat, value);
        }

        public ICommand ExportRealmCommand => new RelayCommand(() =>
        {
            _exportService.ExportRealm((IRealmExportSettings)this);

            CloseRequested?.Invoke(this, EventArgs.Empty);
        });
    }

    public interface IRealmExportSettings
    {
        public RealmExportType RealmExportType {  get; }
        public RealmMapExportFormat RealmExportFormat { get; }
    }
}
