// Code-behind: bindt WorkoutsViewModel en refresh't lijst bij openen.
using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class WorkoutsPage : ContentPage
{
    // DI: ViewModel injecteren en instellen als BindingContext.
    public WorkoutsPage(WorkoutsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    // Bij verschijnen: lijst opnieuw laden (bv. na edit/delete/detail).
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is WorkoutsViewModel vm)
            await vm.RefreshAsync();
    }
}
