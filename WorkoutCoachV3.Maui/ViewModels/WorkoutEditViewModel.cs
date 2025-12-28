using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutCoachV3.Maui.Data.LocalEntities;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class WorkoutEditViewModel : ObservableObject
{
    // Services: lokale DB (offline) + sync naar API na save.
    private readonly LocalDatabaseService _local;
    private readonly ISyncService _sync;

    // Null = create, anders edit van bestaande LocalId.
    private Guid? _editingLocalId;

    // Pagina titel (XAML bindt Title).
    [ObservableProperty] private string title = "New Workout";

    // Form velden voor workout.
    [ObservableProperty] private string workoutTitle = "";
    [ObservableProperty] private string? notes;

    // UI state: foutmelding + busy (buttons/loader).
    [ObservableProperty] private string? error;
    [ObservableProperty] private bool isBusy;

    public WorkoutEditViewModel(LocalDatabaseService local, ISyncService sync)
    {
        _local = local;
        _sync = sync;
    }

    public void InitForCreate()
    {
        // Reset state voor een nieuwe workout (lege velden).
        _editingLocalId = null;
        Title = "New Workout";
        WorkoutTitle = "";
        Notes = null;
        Error = null;
    }

    public async Task InitForEditAsync(Guid localId)
    {
        // Zet edit mode en laad bestaande data in het form.
        _editingLocalId = localId;
        Title = "Edit Workout";
        Error = null;

        await LoadAsync(localId);
    }

    private async Task LoadAsync(Guid localId)
    {
        try
        {
            // Haalt workout uit lokale DB en vult velden.
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
        // Single-flight: voorkomt dubbel klikken op Save.
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            // Minimale validatie: Title is verplicht.
            if (string.IsNullOrWhiteSpace(WorkoutTitle))
            {
                Error = "Title is required.";
                return;
            }

            // Upsert entity: bij create nieuw Guid, bij edit hergebruik LocalId.
            var entity = new LocalWorkout
            {
                LocalId = _editingLocalId ?? Guid.NewGuid(),
                Title = WorkoutTitle.Trim(),
                Notes = Notes,
                IsDeleted = false
            };

            await _local.UpsertWorkoutAsync(entity);

            // Best effort sync: UI mag verdergaan ook als sync faalt.
            try { await _sync.SyncAllAsync(); } catch { }

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
        // Sluit de edit page zonder save.
        await Application.Current!.MainPage!.Navigation.PopAsync();
    }
}
