// Code-behind: bindt SessionDetailViewModel zodat titel/sets via binding geladen worden.
using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class SessionDetailPage : ContentPage
{
    // DI: ViewModel injecteren en instellen als BindingContext.
    public SessionDetailPage(SessionDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
