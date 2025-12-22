using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Linq;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class StatsViewModel : ObservableObject
{
    private readonly LocalDatabaseService _local;
    private readonly ISyncService _sync;
    private readonly IServiceProvider _services;
    private readonly ITokenStore _tokenStore;
    private readonly IUserSessionStore _sessionStore;

    public ObservableCollection<ExercisePickItem> ExerciseOptions { get; } = new();
    public ObservableCollection<LocalDatabaseService.ExerciseStatsRow> TopExercises { get; } = new();

    [ObservableProperty] private ExercisePickItem? selectedExercise;

    [ObservableProperty] private DateTime fromDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime toDate = DateTime.Today;

    [ObservableProperty] private int sessionsCount;
    [ObservableProperty] private int setsCount;
    [ObservableProperty] private int totalReps;
    [ObservableProperty] private double totalVolumeKg;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private string? error;

    // Admin button
    [ObservableProperty] private bool canAccessAdmin;

    public StatsViewModel(
        LocalDatabaseService local,
        ISyncService sync,
        IServiceProvider services,
        ITokenStore tokenStore,
        IUserSessionStore sessionStore)
    {
        _local = local;
        _sync = sync;
        _services = services;
        _tokenStore = tokenStore;
        _sessionStore = sessionStore;

        _ = LoadAdminFlagAsync();
    }

    private async Task LoadAdminFlagAsync()
    {
        CanAccessAdmin = await _sessionStore.IsInAnyRoleAsync("Admin", "Moderator");
    }

    public sealed class ExercisePickItem
    {
        public Guid? ExerciseLocalId { get; init; }
        public string Name { get; init; } = "";
        public override string ToString() => Name;
    }

    public async Task InitAsync()
    {
        await LoadAdminFlagAsync();
        await LoadExerciseOptionsAsync();
        SelectedExercise ??= ExerciseOptions.FirstOrDefault();
    }

    private async Task LoadExerciseOptionsAsync()
    {
        ExerciseOptions.Clear();

        ExerciseOptions.Add(new ExercisePickItem
        {
            ExerciseLocalId = null,
            Name = "All exercises"
        });

        var exercises = await _local.GetExercisesAsync(search: null, category: null);
        foreach (var e in exercises)
        {
            ExerciseOptions.Add(new ExercisePickItem
            {
                ExerciseLocalId = e.LocalId,
                Name = e.Name
            });
        }
    }

    // ✅ Verb: Load (reload zonder sync) -> LoadCommand
    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            var exerciseId = SelectedExercise?.ExerciseLocalId;

            var (summary, top) = await _local.GetStatsAsync(
                exerciseLocalId: exerciseId,
                from: FromDate,
                to: ToDate,
                takeTop: 50);

            SessionsCount = summary.Sessions;
            SetsCount = summary.Sets;
            TotalReps = summary.TotalReps;
            TotalVolumeKg = summary.TotalVolumeKg;

            TopExercises.Clear();
            foreach (var row in top)
                TopExercises.Add(row);
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

    // ✅ Verb: Apply (met sync zoals je had)
    [RelayCommand]
    public async Task ApplyAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            try { await _sync.SyncAllAsync(); } catch { }

            await LoadAsync();
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

    // ✅ Verb voor RefreshView: RefreshCommand (sync + reload)
    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (IsBusy) return;

        IsRefreshing = true;
        try
        {
            await LoadAdminFlagAsync();

            try { await _sync.SyncAllAsync(); } catch { }
            await LoadAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task GoToExercisesAsync()
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<ExercisesPage>());

    [RelayCommand]
    private async Task GoToWorkoutsAsync()
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<WorkoutsPage>());

    [RelayCommand]
    private async Task GoToSessionsAsync()
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<SessionsPage>());

    [RelayCommand]
    private async Task GoToAdminAsync()
    {
        await LoadAdminFlagAsync();
        if (!CanAccessAdmin) return;

        var page = _services.GetRequiredService<AdminPanelPage>();
        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var choice = await Application.Current!.MainPage!.DisplayActionSheet(
            "Add",
            "Cancel",
            null,
            "Exercise",
            "Workout",
            "Session");

        if (choice is null || choice == "Cancel") return;

        if (choice == "Exercise")
        {
            var page = _services.GetRequiredService<ExerciseEditPage>();
            var vm = (ExerciseEditViewModel)page.BindingContext!;
            vm.InitForCreate();
            await Application.Current!.MainPage!.Navigation.PushAsync(page);
        }
        else if (choice == "Workout")
        {
            var page = _services.GetRequiredService<WorkoutEditPage>();
            var vm = (WorkoutEditViewModel)page.BindingContext!;
            vm.InitForCreate();
            await Application.Current!.MainPage!.Navigation.PushAsync(page);
        }
        else if (choice == "Session")
        {
            var page = _services.GetRequiredService<SessionEditPage>();
            var vm = (SessionEditViewModel)page.BindingContext!;
            await vm.InitForCreateAsync();
            await Application.Current!.MainPage!.Navigation.PushAsync(page);
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _tokenStore.ClearAsync();
        await _sessionStore.ClearAsync();
        Application.Current!.MainPage = new NavigationPage(_services.GetRequiredService<LoginPage>());
    }
}
