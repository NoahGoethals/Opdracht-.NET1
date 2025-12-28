using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class WorkoutDetailViewModel : ObservableObject
{
    // Services: detail data uit lokale DB + sync + DI voor manage page.
    private readonly LocalDatabaseService _local;
    private readonly IServiceProvider _services;
    private readonly ISyncService _sync;

    // Huidige workout die deze detail page toont.
    private Guid _workoutLocalId;

    // Header fields voor de UI.
    [ObservableProperty] private string workoutTitle = "";
    [ObservableProperty] private string? notes;

    // Datasource voor "exercises in workout" lijst.
    public ObservableCollection<LocalDatabaseService.WorkoutExerciseDisplay> Items { get; } = new();

    // UI state: busy + foutmelding.
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public WorkoutDetailViewModel(LocalDatabaseService local, IServiceProvider services, ISyncService sync)
    {
        _local = local;
        _services = services;
        _sync = sync;
    }

    public async Task InitAsync(Guid workoutLocalId)
    {
        // Init met LocalId (meestal doorgegeven vanuit WorkoutsPage).
        _workoutLocalId = workoutLocalId;

        // Best effort sync zodat details zo recent mogelijk zijn.
        try { await _sync.SyncAllAsync(); } catch { }

        await LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        // Laadt header + lijst van workout-exercises uit de lokale DB.
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            var w = await _local.GetWorkoutByLocalIdAsync(_workoutLocalId);
            if (w is null)
            {
                Error = "Workout not found.";
                Items.Clear();
                return;
            }

            WorkoutTitle = w.Title;
            Notes = w.Notes;

            var data = await _local.GetWorkoutExercisesAsync(_workoutLocalId);

            Items.Clear();
            foreach (var x in data)
                Items.Add(x);
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
    private async Task ManageAsync()
    {
        // Manage page: checkbox lijst om oefeningen toe te voegen/verwijderen + reps/weight aanpassen.
        var page = _services.GetRequiredService<WorkoutExercisesManagePage>();
        var vm = (WorkoutExercisesManageViewModel)page.BindingContext!;

        await vm.InitAsync(_workoutLocalId, WorkoutTitle);

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }
}
