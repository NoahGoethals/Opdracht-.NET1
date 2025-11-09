
using System;
using System.Windows.Input;

namespace WorkoutCoachV2.App.Helpers
{
    // Eenvoudige ICommand-implementatie voor knoppen en bindings.
    public sealed class RelayCommand : ICommand
    {
        // Actie die uitgevoerd wordt wanneer het commando runt.
        private readonly Action<object?> _execute;

        // Optionele guard die bepaalt of het commando kan uitvoeren.
        private readonly Func<object?, bool>? _canExecute;

        // Maak een nieuw commando met verplichte execute en optionele canExecute.
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Mag dit commando nu uitgevoerd worden?
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        // Voer het commando uit.
        public void Execute(object? parameter) => _execute(parameter);

        // Event om de UI te laten her-evalueren of het commando kan uitvoeren.
        public event EventHandler? CanExecuteChanged;

        // Manueel laten her-evalueren (bv. na state-wijziging).
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
