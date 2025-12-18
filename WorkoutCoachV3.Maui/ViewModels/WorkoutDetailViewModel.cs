using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutCoachV3.Maui.Data.LocalEntities;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class WorkoutDetailViewModel : ObservableObject
{
    private readonly LocalDatabaseService _local;
    private readonly IServiceProvider _services;

    private Guid? _workoutLocalId;

    public ObservableCollection<WorkoutExerciseRow> Items { get; } = new();

    [ObservableProperty] private string titleText = "Workout";
    [ObservableProperty] private string? notes;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public WorkoutDetailViewModel(LocalDatabaseService local, IServiceProvider services)
    {
        _local = local;
        _services = services;
    }

    public async Task InitAsync(Guid workoutLocalId)
    {
        _workoutLocalId = workoutLocalId;
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (_workoutLocalId is null) return;

        IsBusy = true;
        Error = null;

        try
        {
            var w = await _local.GetWorkoutByLocalIdAsync(_workoutLocalId.Value);
            if (w is null)
            {
                Error = "Workout not found.";
                return;
            }

            TitleText = w.Title;
            Notes = w.Notes;

            var rows = await _local.GetWorkoutExercisesAsync(_workoutLocalId.Value);

            Items.Clear();
            foreach (var (link, ex) in rows)
            {
                Items.Add(new WorkoutExerciseRow
                {
                    ExerciseName = ex.Name,
                    Category = ex.Category,
                    Reps = link.Reps,
                    WeightKg = link.WeightKg
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
    private async Task ManageExercisesAsync()
    {
        if (_workoutLocalId is null) return;

        var page = _services.GetRequiredService<WorkoutExercisesManagePage>();
        var vm = (WorkoutExercisesManageViewModel)page.BindingContext!;
        await vm.InitAsync(_workoutLocalId.Value);

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    public class WorkoutExerciseRow
    {
        public string ExerciseName { get; set; } = "";
        public string Category { get; set; } = "";
        public int Reps { get; set; }
        public double WeightKg { get; set; }
    }
}
