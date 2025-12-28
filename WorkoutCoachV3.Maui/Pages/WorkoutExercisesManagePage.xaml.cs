// Code-behind: bindt WorkoutExercisesManageViewModel voor rows + save/cancel.
using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class WorkoutExercisesManagePage : ContentPage
{
    // DI: ViewModel injecteren en instellen als BindingContext.
    public WorkoutExercisesManagePage(WorkoutExercisesManageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
