using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WorkoutCoachV2.App.Helpers;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WorkoutCoachV2.App.ViewModels
{
    public class SessionsViewModel : BaseViewModel
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ObservableCollection<Session> Items { get; } = new();
        private Session? _selected;
        public Session? Selected { get => _selected; set => Set(ref _selected, value); }

        public SessionsViewModel(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var list = await db.Sessions
                .AsNoTracking()
                .Include(s => s.Workout)
                .Include(s => s.Sets).ThenInclude(ss => ss.Exercise)
                .OrderByDescending(s => s.PerformedOn)
                .ToListAsync();

            Items.Clear();
            foreach (var s in list) Items.Add(s);
            if (Items.Count > 0) Selected = Items[0];
        }
    }
}
