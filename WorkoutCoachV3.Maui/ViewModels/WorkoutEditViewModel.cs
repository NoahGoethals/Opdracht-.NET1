using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutCoachV3.Maui.Data.LocalEntities;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class WorkoutEditViewModel : ObservableObject
{
    private readonly LocalDatabaseService _local;
    private readonly ISyncService _sync;

    private Guid? _editingLocalId;

    [ObservableProperty] private string title = "New Workout";

    [ObservableProperty] private string workoutTitle = "";
    [ObservableProperty] private string? notes;

    [ObservableProperty] private string? error;
    [ObservableProperty] private bool isBusy;

    public WorkoutEditViewModel(LocalDatabaseService local, ISyncService sync)
    {
        _local = local;
        _sync = sync;
    }

    public void InitForCreate()
    {
        _editingLocalId = null;
        Title = "New Workout";
        WorkoutTitle = "";
        Notes = null;
        Error = null;
    }

    public async Task InitForEditAsync(Guid localId)
    {
        _editingLocalId = localId;
        Title = "Edit Workout";
        Error = null;

        await LoadAsync(localId);
    }

    private async Task LoadAsync(Guid localId)
    {
        try
        {
            var w = await _local.GetWorkoutByLocalIdAsync(localId);
            if (w is null) return;

            WorkoutTitle = w.Title;
            Notes = w.Notes;
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
            if (string.IsNullOrWhiteSpace(WorkoutTitle))
            {
                Error = "Title is required.";
                return;
            }

            var entity = new LocalWorkout
            {
                LocalId = _editingLocalId ?? Guid.NewGuid(),
                Title = WorkoutTitle.Trim(),
                Notes = Notes,
                IsDeleted = false
            };

            await _local.UpsertWorkoutAsync(entity);

            try { await _sync.SyncAllAsync(); } catch {  }

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
