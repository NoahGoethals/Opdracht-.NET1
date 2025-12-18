using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class WorkoutExercisesManagePage : ContentPage
{
    public WorkoutExercisesManagePage(WorkoutExercisesManageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
