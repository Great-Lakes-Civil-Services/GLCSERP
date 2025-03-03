using System;
using System.Windows.Input;

namespace CivilProcessERP.Helpers
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        // Two constructors for convenience.
        public RelayCommand(Action<object> execute) : this(execute, _ => true) { }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || (parameter != null && _canExecute(parameter));
        }

        public void Execute(object? parameter)
        {
            _execute(parameter ?? throw new ArgumentNullException(nameof(parameter)));
        }
    }
}
