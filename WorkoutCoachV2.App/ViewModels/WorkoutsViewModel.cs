using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WorkoutCoachV2.App.ViewModels
{
    public class WorkoutsViewModel : BaseViewModel
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ObservableCollection<Workout> Items { get; } = new();

        public WorkoutsViewModel(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task LoadAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // LINQ query-syntax voorbeeld
            var list = await (from w in db.Workouts
                              where !w.IsDeleted
                              orderby w.ScheduledOn descending
                              select w).AsNoTracking().ToListAsync();

            Items.Clear();
            foreach (var w in list) Items.Add(w);
        }
    }
}
