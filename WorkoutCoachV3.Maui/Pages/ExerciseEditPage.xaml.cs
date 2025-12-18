using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class ExerciseEditPage : ContentPage
{
    public ExerciseEditPage(ExerciseEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
