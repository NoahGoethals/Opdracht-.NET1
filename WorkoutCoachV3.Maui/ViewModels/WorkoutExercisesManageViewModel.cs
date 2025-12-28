using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class WorkoutExercisesManageViewModel : ObservableObject
{
    // Services: lokale DB + sync zodat web en MAUI snel gelijk lopen.
    private readonly LocalDatabaseService _local;
    private readonly ISyncService _sync;

    // Huidige workout waarvoor we oefeningen beheren.
    private Guid _workoutLocalId;

    // Page header + UI state.
    [ObservableProperty] private string? title;
    [ObservableProperty] private string? error;
    [ObservableProperty] private bool isBusy;

    public partial class ManageRowVm : ObservableObject
    {
        // Exercise id + display name voor de rij.
        public Guid ExerciseLocalId { get; init; }
        public string Name { get; init; } = "";

        // Checkbox: zit deze oefening in de workout?
        [ObservableProperty] private bool isInWorkout;

        // Text velden (Entry) zodat parsing gecontroleerd kan gebeuren.
        [ObservableProperty] private string repetitionsText = "0";

        // Text veld voor gewicht (kg) (Entry).
        [ObservableProperty] private string weightKgText = "0";
    }

    // Datasource voor CollectionView in WorkoutExercisesManagePage.
    public ObservableCollection<ManageRowVm> Rows { get; } = new();

    public WorkoutExercisesManageViewModel(LocalDatabaseService local, ISyncService sync)
    {
        _local = local;
        _sync = sync;
    }

    public async Task InitAsync(Guid workoutLocalId, string workoutTitle)
    {
        // Init screen voor een specifieke workout.
        _workoutLocalId = workoutLocalId;
        Title = $"Manage Exercises - {workoutTitle}";
        Error = null;

        // Best effort sync zodat exercises en links up-to-date zijn.
        try { await _sync.SyncAllAsync(); } catch { }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        // Laadt alle rows (exercise + huidige link status) vanuit local DB.
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
        // Verwerkt rows: parse reps/weight en bewaart links in local DB.
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            var data = Rows.Select(r =>
            {
                // Parsing is tolerant voor culture (komma/punt).
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

            // Best effort sync: push + pull zodat web & maui gelijk lopen.
            try { await _sync.SyncAllAsync(); } catch { }

            await Application.Current!.MainPage!.Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            // Extra detail voor debugging (inner exceptions van DB/EF).
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
        // Sluit de manage page zonder save.
        await Application.Current!.MainPage!.Navigation.PopAsync();
    }

    private static int ParseIntSafe(string? input)
    {
        // Verhindert crashes door lege/ongeldige input: default 0 en geen negatieve waarden.
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
        // Tolerant parsing (culture + manual comma->dot fallback), default 0.0.
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
