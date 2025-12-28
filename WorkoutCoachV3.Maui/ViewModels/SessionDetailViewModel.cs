using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class SessionDetailViewModel : ObservableObject
{
    // Local DB service voor session + sets detail info.
    private readonly LocalDatabaseService _local;
    private Guid _sessionLocalId;

    // Header info voor de detail page.
    [ObservableProperty] private string title = "Session";
    [ObservableProperty] private DateTime date = DateTime.Today;
    [ObservableProperty] private string? description;

    // UI state voor loading + foutmelding.
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    // CollectionView datasource voor sets display.
    public ObservableCollection<LocalDatabaseService.SessionSetDisplay> Sets { get; } = new();

    public SessionDetailViewModel(LocalDatabaseService local)
    {
        _local = local;
    }

    // Init met session id (wordt meestal vanuit navigation doorgegeven).
    public async Task InitAsync(Guid sessionLocalId)
    {
        _sessionLocalId = sessionLocalId;
        await LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        // Voorkom dubbele load (OnAppearing + refresh, etc.).
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            // Session ophalen en header velden vullen.
            var s = await _local.GetSessionByLocalIdAsync(_sessionLocalId);
            if (s is null)
            {
                Error = "Session not found.";
                return;
            }

            Title = s.Title;
            Date = s.Date;
            Description = s.Description;

            // Sets ophalen en UI collection vervangen.
            var sets = await _local.GetSessionSetsAsync(_sessionLocalId);
            Sets.Clear();
            foreach (var set in sets)
                Sets.Add(set);
        }
        catch (Exception ex)
        {
            // InnerException is vaak de echte SQLite melding.
            Error = ex.InnerException?.Message ?? ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
