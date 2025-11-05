using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WorkoutCoachV2.App.Helpers;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.ViewModels
{
    public partial class SessionsViewModel : BaseViewModel
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public SessionsViewModel(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;

            RefreshCmd = new RelayCommand(_ => _ = LoadAsync());
            AddSessionCmd = new RelayCommand(_ => _ = AddSessionAsync());
            ExportSessionsCmd = new RelayCommand(_ => _ = ExportSessionsAsync());
            ImportSessionsCmd = new RelayCommand(_ => _ = ImportSessionsAsync());
            CalcStatsCmd = new RelayCommand(_ => _ = CalcStatsAsync());

            ToDate = DateTime.Today;
            FromDate = ToDate.AddDays(-7);

            _ = LoadAsync();
        }

        public ObservableCollection<SessionListItem> Items { get; } = new();

        private SessionListItem? _selected;
        public SessionListItem? Selected
        {
            get => _selected;
            set => SetProperty(ref _selected, value);
        }

        public RelayCommand RefreshCmd { get; }
        public RelayCommand AddSessionCmd { get; }
        public RelayCommand ExportSessionsCmd { get; }
        public RelayCommand ImportSessionsCmd { get; }
        public RelayCommand CalcStatsCmd { get; }

        public async Task LoadAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var list = await db.Sessions
                .AsNoTracking()
                .Include(s => s.Sets)
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

        private async Task AddSessionAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var session = new Session  
            {
                Title = "Nieuwe sessie",
                Date = DateTime.Today
            };

            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            await LoadAsync();
            Selected = Items.FirstOrDefault(i => i.Id == session.Id);
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
