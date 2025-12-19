using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class SessionDetailViewModel : ObservableObject
{
    private readonly LocalDatabaseService _local;
    private Guid _sessionLocalId;

    [ObservableProperty] private string title = "Session";
    [ObservableProperty] private DateTime date = DateTime.Today;
    [ObservableProperty] private string? description;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public ObservableCollection<LocalDatabaseService.SessionSetDisplay> Sets { get; } = new();

    public SessionDetailViewModel(LocalDatabaseService local)
    {
        _local = local;
    }

    public async Task InitAsync(Guid sessionLocalId)
    {
        _sessionLocalId = sessionLocalId;
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
            var s = await _local.GetSessionByLocalIdAsync(_sessionLocalId);
            if (s is null)
            {
                Error = "Session not found.";
                return;
            }

            Title = s.Title;
            Date = s.Date;
            Description = s.Description;

            var sets = await _local.GetSessionSetsAsync(_sessionLocalId);
            Sets.Clear();
            foreach (var set in sets)
                Sets.Add(set);
        }
        catch (Exception ex)
        {
            Error = ex.InnerException?.Message ?? ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
