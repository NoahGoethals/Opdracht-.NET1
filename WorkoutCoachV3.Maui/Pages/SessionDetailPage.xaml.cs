using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class SessionDetailPage : ContentPage
{
    public SessionDetailPage(SessionDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
