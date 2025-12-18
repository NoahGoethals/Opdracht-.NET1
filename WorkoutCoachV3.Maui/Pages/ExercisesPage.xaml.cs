using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class ExercisesPage : ContentPage
{
    private readonly ExercisesViewModel _vm;

    public ExercisesPage(ExercisesViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_vm.Items.Count == 0)
            await _vm.LoadAsync();
    }
}
