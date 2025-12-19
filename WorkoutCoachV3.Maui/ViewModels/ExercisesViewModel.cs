using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    private CancellationTokenSource? _searchDebounceCts;

    public ObservableCollection<LocalExercise> Items { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private string? error;

    [ObservableProperty] private string? searchText;
    [ObservableProperty] private string selectedCategory = "All";

    public ExercisesViewModel(
        LocalDatabaseService local,
        ISyncService sync,
        IServiceProvider services,
        ITokenStore tokenStore)
    {
        _local = local;
        _sync = sync;
        _services = services;
        _tokenStore = tokenStore;

        Categories.Add("All");
    }

    public async Task RefreshAsync()
    {
        await LoadLocalAsync();

        try { await _sync.SyncAllAsync(); }
        catch { /* silent */ }

        await LoadLocalAsync();
    }

    [RelayCommand]
    public async Task LoadLocalAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            var cat = SelectedCategory == "All" ? null : SelectedCategory;
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

            var data = await _local.GetExercisesAsync(search: search, category: cat);

            Items.Clear();
            foreach (var x in data)
                Items.Add(x);

            var distinctCats = data
                .Select(x => x.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();

            Categories.Clear();
            Categories.Add("All");
            foreach (var c in distinctCats)
                Categories.Add(c);

            if (!Categories.Contains(SelectedCategory))
                SelectedCategory = "All";
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
    private async Task RefreshCommandAsync()
    {
        if (IsBusy) return;

        IsRefreshing = true;
        try
        {
            await RefreshAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        _ = LoadLocalAsync();
    }

    partial void OnSearchTextChanged(string? value)
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts = new CancellationTokenSource();
        var token = _searchDebounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(350, token);
                if (token.IsCancellationRequested) return;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await LoadLocalAsync();
                });
            }
            catch {  }
        }, token);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var page = _services.GetRequiredService<ExerciseEditPage>();
        var vm = (ExerciseEditViewModel)page.BindingContext!;
        vm.InitForCreate();

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task EditAsync(LocalExercise? item)
    {
        if (item is null) return;

        var page = _services.GetRequiredService<ExerciseEditPage>();
        var vm = (ExerciseEditViewModel)page.BindingContext!;
        await vm.InitForEditAsync(item.LocalId);

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task DeleteAsync(LocalExercise? item)
    {
        if (item is null) return;

        var ok = await Application.Current!.MainPage!.DisplayAlert(
            "Delete exercise",
            $"Delete '{item.Name}'?",
            "Delete",
            "Cancel");

        if (!ok) return;

        try
        {
            await _local.SoftDeleteExerciseAsync(item.LocalId);

            try { await _sync.SyncAllAsync(); } catch { }

            await LoadLocalAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }


    [RelayCommand]
    private async Task GoToWorkoutsAsync()
    {
        var page = _services.GetRequiredService<WorkoutsPage>();
        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task GoToSessionsAsync()
    {
        var page = _services.GetRequiredService<SessionsPage>();
        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _tokenStore.ClearAsync();
        var loginPage = _services.GetRequiredService<LoginPage>();
        Application.Current!.MainPage = new NavigationPage(loginPage);
    }
}
