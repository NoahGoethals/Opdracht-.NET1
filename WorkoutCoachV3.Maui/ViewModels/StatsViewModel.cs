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
    // Services: stats uit lokale DB + sync + DI (navigatie) + logout/admin info.
    private readonly LocalDatabaseService _local;
    private readonly ISyncService _sync;
    private readonly IServiceProvider _services;
    private readonly ITokenStore _tokenStore;
    private readonly IUserSessionStore _sessionStore;

    // Picker datasource + "Top exercises" tabel datasource.
    public ObservableCollection<ExercisePickItem> ExerciseOptions { get; } = new();
    public ObservableCollection<LocalDatabaseService.ExerciseStatsRow> TopExercises { get; } = new();

    // Filter: gekozen exercise (null = All).
    [ObservableProperty] private ExercisePickItem? selectedExercise;

    // Filter: datumbereik (default = laatste maand).
    [ObservableProperty] private DateTime fromDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime toDate = DateTime.Today;

    // Summary blokken op de UI.
    [ObservableProperty] private int sessionsCount;
    [ObservableProperty] private int setsCount;
    [ObservableProperty] private int totalReps;
    [ObservableProperty] private double totalVolumeKg;

    // UI state: busy/refresh + error label.
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

        // Init admin flag zodat Admin-knop klopt.
        _ = LoadAdminFlagAsync();
    }

    private async Task LoadAdminFlagAsync()
    {
        // Roles komen uit session store (server -> login -> opgeslagen).
        CanAccessAdmin = await _sessionStore.IsInAnyRoleAsync("Admin", "Moderator");
    }

    public sealed class ExercisePickItem
    {
        // LocalId = filter value (null betekent "All exercises").
        public Guid? ExerciseLocalId { get; init; }
        public string Name { get; init; } = "";
        public override string ToString() => Name;
    }

    public async Task InitAsync()
    {
        // Called vanuit page OnAppearing: options laden en default selection zetten.
        await LoadAdminFlagAsync();
        await LoadExerciseOptionsAsync();
        SelectedExercise ??= ExerciseOptions.FirstOrDefault();
    }

    private async Task LoadExerciseOptionsAsync()
    {
        // Picker vullen: eerst "All", daarna alle exercises uit lokale DB.
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
        // Load = bereken stats enkel op basis van lokale data.
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
        // Apply = eerst sync (best effort), daarna Load voor correcte cijfers.
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
        // Pull-to-refresh: admin flag updaten + sync + reload.
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
        // Navigatie: tab-achtige knoppen bovenaan.
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<ExercisesPage>());

    [RelayCommand]
    private async Task GoToWorkoutsAsync()
        // Navigatie: tab-achtige knoppen bovenaan.
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<WorkoutsPage>());

    [RelayCommand]
    private async Task GoToSessionsAsync()
        // Navigatie: tab-achtige knoppen bovenaan.
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<SessionsPage>());

    [RelayCommand]
    private async Task GoToAdminAsync()
    {
        // Admin pagina openen alleen als role het toelaat.
        await LoadAdminFlagAsync();
        if (!CanAccessAdmin) return;

        var page = _services.GetRequiredService<AdminPanelPage>();
        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        // Quick add menu vanuit StatsPage (keuze tussen 3 entities).
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
        // Logout: token + session wissen en root naar LoginPage resetten.
        await _tokenStore.ClearAsync();
        await _sessionStore.ClearAsync();
        Application.Current!.MainPage = new NavigationPage(_services.GetRequiredService<LoginPage>());
    }
}
