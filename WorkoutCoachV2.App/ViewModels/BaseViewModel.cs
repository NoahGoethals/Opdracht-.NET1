// ViewModel-basisklasse: INotifyPropertyChanged + helpers (SetProperty, Set, Raise).

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkoutCoachV2.App.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        // Event voor databinding (triggert UI updates).
        public event PropertyChangedEventHandler? PropertyChanged;

        // Set + raise property-changed; returnt false als de waarde niet wijzigt.
        protected bool SetProperty<T>(
            ref T storage,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Roept PropertyChanged veilig aan.
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // Korte alias voor SetProperty.
        protected bool Set<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
            => SetProperty(ref storage, value, propertyName);

        // Korte alias om alleen het event te raisen.
        protected void Raise([CallerMemberName] string? propertyName = null)
            => OnPropertyChanged(propertyName);
    }
}
