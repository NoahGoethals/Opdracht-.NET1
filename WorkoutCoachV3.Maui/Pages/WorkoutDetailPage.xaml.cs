// Code-behind: bindt WorkoutDetailViewModel en laadt data bij openen.
using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class WorkoutDetailPage : ContentPage
{
    // DI: ViewModel injecteren en instellen als BindingContext.
    public WorkoutDetailPage(WorkoutDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    // Bij verschijnen: details + items refreshen (bv. na manage pagina).
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is WorkoutDetailViewModel vm)
            await vm.LoadAsync();
    }
}
