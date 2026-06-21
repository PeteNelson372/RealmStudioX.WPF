using RealmStudioX.Core;
using RealmStudioX.WPF.ViewModels.Infrastructure;

namespace RealmStudioX.WPF.Editor.Services
{
    public class CommandService : ViewModelBase
    {
        /*
         * The CommandService routes undo/redo operations to the proper CommandManager
         * instance. It also tracks modification state for the project, since the
         * modification state of the project is partially determined by the state of
         * the Undo stack in the CommandManager instances.
         */

        private readonly CommandManager _projectCommands;
        private readonly CommandManager _mapCommands;

        private bool _projectPanelSelected = false;

        private int _mapSaveUndoCount = 0;
        private int _projectSaveUndoCount = 0;

        private bool _projectDataModified = false;
        private bool _mapsModified = false;

        private bool _lastUnsavedChangesState;

        public event EventHandler<bool>? HasSavedChangesUpdate;

        public CommandService(CommandManager projectCommands, CommandManager mapCommands)
        {
            _projectCommands = projectCommands;
            _mapCommands = mapCommands;

            _projectCommands.CommandHistoryChanged += OnCommandStateChanged;
            _mapCommands.CommandHistoryChanged += OnCommandStateChanged;
        }

        private void OnCommandStateChanged()
        {
            OnPropertyChanged(nameof(HasUnsavedChanges));
            NotifyUnsavedChangesChanged();
        }

        public bool ProjectPanelSelected
        {
            get => _projectPanelSelected;
            set => _projectPanelSelected = value;
        }

        public CommandManager ActiveCommands
        {
            get
            {
                return _projectPanelSelected ? _projectCommands : _mapCommands;
            }
        }

        public void MarkSaved()
        {
            _mapSaveUndoCount = _mapCommands.UndoCount;
            _mapsModified = false;
            
            _projectSaveUndoCount = _projectCommands.UndoCount;
            _projectDataModified = false;

            OnPropertyChanged(nameof(HasUnsavedChanges));

            NotifyUnsavedChangesChanged();
        }

        public void MarkProjectDataModified()
        {
            _projectDataModified = true;
            OnPropertyChanged(nameof(HasUnsavedChanges));

            NotifyUnsavedChangesChanged();
        }

        public void MarkMapModified()
        {
            _mapsModified = true;
            OnPropertyChanged(nameof(HasUnsavedChanges));

            NotifyUnsavedChangesChanged();
        }

        private bool _hasUnsavedChanges = false;

        public bool HasUnsavedChanges
        {
            get
            {
                _hasUnsavedChanges = (_mapSaveUndoCount != _mapCommands.UndoCount)
                    || (_projectSaveUndoCount != _projectCommands.UndoCount)
                    || _mapsModified
                    || _projectDataModified;

                return _hasUnsavedChanges;
            }
        }

        private void NotifyUnsavedChangesChanged()
        {
            bool current = HasUnsavedChanges;

            if (current == _lastUnsavedChangesState)
            {
                return;
            }

            _lastUnsavedChangesState = current;

            HasSavedChangesUpdate?.Invoke(this, current);
        }
    }
}
