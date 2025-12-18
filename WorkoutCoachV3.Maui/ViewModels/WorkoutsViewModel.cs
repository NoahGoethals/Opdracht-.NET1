using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutCoachV3.Maui.Data.LocalEntities;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class WorkoutsViewModel : ObservableObject
{
    private readonly LocalDatabaseService _local;
    private readonly ISyncService _sync;
    private readonly IServiceProvider _services;
    private readonly ITokenStore _tokenStore;

    public ObservableCollection<LocalWorkout> Items { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    [ObservableProperty] private string? searchText;

    public WorkoutsViewModel(LocalDatabaseService local, ISyncService sync, IServiceProvider services, ITokenStore tokenStore)
    {
        _local = local;
        _sync = sync;
        _services = services;
        _tokenStore = tokenStore;
    }

    public async Task RefreshAsync()
    {
        await LoadLocalAsync();

        try { await _sync.SyncAllAsync(); }
        catch { }

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
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText;
            var data = await _local.GetWorkoutsAsync(search);

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
    private async Task AddAsync()
    {
        var page = _services.GetRequiredService<WorkoutEditPage>();
        var vm = (WorkoutEditViewModel)page.BindingContext!;
        vm.InitForCreate();

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task EditAsync(LocalWorkout? item)
    {
        if (item is null) return;

        var page = _services.GetRequiredService<WorkoutEditPage>();
        var vm = (WorkoutEditViewModel)page.BindingContext!;
        await vm.InitForEditAsync(item.LocalId);

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task DeleteAsync(LocalWorkout? item)
    {
        if (item is null) return;

        var ok = await Application.Current!.MainPage!.DisplayAlert(
            "Delete workout",
            $"Delete '{item.Title}'?",
            "Delete",
            "Cancel");

        if (!ok) return;

        try
        {
            await _local.SoftDeleteWorkoutAsync(item.LocalId);

            try { await _sync.SyncAllAsync(); } catch { }

            await LoadLocalAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _tokenStore.ClearAsync();
        var loginPage = _services.GetRequiredService<LoginPage>();
        Application.Current!.MainPage = new NavigationPage(loginPage);
    }
}
