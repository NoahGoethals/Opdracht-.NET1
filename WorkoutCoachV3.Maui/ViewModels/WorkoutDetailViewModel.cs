using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class WorkoutDetailViewModel : ObservableObject
{
    private readonly LocalDatabaseService _local;
    private readonly IServiceProvider _services;

    private Guid _workoutLocalId;

    [ObservableProperty] private string workoutTitle = "";
    [ObservableProperty] private string? notes;

    public ObservableCollection<LocalDatabaseService.WorkoutExerciseDisplay> Items { get; } = new();

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
            var w = await _local.GetWorkoutByLocalIdAsync(_workoutLocalId);
            if (w is null)
            {
                Error = "Workout not found.";
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
        var page = _services.GetRequiredService<WorkoutExercisesManagePage>();
        var vm = (WorkoutExercisesManageViewModel)page.BindingContext!;
        await vm.InitAsync(_workoutLocalId);

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }
}
