using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class AdminPanelPage : ContentPage
{
    public AdminPanelPage(AdminPanelViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is AdminPanelViewModel vm)
            await vm.LoadAsync();
    }
}
