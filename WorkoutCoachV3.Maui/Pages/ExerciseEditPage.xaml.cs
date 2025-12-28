// Page code-behind: edit-scherm voor een oefening, volledig aangestuurd via ExerciseEditViewModel.
using WorkoutCoachV3.Maui.ViewModels;

namespace WorkoutCoachV3.Maui.Pages;

public partial class ExerciseEditPage : ContentPage
{
    // ViewModel wordt via DI meegegeven zodat Save/Cancel en velden via binding werken.
    public ExerciseEditPage(ExerciseEditViewModel vm)
    {
        InitializeComponent();
        // Verbindt alle XAML bindings (Title/Name/Category/Commands) met deze ViewModel.
        BindingContext = vm;
    }
}
