using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using WorkoutCoachV3.Maui.Data.LocalEntities;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class ExercisesViewModel : ObservableObject
{
    private readonly LocalDatabaseService _local;
    private readonly ISyncService _sync;
    private readonly IServiceProvider _services;
    private readonly ITokenStore _tokenStore;
    private readonly IUserSessionStore _sessionStore;

    private CancellationTokenSource? _searchCts;

    private readonly SemaphoreSlim _loadGate = new(1, 1);

    private bool _suppressCategoryReload;

    public ExercisesViewModel(
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

        Categories.Add("All");
        SelectedCategory = "All";

        _ = LoadAdminFlagAsync();
        _ = RefreshAsyncCore();
    }

    public ObservableCollection<LocalExercise> Items { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();

    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private string? _searchText;
    [ObservableProperty] private string _selectedCategory = "All";
    [ObservableProperty] private bool _canAccessAdmin;

    private static bool ContainsIgnoreCase(IEnumerable<string> list, string value)
        => list.Any(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));

    private string? CategoryFilterOrNull()
    {
        if (string.IsNullOrWhiteSpace(SelectedCategory) ||
            string.Equals(SelectedCategory, "All", StringComparison.OrdinalIgnoreCase))
            return null;

        return SelectedCategory;
    }

    private async Task LoadAdminFlagAsync()
    {
        var roles = await _sessionStore.GetRolesAsync();
        CanAccessAdmin = roles.Any(r => r is "Admin" or "Moderator");
    }

    [RelayCommand]
    private async Task RefreshAsync()
        => await RefreshAsyncCore();

    private async Task RefreshAsyncCore()
    {
        if (IsRefreshing) return;

        try
        {
            IsRefreshing = true;
            Error = null;

            await _sync.SyncAllAsync();
            await LoadLocalAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task LoadLocalAsync()
    {
        await _loadGate.WaitAsync();
        try
        {
            Error = null;

            var data = await _local.GetExercisesAsync(
                search: SearchText,
                category: CategoryFilterOrNull());

            var allForCategories = await _local.GetExercisesAsync(search: null, category: null);

            var distinctCats = allForCategories
                .Select(x => x.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Items.Clear();
                foreach (var x in data)
                    Items.Add(x);

                var desired = new List<string> { "All" };
                desired.AddRange(distinctCats);

                if (!SequenceEqualIgnoreCase(Categories, desired))
                {
                    Categories.Clear();
                    foreach (var c in desired)
                        Categories.Add(c);
                }
            });

            if (!ContainsIgnoreCase(Categories, SelectedCategory))
            {
                _suppressCategoryReload = true;
                SelectedCategory = "All";
                _suppressCategoryReload = false;
            }
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    private static bool SequenceEqualIgnoreCase(IList<string> a, IList<string> b)
    {
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    partial void OnSearchTextChanged(string? value)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(250, token);
                if (token.IsCancellationRequested) return;

                await MainThread.InvokeOnMainThreadAsync(async () => await LoadLocalAsync());
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() => Error = ex.Message);
            }
        }, token);
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        if (_suppressCategoryReload) return;

        _ = MainThread.InvokeOnMainThreadAsync(async () => await LoadLocalAsync());
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var page = _services.GetRequiredService<ExerciseEditPage>();
        var vm = _services.GetRequiredService<ExerciseEditViewModel>();

        vm.InitForCreate();
        page.BindingContext = vm;

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task EditAsync(LocalExercise item)
    {
        var page = _services.GetRequiredService<ExerciseEditPage>();
        var vm = _services.GetRequiredService<ExerciseEditViewModel>();

        await vm.InitForEditAsync(item.LocalId);
        page.BindingContext = vm;

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task DeleteAsync(LocalExercise item)
    {
        var ok = await Application.Current!.MainPage!.DisplayAlert(
            "Delete exercise",
            $"Delete '{item.Name}'?",
            "Yes",
            "No");

        if (!ok) return;

        await _local.SoftDeleteExerciseAsync(item.LocalId);
        await LoadLocalAsync();
    }

    [RelayCommand]
    private async Task GoToWorkoutsAsync()
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<WorkoutsPage>());

    [RelayCommand]
    private async Task GoToSessionsAsync()
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<SessionsPage>());

    [RelayCommand]
    private async Task GoToStatsAsync()
        => await Application.Current!.MainPage!.Navigation.PushAsync(_services.GetRequiredService<StatsPage>());

    [RelayCommand]
    private async Task GoToAdminAsync()
    {
        await LoadAdminFlagAsync();

        if (!CanAccessAdmin)
        {
            await Application.Current!.MainPage!.DisplayAlert("Admin", "Not authorized.", "OK");
            return;
        }

        var page = _services.GetRequiredService<AdminPanelPage>();
        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _tokenStore.ClearAsync();
        await _sessionStore.ClearAsync();
        Application.Current!.MainPage = new NavigationPage(_services.GetRequiredService<LoginPage>());
    }

    public IAsyncRelayCommand GoWorkoutsCommand => GoToWorkoutsCommand;
    public IAsyncRelayCommand GoSessionsCommand => GoToSessionsCommand;
}
