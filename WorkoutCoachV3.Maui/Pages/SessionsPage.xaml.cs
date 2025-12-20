using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class SessionsPage : ContentPage
{
    private SessionsViewModel Vm => (SessionsViewModel)BindingContext;

    public SessionsPage(SessionsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await Vm.RefreshAsync();
    }
}
