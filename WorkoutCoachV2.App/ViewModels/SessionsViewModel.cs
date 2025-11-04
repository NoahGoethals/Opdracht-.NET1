using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.App.Helpers;        // voor BaseViewModel/RelayCommand
using WorkoutCoachV2.Model.Data;         // voor AppDbContext

namespace WorkoutCoachV2.App.ViewModels
{
    public class SessionsViewModel : BaseViewModel
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ObservableCollection<SessionListItem> Items { get; } = new();
        private SessionListItem? _selected;
        public SessionListItem? Selected { get => _selected; set => SetProperty(ref _selected, value); }

        public RelayCommand RefreshCmd { get; }

        public SessionsViewModel(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            RefreshCmd = new RelayCommand(_ => _ = LoadAsync());
            _ = LoadAsync(); // eerste keer automatisch laden
        }

        public async Task LoadAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var list = await db.Sessions
                .AsNoTracking()
                .OrderByDescending(s => s.Date)
                .Select(s => new SessionListItem
                {
                    Id = s.Id,
                    Title = s.Title,
                    Date = s.Date,
                    SetCount = s.Sets.Count
                })
                .ToListAsync();

            Items.Clear();
            foreach (var it in list) Items.Add(it);
        }
    }

    public class SessionListItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public DateTime Date { get; set; }
        public int SetCount { get; set; }
    }
}
