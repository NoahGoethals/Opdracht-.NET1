using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using WorkoutCoachV3.Maui.Messages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class ExerciseEditViewModel : ObservableObject
{
    private readonly IExercisesApi _api;
    private int? _id;

    [ObservableProperty] private string title = "New Exercise";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string? category;
    [ObservableProperty] private string? notes;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public ExerciseEditViewModel(IExercisesApi api) => _api = api;

    public void InitForCreate()
    {
        _id = null;
        Title = "New Exercise";
        Name = "";
        Category = "";
        Notes = "";
        Error = null;
    }

    public void InitForEdit(ExerciseDto dto)
    {
        _id = dto.Id;
        Title = "Edit Exercise";
        Name = dto.Name;
        Category = dto.Category;
        Notes = dto.Notes;
        Error = null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy) return;

        Error = null;

        if (string.IsNullOrWhiteSpace(Name))
        {
            Error = "Name is required.";
            return;
        }

        IsBusy = true;

        try
        {
            var dto = new ExerciseUpsertDto(
                Name.Trim(),
                string.IsNullOrWhiteSpace(Category) ? "" : Category!.Trim(),
                string.IsNullOrWhiteSpace(Notes) ? null : Notes!.Trim()
            );

            if (_id is null)
                await _api.CreateAsync(dto);
            else
                await _api.UpdateAsync(_id.Value, dto);

            WeakReferenceMessenger.Default.Send(new ExercisesChangedMessage());
            await Application.Current!.MainPage!.Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Application.Current!.MainPage!.Navigation.PopAsync();
    }
}
