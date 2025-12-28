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
    // Services: lokale DB (offline) + sync naar API + DI voor pages/VMs + auth/session stores.
    private readonly LocalDatabaseService _local;
    private readonly ISyncService _sync;
    private readonly IServiceProvider _services;
    private readonly ITokenStore _tokenStore;
    private readonly IUserSessionStore _sessionStore;

    // Debounce cancellation voor search typing (vermijdt te veel DB calls).
    private CancellationTokenSource? _searchCts;

    // Gate om LoadLocalAsync niet tegelijk te laten lopen (race conditions vermijden).
    private readonly SemaphoreSlim _loadGate = new(1, 1);

    // Flag om geen reload te triggeren wanneer SelectedCategory intern wordt aangepast.
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

        // Standaard categorie filter: "All" = geen filter.
        Categories.Add("All");
        SelectedCategory = "All";

        // Init: admin-visibility + eerste sync/refresh.
        _ = LoadAdminFlagAsync();
        _ = RefreshAsyncCore();
    }

    // UI collections: Items (CollectionView) + Categories (Picker).
    public ObservableCollection<LocalExercise> Items { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();

    // UI state/binding properties: refreshing + error + search + category + admin knop.
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private string? _searchText;
    [ObservableProperty] private string _selectedCategory = "All";
    [ObservableProperty] private bool _canAccessAdmin;

    // Case-insensitive check om te zien of de selected category nog bestaat.
    private static bool ContainsIgnoreCase(IEnumerable<string> list, string value)
        => list.Any(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));

    // Zet de picker waarde om naar een echte filter: "All" => null.
    private string? CategoryFilterOrNull()
    {
        if (string.IsNullOrWhiteSpace(SelectedCategory) ||
            string.Equals(SelectedCategory, "All", StringComparison.OrdinalIgnoreCase))
            return null;

        return SelectedCategory;
    }

    // Bepaalt of de Admin knop zichtbaar mag zijn op basis van opgeslagen roles.
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
        // Voorkomt meerdere refreshes tegelijk (bv. pull-to-refresh + OnAppearing).
        if (IsRefreshing) return;

        try
        {
            IsRefreshing = true;
            Error = null;

            // Sync eerst zodat lokale data up-to-date is.
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
        // Gate: DB reads + UI updates mogen niet overlappen.
        await _loadGate.WaitAsync();
        try
        {
            Error = null;

            // Gefilterde data voor de lijst (search + category).
            var data = await _local.GetExercisesAsync(
                search: SearchText,
                category: CategoryFilterOrNull());

            // Ongefilterde lijst voor het opbouwen van category opties.
            var allForCategories = await _local.GetExercisesAsync(search: null, category: null);

            var distinctCats = allForCategories
                .Select(x => x.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();

            // UI collections altijd updaten op de main thread.
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Items.Clear();
                foreach (var x in data)
                    Items.Add(x);

                var desired = new List<string> { "All" };
                desired.AddRange(distinctCats);

                // Alleen resetten als de lijst effectief veranderde (flikkeren vermijden).
                if (!SequenceEqualIgnoreCase(Categories, desired))
                {
                    Categories.Clear();
                    foreach (var c in desired)
                        Categories.Add(c);
                }
            });

            // Als gekozen category niet meer bestaat, terugzetten naar All zonder event loop.
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

    // Case-insensitive vergelijking om Categories updates te beperken.
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
        // Debounce: cancel vorige delay en start een nieuwe.
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(250, token);
                if (token.IsCancellationRequested) return;

                // LoadLocalAsync doet UI updates -> via MainThread.
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
        // Bij interne reset (naar All) niet opnieuw laden.
        if (_suppressCategoryReload) return;

        _ = MainThread.InvokeOnMainThreadAsync(async () => await LoadLocalAsync());
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        // Create flow: nieuwe VM state en navigeren naar edit page.
        var page = _services.GetRequiredService<ExerciseEditPage>();
        var vm = _services.GetRequiredService<ExerciseEditViewModel>();

        vm.InitForCreate();
        page.BindingContext = vm;

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task EditAsync(LocalExercise item)
    {
        // Edit flow: laad entity in VM en navigeer naar dezelfde edit page.
        var page = _services.GetRequiredService<ExerciseEditPage>();
        var vm = _services.GetRequiredService<ExerciseEditViewModel>();

        await vm.InitForEditAsync(item.LocalId);
        page.BindingContext = vm;

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task DeleteAsync(LocalExercise item)
    {
        // Confirm dialog voor soft delete.
        var ok = await Application.Current!.MainPage!.DisplayAlert(
            "Delete exercise",
            $"Delete '{item.Name}'?",
            "Yes",
            "No");

        if (!ok) return;

        // Soft delete zodat sync dit kan doorsturen naar de API.
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
        // Refresh roles (kan veranderd zijn) voordat je navigeert.
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
        // Logout: token + session wissen en root terugzetten naar LoginPage.
        await _tokenStore.ClearAsync();
        await _sessionStore.ClearAsync();
        Application.Current!.MainPage = new NavigationPage(_services.GetRequiredService<LoginPage>());
    }

    // Alias properties zodat XAML kortere Command names kan binden.
    public IAsyncRelayCommand GoWorkoutsCommand => GoToWorkoutsCommand;
    public IAsyncRelayCommand GoSessionsCommand => GoToSessionsCommand;
}
