// File: WorkoutCoachV2.App/Utils/AsyncRelayCommand.cs
// AsyncRelayCommand: wrapt een async Task als ICommand met 'busy'-guard en CanExecute-updates.
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WorkoutCoachV2.App.Utils
{
    public sealed class AsyncRelayCommand : ICommand
    {
        // Te uitvoeren async-actie.
        private readonly Func<Task> _execute;

        // Optionele predicate om te bepalen of uitvoeren mag.
        private readonly Func<bool>? _canExecute;

        // Voorkomt dubbele uitvoering terwijl de taak loopt.
        private bool _running;

        // Constructor: injecteert async-actie en optionele can-execute.
        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Alleen uitvoerbaar als niet bezig én predicate (indien aanwezig) true geeft.
        public bool CanExecute(object? parameter) => !_running && (_canExecute?.Invoke() ?? true);

        // Start de async-actie; togglet _running en triggert CanExecuteChanged voor UI enable/disable.
        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            try
            {
                _running = true;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                await _execute();
            }
            finally
            {
                _running = false;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // Event: UI kan hierop listen voor knoppen/commands die opnieuw geëvalueerd moeten worden.
        public event EventHandler? CanExecuteChanged;
    }
}
