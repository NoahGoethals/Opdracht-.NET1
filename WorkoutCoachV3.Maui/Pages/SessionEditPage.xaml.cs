using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class SessionEditPage : ContentPage
{
    public SessionEditPage(SessionEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
