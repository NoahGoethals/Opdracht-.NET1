// Code-behind: bindt WorkoutEditViewModel zodat Save/Cancel en velden werken.
using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class WorkoutEditPage : ContentPage
{
    // DI: ViewModel injecteren en instellen als BindingContext.
    public WorkoutEditPage(WorkoutEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
