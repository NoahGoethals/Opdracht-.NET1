// CRUD + zoeken/sorteren voor Workouts-tab (soft delete via IsDeleted).

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.App.Helpers;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.ViewModels
{
    public class WorkoutsViewModel : BaseViewModel
    {
        // DI: scope voor DbContext.
        private readonly IServiceScopeFactory _scopeFactory;

        // Data-binding: lijst + selectie.
        public ObservableCollection<Workout> Items { get; } = new();

        private Workout? _selected;
        public Workout? Selected
        {
            get => _selected;
            set { SetProperty(ref _selected, value); UpdateButtons(); }
        }

        // Zoekterm (filtert op titel).
        private string _search = "";
        public string Search
        {
            get => _search;
            set { if (SetProperty(ref _search, value)) _ = LoadAsync(); }
        }

        // Commands voor de toolbar.
        public RelayCommand AddCmd { get; }
        public RelayCommand EditCmd { get; }
        public RelayCommand DeleteCmd { get; }
        public RelayCommand RefreshCmd { get; }

        // Ctor: commands koppelen; geen autoload hier (view triggert LoadAsync).
        public WorkoutsViewModel(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;

            AddCmd = new RelayCommand(_ => Add());
            EditCmd = new RelayCommand(_ => Edit(), _ => Selected != null);
            DeleteCmd = new RelayCommand(_ => DeleteAsync(), _ => Selected != null);
            RefreshCmd = new RelayCommand(_ => _ = LoadAsync());
        }

        // Buttons en CanExecute updaten bij selectie-wijziging.
        private void UpdateButtons()
        {
            EditCmd.RaiseCanExecuteChanged();
            DeleteCmd.RaiseCanExecuteChanged();
        }

        // Ophalen van workouts (soft-deleted uitsluiten), zoekterm toepassen, sorteren op datum ↓.
        public async Task LoadAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var q = db.Workouts.AsNoTracking().Where(w => !w.IsDeleted);

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim();
                q = q.Where(w => w.Title.Contains(term));
            }

            var list = await q.OrderByDescending(w => w.ScheduledOn).ToListAsync();

            Items.Clear();
            foreach (var w in list) Items.Add(w);

            UpdateButtons();
        }

        // Nieuw: dialoog openen → resultaat bewaren → refresh.
        private void Add()
        {
            var win = new View.AddWorkoutWindow();
            if (win.ShowDialog() == true && win.Result is Workout w)
            {
                _ = SaveNewAsync(w);
            }
        }

        // Bewaar nieuwe workout (DB) en herlaad.
        private async Task SaveNewAsync(Workout w)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Workouts.Add(w);
            await db.SaveChangesAsync();
            await LoadAsync();
        }

        // Bewerken: maak kopie → dialoog → opgeslagen wijzigingen toepassen.
        private void Edit()
        {
            if (Selected is null) return;

            var copy = new Workout { Id = Selected.Id, Title = Selected.Title, ScheduledOn = Selected.ScheduledOn };
            var win = new View.AddWorkoutWindow(copy);
            if (win.ShowDialog() == true && win.Result is Workout updated)
            {
                _ = SaveEditAsync(updated);
            }
        }

        // Bewaar edits op bestaande workout.
        private async Task SaveEditAsync(Workout updated)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var entity = await db.Workouts.FirstAsync(x => x.Id == updated.Id);
            entity.Title = updated.Title;
            entity.ScheduledOn = updated.ScheduledOn;

            await db.SaveChangesAsync();
            await LoadAsync();
        }

        // Verwijderen: soft delete (IsDeleted = true) met bevestiging.
        private async void DeleteAsync()
        {
            if (Selected is null) return;

            if (MessageBox.Show($"Verwijder '{Selected.Title}'?", "Bevestigen",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var entity = await db.Workouts.FirstAsync(x => x.Id == Selected.Id);
            entity.IsDeleted = true;
            await db.SaveChangesAsync();
            await LoadAsync();
        }
    }
}
