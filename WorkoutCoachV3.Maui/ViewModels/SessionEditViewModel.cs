using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class SessionEditViewModel : ObservableObject
{
    private readonly LocalDatabaseService _local;
    private readonly ISyncService _sync;

    private Guid? _editingSessionLocalId;

    [ObservableProperty] private string title = "New Session";
    [ObservableProperty] private string sessionTitle = "";
    [ObservableProperty] private DateTime sessionDate = DateTime.Today;
    [ObservableProperty] private string? description;

    public ObservableCollection<WorkoutPickRowVm> Workouts { get; } = new();

    [ObservableProperty] private bool isCreate;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public class WorkoutPickRowVm : ObservableObject
    {
        public Guid WorkoutLocalId { get; init; }
        public string WorkoutTitle { get; init; } = "";

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public SessionEditViewModel(LocalDatabaseService local, ISyncService sync)
    {
        _local = local;
        _sync = sync;
    }

    public async Task InitForCreateAsync()
    {
        _editingSessionLocalId = null;
        IsCreate = true;

        Title = "New Session";
        SessionTitle = "";
        SessionDate = DateTime.Today;
        Description = null;
        Error = null;

        await LoadWorkoutsAsync(preselectWorkoutLocalIds: null, preselectExerciseLocalIds: null);
    }

    public async Task InitForEditAsync(Guid sessionLocalId)
    {
        _editingSessionLocalId = sessionLocalId;
        IsCreate = false;

        Title = "Edit Session";
        Error = null;

        var session = await _local.GetSessionByLocalIdAsync(sessionLocalId);
        if (session is null)
        {
            Error = "Session not found.";
            return;
        }

        SessionTitle = session.Title;
        SessionDate = session.Date;
        Description = session.Description;

        var preselectWorkoutIds = ParseSourceWorkoutIdsFromNotes(session.Notes);

        if (preselectWorkoutIds.Count > 0)
        {
            await LoadWorkoutsAsync(preselectWorkoutLocalIds: preselectWorkoutIds, preselectExerciseLocalIds: null);
            return;
        }

        var sets = await _local.GetSessionSetsEntitiesAsync(sessionLocalId, includeDeleted: false);
        var exerciseIdsInSession = sets
            .Select(x => x.ExerciseLocalId)
            .Distinct()
            .ToHashSet();

        await LoadWorkoutsAsync(preselectWorkoutLocalIds: null, preselectExerciseLocalIds: exerciseIdsInSession);
    }

    private static HashSet<Guid> ParseSourceWorkoutIdsFromNotes(string? notes)
    {
        var result = new HashSet<Guid>();

        if (string.IsNullOrWhiteSpace(notes)) return result;
        if (!notes.StartsWith(LocalDatabaseService.SessionSourceWorkoutsNotesPrefix, StringComparison.Ordinal)) return result;

        var payload = notes.Substring(LocalDatabaseService.SessionSourceWorkoutsNotesPrefix.Length);
        if (string.IsNullOrWhiteSpace(payload)) return result;

        foreach (var part in payload.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Guid.TryParse(part, out var id))
                result.Add(id);
        }

        return result;
    }

    private async Task LoadWorkoutsAsync(HashSet<Guid>? preselectWorkoutLocalIds, HashSet<Guid>? preselectExerciseLocalIds)
    {
        Workouts.Clear();

        var workouts = await _local.GetWorkoutsAsync(search: null);

        foreach (var w in workouts)
        {
            var isSelected = false;

            if (preselectWorkoutLocalIds is not null && preselectWorkoutLocalIds.Count > 0)
            {
                isSelected = preselectWorkoutLocalIds.Contains(w.LocalId);
            }
            else if (preselectExerciseLocalIds is not null && preselectExerciseLocalIds.Count > 0)
            {
                var links = await _local.GetWorkoutExercisesAllStatesAsync(w.LocalId);
                var activeLinks = links.Where(l => !l.IsDeleted).ToList();

                isSelected =
                    activeLinks.Count > 0 &&
                    activeLinks.All(l => preselectExerciseLocalIds.Contains(l.ExerciseLocalId));
            }

            Workouts.Add(new WorkoutPickRowVm
            {
                WorkoutLocalId = w.LocalId,
                WorkoutTitle = w.Title,
                IsSelected = isSelected
            });
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
            if (string.IsNullOrWhiteSpace(SessionTitle))
            {
                Error = "Title is required.";
                return;
            }

            var selected = Workouts
                .Where(x => x.IsSelected)
                .Select(x => x.WorkoutLocalId)
                .Distinct()
                .ToList();

            if (selected.Count == 0)
            {
                Error = "Select at least 1 workout.";
                return;
            }

            if (IsCreate)
            {
                await _local.CreateSessionFromWorkoutsAsync(SessionTitle, SessionDate, Description, selected);
            }
            else
            {
                if (_editingSessionLocalId is null)
                {
                    Error = "Invalid session.";
                    return;
                }

                await _local.UpdateSessionFromWorkoutsAsync(
                    _editingSessionLocalId.Value,
                    SessionTitle,
                    SessionDate,
                    Description,
                    selected);
            }

            try { await _sync.SyncAllAsync(); } catch { }

            await Application.Current!.MainPage!.Navigation.PopAsync();
        }
        catch (DbUpdateException dbEx)
        {
            Error = dbEx.InnerException?.Message ?? dbEx.Message;
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
