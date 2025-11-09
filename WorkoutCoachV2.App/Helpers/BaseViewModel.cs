using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkoutCoachV2.App.Helpers
{
    // Basisklasse voor viewmodels met INotifyPropertyChanged.
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        // Event dat de UI verwittigt bij property-wijzigingen.
        public event PropertyChangedEventHandler? PropertyChanged;

        // Helper: stel een property in en trigger PropertyChanged als de waarde effectief verandert.
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            return true;
        }
    }
}
