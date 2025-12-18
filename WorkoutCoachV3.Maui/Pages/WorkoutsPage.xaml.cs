using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class WorkoutsPage : ContentPage
{
    public WorkoutsPage(WorkoutsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is WorkoutsViewModel vm)
            await vm.RefreshAsync();
    }
}
