using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV3.Maui.Messages;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class ExercisesViewModel : ObservableObject
{
    private readonly IExercisesApi _api;
    private readonly ITokenStore _tokens;
    private readonly IServiceProvider _services;
    private readonly IApiHealthService _health;

    public ObservableCollection<ExerciseDto> Items { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    [ObservableProperty] private string? searchText;
    [ObservableProperty] private string selectedCategory = "All";
    [ObservableProperty] private string sort = "name";

    public ExercisesViewModel(IExercisesApi api, ITokenStore tokens, IServiceProvider services, IApiHealthService health)
    {
        _api = api;
        _tokens = tokens;
        _services = services;
        _health = health;

        Categories.Add("All");

        WeakReferenceMessenger.Default.Register<ExercisesChangedMessage>(this, async (_, __) =>
        {
            await LoadAsync();
        });
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        Error = null;

        try
        {
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                var ok = await _health.IsApiReachableAsync();
                if (ok) break;

                Error = "API is nog aan het opstarten… (probeer opnieuw)";
                await Task.Delay(2000);
            }

            if (!await _health.IsApiReachableAsync())
            {
                Error = "API is niet bereikbaar. Start WorkoutCoachV2.Web (Multiple startup projects).";
                return;
            }

            var category = SelectedCategory == "All" ? null : SelectedCategory;

            var data = await _api.GetAllAsync(
                search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                category: category,
                sort: Sort
            );

            Items.Clear();
            foreach (var x in data)
                Items.Add(x);

            var distinct = data
                .Select(x => x.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();

            Categories.Clear();
            Categories.Add("All");
            foreach (var c in distinct)
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
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task AddAsync()
    {
        var page = _services.GetRequiredService<ExerciseEditPage>();
        var vm = (ExerciseEditViewModel)page.BindingContext!;
        vm.InitForCreate();

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task EditAsync(ExerciseDto item)
    {
        var page = _services.GetRequiredService<ExerciseEditPage>();
        var vm = (ExerciseEditViewModel)page.BindingContext!;
        vm.InitForEdit(item);

        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task DeleteAsync(ExerciseDto item)
    {
        var ok = await Application.Current!.MainPage!.DisplayAlert(
            "Delete exercise",
            $"Delete '{item.Name}'?",
            "Delete",
            "Cancel");

        if (!ok) return;

        try
        {
            await _api.DeleteAsync(item.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _tokens.ClearAsync();

        var loginPage = _services.GetRequiredService<LoginPage>();
        Application.Current!.MainPage = new NavigationPage(loginPage);
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        _ = LoadAsync();
    }
}
