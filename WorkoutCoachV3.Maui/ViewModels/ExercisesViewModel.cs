using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV3.Maui.Pages;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class ExercisesViewModel : ObservableObject
{
    private readonly IExercisesApi _api;
    private readonly ITokenStore _tokens;
    private readonly IServiceProvider _services;

    public ObservableCollection<ExerciseDto> Items { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    [ObservableProperty] private string? searchText;
    [ObservableProperty] private string selectedCategory = "All";
    [ObservableProperty] private string sort = "name";

    public ExercisesViewModel(IExercisesApi api, ITokenStore tokens, IServiceProvider services)
    {
        _api = api;
        _tokens = tokens;
        _services = services;

        Categories.Add("All");
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        Error = null;

        try
        {
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
