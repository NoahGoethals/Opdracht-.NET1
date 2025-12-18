using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutCoachV3.Maui.Data.LocalEntities;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class ExerciseEditViewModel : ObservableObject
{
    private readonly LocalDatabaseService _local;
    private readonly ISyncService _sync;

    private Guid? _editingLocalId;

    [ObservableProperty] private string title = "New Exercise";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string category = "";
    [ObservableProperty] private string? notes;

    [ObservableProperty] private string? error;
    [ObservableProperty] private bool isBusy;

    public ExerciseEditViewModel(LocalDatabaseService local, ISyncService sync)
    {
        _local = local;
        _sync = sync;
    }

    public void InitForCreate()
    {
        _editingLocalId = null;
        Title = "New Exercise";
        Name = "";
        Category = "";
        Notes = null;
        Error = null;
    }

    public async Task InitForEditAsync(Guid localId)
    {
        _editingLocalId = localId;
        Title = "Edit Exercise";
        Error = null;

        await LoadAsync(localId);
    }

    private async Task LoadAsync(Guid localId)
    {
        try
        {
            var e = await _local.GetExerciseByLocalIdAsync(localId);
            if (e is null) return;

            Name = e.Name;
            Category = e.Category ?? "";
            Notes = e.Notes;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Error = "Name is required.";
                return;
            }

            var entity = new LocalExercise
            {
                LocalId = _editingLocalId ?? Guid.NewGuid(),
                Name = Name.Trim(),
                Category = Category?.Trim() ?? "",
                Notes = Notes,
                IsDeleted = false
            };

            await _local.UpsertExerciseAsync(entity);

            try { await _sync.SyncAllAsync(); }
            catch {  }

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
    public async Task CancelAsync()
    {
        await Application.Current!.MainPage!.Navigation.PopAsync();
    }
}
