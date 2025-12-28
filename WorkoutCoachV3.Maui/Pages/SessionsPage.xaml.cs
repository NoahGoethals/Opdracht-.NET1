// Code-behind: refresh't de sessielijst telkens de pagina verschijnt.
using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class SessionsPage : ContentPage
{
    // Helper property om de sterk getypte ViewModel snel te gebruiken.
    private SessionsViewModel Vm => (SessionsViewModel)BindingContext;

    // DI: ViewModel injecteren en instellen als BindingContext voor alle XAML bindings.
    public SessionsPage(SessionsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    // Bij openen/terugkeren: lijst opnieuw laden zodat nieuwe/bewerkte sessies zichtbaar zijn.
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await Vm.RefreshAsync();
    }
}
