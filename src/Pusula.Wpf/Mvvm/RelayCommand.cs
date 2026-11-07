using System;
using System.Windows.Input;

namespace Pusula.Wpf.Mvvm
{
    /// <summary>
    /// Bir eylemi çalıştıran basit <see cref="ICommand"/>.
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Action execute;

        /// <summary>Komutu oluşturur.</summary>
        public RelayCommand(Action execute)
        {
            this.execute = execute;
        }

        /// <inheritdoc />
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <inheritdoc />
        public bool CanExecute(object? parameter)
        {
            return true;
        }

        /// <inheritdoc />
        public void Execute(object? parameter)
        {
            execute();
        }
    }
}
