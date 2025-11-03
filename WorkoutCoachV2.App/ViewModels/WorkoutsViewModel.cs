using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.App.Helpers;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.ViewModels
{
    public class WorkoutsViewModel : BaseViewModel
    {
        private readonly IServiceScopeFactory _scopeFactory;
        public ObservableCollection<Workout> Items { get; } = new();

        private Workout _selected;
        public Workout Selected
        {
            get => _selected;
            set => SetProperty(ref _selected, value);
        }

        public RelayCommand RefreshCmd { get; }

        public WorkoutsViewModel(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            RefreshCmd = new RelayCommand(_ => _ = LoadAsync());
        }

        public async Task LoadAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var list = await db.Workouts.AsNoTracking()
                .Where(w => !w.IsDeleted)
                .OrderByDescending(w => w.ScheduledOn)
                .ToListAsync();

            Items.Clear();
            foreach (var w in list) Items.Add(w);
        }
    }
}
