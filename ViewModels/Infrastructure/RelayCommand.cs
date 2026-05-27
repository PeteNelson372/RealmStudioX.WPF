using System;
using System.Windows.Input;

namespace RealmStudioX.WPF.ViewModels.Infrastructure
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?>? _executeWithParameter;

        private readonly Action? _execute;

        private readonly Func<object?, bool>? _canExecute;

        // -------------------------------------------------
        // Parameterless command
        // -------------------------------------------------

        public RelayCommand(
            Action execute,
            Func<bool>? canExecute = null)
        {
            _execute = execute;

            if (canExecute != null)
            {
                _canExecute = _ => canExecute();
            }
        }

        // -------------------------------------------------
        // Parameterized command
        // -------------------------------------------------

        public RelayCommand(
            Action<object?> execute,
            Func<object?, bool>? canExecute = null,
            bool usesParameter = true)
        {
            _executeWithParameter = execute;

            _canExecute = canExecute;
        }

        // -------------------------------------------------

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke(parameter)
                ?? true;
        }

        public void Execute(object? parameter)
        {
            if (_executeWithParameter != null)
            {
                _executeWithParameter(parameter);
            }
            else
            {
                _execute?.Invoke();
            }
        }

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(
                this,
                EventArgs.Empty);
        }
    }
}