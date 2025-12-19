using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class WorkoutExercisesManageViewModel : ObservableObject
{
    private readonly LocalDatabaseService _local;
    private Guid _workoutLocalId;

    [ObservableProperty] private string? title;
    [ObservableProperty] private string? error;
    [ObservableProperty] private bool isBusy;

    public class ManageRowVm : ObservableObject
    {
        public Guid ExerciseLocalId { get; init; }
        public string Name { get; init; } = "";

        private bool _isInWorkout;
        public bool IsInWorkout { get => _isInWorkout; set => SetProperty(ref _isInWorkout, value); }

        private string _repetitionsText = "0";
        public string RepetitionsText { get => _repetitionsText; set => SetProperty(ref _repetitionsText, value); }

        private string _weightKgText = "0";
        public string WeightKgText { get => _weightKgText; set => SetProperty(ref _weightKgText, value); }
    }

    public IList<ManageRowVm> Rows { get; } = new List<ManageRowVm>();

    public WorkoutExercisesManageViewModel(LocalDatabaseService local)
    {
        _local = local;
    }

    public async Task InitAsync(Guid workoutLocalId, string workoutTitle)
    {
        _workoutLocalId = workoutLocalId;
        Title = $"Manage Exercises - {workoutTitle}";
        Error = null;

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Rows.Clear();

        var rows = await _local.GetWorkoutExerciseManageRowsAsync(_workoutLocalId);
        foreach (var r in rows)
        {
            Rows.Add(new ManageRowVm
            {
                ExerciseLocalId = r.ExerciseLocalId,
                Name = r.Name,
                IsInWorkout = r.IsInWorkout,
                RepetitionsText = r.Repetitions.ToString(),
                WeightKgText = r.WeightKg.ToString()
            });
        }

        OnPropertyChanged(nameof(Rows));
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
                int.TryParse(r.RepetitionsText, out var reps);
                double.TryParse(r.WeightKgText, out var weight);

                return (
                    ExerciseLocalId: r.ExerciseLocalId,
                    IsInWorkout: r.IsInWorkout,
                    Repetitions: reps,
                    WeightKg: weight
                );
            }).ToList();

            await _local.SaveWorkoutExercisesAsync(_workoutLocalId, data);

            await Application.Current!.MainPage!.Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message + (ex.InnerException != null ? "\n\nInner:\n" + ex.InnerException.Message : "");
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
