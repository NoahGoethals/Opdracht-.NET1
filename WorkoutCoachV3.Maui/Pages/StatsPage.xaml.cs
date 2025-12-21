using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class StatsPage : ContentPage
{
    private bool _initialized;

    public StatsPage(StatsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

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
