using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class WorkoutExercisesManageViewModel : ObservableObject
{
    private readonly LocalDatabaseService _local;

    private Guid _workoutLocalId;

    public ObservableCollection<RowVm> Rows { get; } = new();

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

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            var rows = await _local.GetWorkoutExerciseManageRowsAsync(_workoutLocalId);
            Rows.Clear();
            foreach (var r in rows)
            {
                Rows.Add(new RowVm(r.ExerciseLocalId, r.Name)
                {
                    IsInWorkout = r.IsInWorkout,
                    RepetitionsText = r.Repetitions.ToString(),
                    WeightText = r.WeightKg.ToString()
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
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            var data = Rows.Select(r =>
            {
                var reps = 0;
                _ = int.TryParse(r.RepetitionsText, out reps);

                var weight = 0d;
                _ = double.TryParse(r.WeightText, out weight);

                return (r.ExerciseLocalId, r.IsInWorkout, reps, weight);
            }).ToList();

            await _local.SaveWorkoutExercisesAsync(_workoutLocalId, data);

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

    public partial class RowVm : ObservableObject
    {
        public Guid ExerciseLocalId { get; }
        public string Name { get; }

        [ObservableProperty] private bool isInWorkout;
        [ObservableProperty] private string repetitionsText = "0";
        [ObservableProperty] private string weightText = "0";

        public RowVm(Guid exerciseLocalId, string name)
        {
            ExerciseLocalId = exerciseLocalId;
            Name = name;
        }
    }
}
