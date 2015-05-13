using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TodoListWpf
{
    public class AsyncRelayCommand : ICommand
    {
        #region Member Fields

        private readonly Func<object, Task> execute;
        private readonly Predicate<object> canExecute;

        #endregion

        #region Properties

        public bool IsExecuting { get; private set; }

        #endregion

        #region Constructors

        public AsyncRelayCommand(Func<object, Task> execute)
            : this(execute, null)
        {
        }

        public AsyncRelayCommand(Func<object, Task> execute, Predicate<object> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }
            this.execute = execute;
            this.canExecute = canExecute;
        }

        #endregion

        #region ICommand Members

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return this.IsExecuting ? false : (this.canExecute == null ? true : this.canExecute(parameter));
        }

        public async void Execute(object parameter)
        {
            if (!CanExecute(parameter))
            {
                throw new InvalidOperationException("The requested command cannot be executed at this time.");
            }
            try
            {
                this.IsExecuting = true;
                await this.execute(parameter);
            }
            finally
            {
                this.IsExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #endregion
    }
}