// Code-behind: initialiseert stats 1x en past filters toe bij openen.
using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class StatsPage : ContentPage
{
    // Flag om InitAsync slechts één keer uit te voeren.
    private bool _initialized;

    // DI: ViewModel injecteren en instellen als BindingContext.
    public StatsPage(StatsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    // Bij openen: init 1x (options laden), daarna altijd ApplyAsync (filters toepassen).
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is not StatsViewModel vm) return;

        if (!_initialized)
        {
            _initialized = true;
            await vm.InitAsync();
        }

        await vm.ApplyAsync();
    }
}
