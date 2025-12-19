using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutCoachV3.Maui.Data.LocalEntities;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class SessionEditViewModel : ObservableObject
{
    private readonly LocalDatabaseService _local;
    private Guid? _editingSessionLocalId;

    [ObservableProperty] private string title = "New Session";
    [ObservableProperty] private string sessionTitle = "";
    [ObservableProperty] private DateTime sessionDate = DateTime.Today;
    [ObservableProperty] private string? description;

    [ObservableProperty] private bool isCreate;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public class WorkoutPickRowVm : ObservableObject
    {
        public Guid WorkoutLocalId { get; init; }
        public string WorkoutTitle { get; init; } = "";

        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    }

    public ObservableCollection<WorkoutPickRowVm> Workouts { get; } = new();

    public SessionEditViewModel(LocalDatabaseService local)
    {
        _local = local;
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

        await LoadWorkoutsAsync();
    }

    public async Task InitForEditAsync(Guid sessionLocalId)
    {
        _editingSessionLocalId = sessionLocalId;
        IsCreate = false;

        Title = "Edit Session";
        Error = null;

        var s = await _local.GetSessionByLocalIdAsync(sessionLocalId);
        if (s is null)
        {
            Error = "Session not found.";
            return;
        }

        SessionTitle = s.Title;
        SessionDate = s.Date;
        Description = s.Description;

        Workouts.Clear(); 
    }

    private async Task LoadWorkoutsAsync()
    {
        Workouts.Clear();
        var workouts = await _local.GetWorkoutsAsync();

        foreach (var w in workouts)
        {
            Workouts.Add(new WorkoutPickRowVm
            {
                WorkoutLocalId = w.LocalId,
                WorkoutTitle = w.Title,
                IsSelected = false
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

            if (IsCreate)
            {
                var selected = Workouts.Where(x => x.IsSelected).Select(x => x.WorkoutLocalId).ToList();
                if (selected.Count == 0)
                {
                    Error = "Select at least 1 workout.";
                    return;
                }

                await _local.CreateSessionFromWorkoutsAsync(SessionTitle, SessionDate, Description, selected);
            }
            else
            {
                var entity = new LocalSession
                {
                    LocalId = _editingSessionLocalId!.Value,
                    Title = SessionTitle.Trim(),
                    Date = SessionDate,
                    Description = Description,
                    IsDeleted = false
                };

                await _local.UpsertSessionAsync(entity);
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
    public async Task CancelAsync()
    {
        await Application.Current!.MainPage!.Navigation.PopAsync();
    }
}
