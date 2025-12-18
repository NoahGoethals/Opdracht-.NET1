using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class ExercisesPage : ContentPage
{
    public ExercisesPage(ExercisesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ExercisesViewModel vm)
            await vm.RefreshAsync();
    }
}
