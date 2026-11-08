using System;
using System.Windows.Input;

namespace Pusula.Wpf.Mvvm
{
    /// <summary>
    /// Bir eylemi çalıştıran basit <see cref="ICommand"/>. İsterse komut parametresini alır.
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> execute;

        /// <summary>Parametresiz komut oluşturur.</summary>
        public RelayCommand(Action execute)
        {
            this.execute = _ => execute();
        }

        /// <summary>Komut parametresini alan komut oluşturur.</summary>
        public RelayCommand(Action<object?> execute)
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
            execute(parameter);
        }
    }
}
