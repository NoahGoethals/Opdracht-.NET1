using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class SessionsViewModel : ObservableObject
{
    private readonly LocalDatabaseService _local;
    private readonly IServiceProvider _services;

    public ObservableCollection<LocalDatabaseService.SessionListDisplay> Items { get; } = new();

    [ObservableProperty] private string? search;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    public SessionsViewModel(LocalDatabaseService local, IServiceProvider services)
    {
        _local = local;
        _services = services;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;

        try
        {
            var data = await _local.GetSessionsAsync(Search);
            Items.Clear();
            foreach (var s in data)
                Items.Add(s);
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
        var page = _services.GetRequiredService<SessionEditPage>();
        var vm = (SessionEditViewModel)page.BindingContext!;
        await vm.InitForCreateAsync();

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task EditAsync(Guid sessionLocalId)
    {
        var page = _services.GetRequiredService<SessionEditPage>();
        var vm = (SessionEditViewModel)page.BindingContext!;
        await vm.InitForEditAsync(sessionLocalId);

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task DeleteAsync(Guid sessionLocalId)
    {
        try
        {
            await _local.SoftDeleteSessionAsync(sessionLocalId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }

    [RelayCommand]
    private async Task OpenAsync(Guid sessionLocalId)
    {
        var page = _services.GetRequiredService<SessionDetailPage>();
        var vm = (SessionDetailViewModel)page.BindingContext!;
        await vm.InitAsync(sessionLocalId);

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }
}
