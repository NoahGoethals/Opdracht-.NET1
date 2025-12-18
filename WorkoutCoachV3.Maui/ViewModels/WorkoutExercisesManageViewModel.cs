using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutCoachV3.Maui.Data.LocalEntities;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class WorkoutExercisesManageViewModel : ObservableObject
{
    private readonly LocalDatabaseService _local;

    private Guid? _workoutLocalId;

    public ObservableCollection<WorkoutExerciseLineItem> Items { get; } = new();

    [ObservableProperty] private string titleText = "Manage Exercises";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public WorkoutExercisesManageViewModel(LocalDatabaseService local)
    {
        _local = local;
    }

    public async Task InitAsync(Guid workoutLocalId)
    {
        _workoutLocalId = workoutLocalId;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_workoutLocalId is null) return;

        IsBusy = true;
        Error = null;

        try
        {
            var workout = await _local.GetWorkoutByLocalIdAsync(_workoutLocalId.Value);
            TitleText = workout is null ? "Manage Exercises" : $"Manage: {workout.Title}";

            var allExercises = await _local.GetExercisesAsync();
            var links = await _local.GetWorkoutExerciseLinksAsync(_workoutLocalId.Value);

            var activeLinks = links.Where(x => !x.IsDeleted).ToDictionary(x => x.ExerciseLocalId, x => x);
            var deletedLinks = links.Where(x => x.IsDeleted).ToDictionary(x => x.ExerciseLocalId, x => x);

            Items.Clear();
            foreach (var ex in allExercises)
            {
                var hasActive = activeLinks.TryGetValue(ex.LocalId, out var link);
                var hasDeleted = !hasActive && deletedLinks.TryGetValue(ex.LocalId, out link);

                Items.Add(new WorkoutExerciseLineItem
                {
                    ExerciseLocalId = ex.LocalId,
                    ExerciseName = ex.Name,
                    Category = ex.Category,
                    IsInWorkout = hasActive,
                    Reps = (hasActive || hasDeleted) ? link!.Reps : 0,
                    WeightKg = (hasActive || hasDeleted) ? link!.WeightKg : 0,
                    ExistingLinkLocalId = (hasActive || hasDeleted) ? link!.LocalId : (Guid?)null
                });
            }
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
    private async Task SaveAsync()
    {
        if (_workoutLocalId is null) return;
        if (IsBusy) return;

        IsBusy = true;
        Error = null;

        try
        {
            foreach (var item in Items)
            {
                // Ignore invalid reps/weight when not selected
                if (!item.IsInWorkout)
                {
                    // If it existed before => soft delete
                    if (item.ExistingLinkLocalId is not null)
                    {
                        await _local.SoftDeleteWorkoutExerciseAsync(item.ExistingLinkLocalId.Value);
                    }
                    continue;
                }

                // Selected: create/update
                var reps = item.Reps < 0 ? 0 : item.Reps;
                var weight = item.WeightKg < 0 ? 0 : item.WeightKg;

                var link = new LocalWorkoutExercise
                {
                    LocalId = item.ExistingLinkLocalId ?? Guid.NewGuid(),
                    WorkoutLocalId = _workoutLocalId.Value,
                    ExerciseLocalId = item.ExerciseLocalId,
                    Reps = reps,
                    WeightKg = weight,
                    IsDeleted = false
                };

                await _local.UpsertWorkoutExerciseAsync(link);
            }

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

    public partial class WorkoutExerciseLineItem : ObservableObject
    {
        public Guid ExerciseLocalId { get; set; }
        public Guid? ExistingLinkLocalId { get; set; }

        [ObservableProperty] private string exerciseName = "";
        [ObservableProperty] private string category = "";

        [ObservableProperty] private bool isInWorkout;

        [ObservableProperty] private int reps;
        [ObservableProperty] private double weightKg;
    }
}
