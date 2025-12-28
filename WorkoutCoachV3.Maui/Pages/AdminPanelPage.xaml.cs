// Page code-behind: koppelt de UI aan de AdminPanelViewModel en triggert data-load bij openen.
using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class AdminPanelPage : ContentPage
{
    // DI-injectie van de ViewModel zodat Shell/Routes de pagina kan maken met dependencies.
    public AdminPanelPage(AdminPanelViewModel vm)
    {
        InitializeComponent();
        // BindingContext bepaalt waar alle {Binding ...} in XAML naar verwijzen.
        BindingContext = vm;
    }

    // Wordt aangeroepen telkens de pagina zichtbaar wordt (ook na terug navigeren).
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Laadt/refresh't de lijst met users + roles wanneer de pagina verschijnt.
        if (BindingContext is AdminPanelViewModel vm)
            await vm.LoadAsync();
    }
}
