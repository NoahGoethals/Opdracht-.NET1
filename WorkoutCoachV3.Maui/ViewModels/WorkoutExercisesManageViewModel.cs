using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class WorkoutExercisesManageViewModel : ObservableObject
{
    private readonly LocalDatabaseService _local;
    private readonly ISyncService _sync;

    private Guid _workoutLocalId;

    [ObservableProperty] private string? title;
    [ObservableProperty] private string? error;
    [ObservableProperty] private bool isBusy;

    public partial class ManageRowVm : ObservableObject
    {
        public Guid ExerciseLocalId { get; init; }
        public string Name { get; init; } = "";

        [ObservableProperty] private bool isInWorkout;

        [ObservableProperty] private string repetitionsText = "0";

        [ObservableProperty] private string weightKgText = "0";
    }

    public ObservableCollection<ManageRowVm> Rows { get; } = new();

    public WorkoutExercisesManageViewModel(LocalDatabaseService local, ISyncService sync)
    {
        _local = local;
        _sync = sync;
    }

    public async Task InitAsync(Guid workoutLocalId, string workoutTitle)
    {
        _workoutLocalId = workoutLocalId;
        Title = $"Manage Exercises - {workoutTitle}";
        Error = null;

        // ✅ Zorg dat je “live” exercises hebt (ook als ze net op web of elders zijn toegevoegd)
        try { await _sync.SyncAllAsync(); } catch { }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
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
                    RepetitionsText = r.Repetitions.ToString(CultureInfo.InvariantCulture),
                    WeightKgText = r.WeightKg.ToString(CultureInfo.InvariantCulture)
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
                var reps = ParseIntSafe(r.RepetitionsText);
                var weight = ParseDoubleSafe(r.WeightKgText);

                return (
                    ExerciseLocalId: r.ExerciseLocalId,
                    IsInWorkout: r.IsInWorkout,
                    Repetitions: reps,
                    WeightKg: weight
                );
            }).ToList();

            await _local.SaveWorkoutExercisesAsync(_workoutLocalId, data);

            // ✅ Push direct naar web + pull terug zodat je web & maui snel gelijk lopen
            try { await _sync.SyncAllAsync(); } catch { }

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

    private static int ParseIntSafe(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return 0;
        input = input.Trim();

        return int.TryParse(input, NumberStyles.Integer, CultureInfo.CurrentCulture, out var v)
            ? Math.Max(0, v)
            : int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)
                ? Math.Max(0, v)
                : 0;
    }

    private static double ParseDoubleSafe(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return 0.0;
        input = input.Trim();

        if (double.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out var v))
            return Math.Max(0.0, v);

        if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
            return Math.Max(0.0, v);

        input = input.Replace(',', '.');
        return double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out v)
            ? Math.Max(0.0, v)
            : 0.0;
    }
}
