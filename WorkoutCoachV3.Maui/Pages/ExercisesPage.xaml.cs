// Code-behind: houdt ViewModel referentie bij en refresh't bij openen.
using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class ExercisesPage : ContentPage
{
    // Bewaart de geïnjecteerde ViewModel zodat we commands kunnen aanroepen in code-behind.
    private readonly ExercisesViewModel _vm;

    // DI: ViewModel wordt meegegeven en als BindingContext ingesteld voor XAML bindings.
    public ExercisesPage(ExercisesViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    // Bij verschijnen: probeer data opnieuw te laden (bv. na terug navigeren).
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // RefreshCommand haalt items/categorieën opnieuw op (async command).
            await _vm.RefreshCommand.ExecuteAsync(null);
        }
        catch
        {
            // Bewust leeg: voorkomt crash als refresh faalt (fout wordt elders getoond/gelogd).
        }
    }
}
