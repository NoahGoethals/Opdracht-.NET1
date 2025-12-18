using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class WorkoutEditPage : ContentPage
{
    public WorkoutEditPage(WorkoutEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
