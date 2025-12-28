// Code-behind: bindt SessionEditViewModel zodat velden en Save/Cancel werken.
using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class SessionEditPage : ContentPage
{
    // DI: ViewModel injecteren en instellen als BindingContext.
    public SessionEditPage(SessionEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
