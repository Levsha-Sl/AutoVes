using System;
using System.Windows.Input;

namespace WeighingSystem.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _executeWithParameter;
        private readonly Action _executeWithoutParameter;
        private readonly Predicate<object> _canExecute;

        /// <summary>
        /// Конструктор для методов с параметром
        /// </summary>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _executeWithParameter = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Конструктор для методов без параметра
        /// </summary> 
        public RelayCommand(Action execute, Predicate<object> canExecute = null)
        {
            _executeWithoutParameter = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            if (_executeWithParameter != null)
            {
                _executeWithParameter(parameter);
            }
            else if (_executeWithoutParameter != null)
            {
                _executeWithoutParameter();
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}