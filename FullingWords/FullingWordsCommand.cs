using System;
using System.Windows.Input;

namespace FullingWords
{
    public class FullingWordsCommand:ICommand
    {
        private Action _execute;
        private Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public FullingWordsCommand(Action execute):this(execute, null)
        { }

        public FullingWordsCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
        
    }
}
