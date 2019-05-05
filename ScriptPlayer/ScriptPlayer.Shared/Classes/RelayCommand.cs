using System;
using System.Windows.Input;

namespace ScriptPlayer.Shared
{
    public class RelayCommand<T> : ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;
        public RelayCommand(Action<T> execute, Func<T,bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }

            if (parameter is T variable)
                return _canExecute(variable);

            return _canExecute(default(T));
        }
        public void Execute(object parameter)
        {
            if (parameter is T variable)
                _execute(variable);
            else
                _execute(default(T));
        }
    }

    public class RelayCommand : ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;
        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        public RelayCommand(Action execute)
            : this(execute, null)
        {
        }
        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }
            return _canExecute();
        }
        public void Execute(object parameter)
        {
            _execute();
        }
    }
}
