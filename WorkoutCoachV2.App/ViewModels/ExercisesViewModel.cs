// ViewModel voor Exercises-tab: lijst, zoeken, CRUD en soft-delete.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WorkoutCoachV2.App.Helpers;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.ViewModels
{
    public partial class ExercisesViewModel : BaseViewModel
    {
        // DI: maakt per actie een scope aan om AppDbContext te verkrijgen.
        private readonly IServiceScopeFactory _scopeFactory;

        // Bindingsbron voor de DataGrid.
        public ObservableCollection<Exercise> Items { get; } = new();

        // Geselecteerde oefening (stuurt Edit/Delete knopstatus).
        private Exercise? _selected;
        public Exercise? Selected
        {
            get => _selected;
            set
            {
                if (SetProperty(ref _selected, value))
                {
                    EditCmd.RaiseCanExecuteChanged();
                    DeleteCmd.RaiseCanExecuteChanged();
                }
            }
        }

        // Zoekterm; triggert herladen.
        private string _search = "";
        public string Search
        {
            get => _search;
            set
            {
                if (SetProperty(ref _search, value))
                    _ = LoadAsync();
            }
        }

        // Commands voor UI-knoppen.
        public RelayCommand AddCmd { get; }
        public RelayCommand EditCmd { get; }
        public RelayCommand DeleteCmd { get; }
        public RelayCommand RefreshCmd { get; }

        // Init: commands koppelen en eerste load starten.
        public ExercisesViewModel(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;

            AddCmd = new RelayCommand(_ => Add());
            EditCmd = new RelayCommand(_ => Edit(), _ => Selected != null);
            DeleteCmd = new RelayCommand(_ => DeleteAsync(), _ => Selected != null);
            RefreshCmd = new RelayCommand(_ => _ = LoadAsync());

            _ = LoadAsync();
        }

        // Leest oefeningen (zonder soft-deleted) met optionele filter.
        public async Task LoadAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var q = db.Exercises.AsNoTracking().Where(e => !e.IsDeleted);

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim();
                q = q.Where(e => e.Name.Contains(term) || (e.Category ?? "").Contains(term));
            }

            var list = await q.OrderBy(e => e.Name).ToListAsync();

            Items.Clear();
            foreach (var e in list) Items.Add(e);
        }

        // Nieuw: opent dialoog en bewaart resultaat.
        private void Add()
        {
            var win = new View.AddExerciseWindow();
            if (win.ShowDialog() == true && win.Result is Exercise e)
                _ = SaveNewAsync(e);
        }

        // Persist van nieuw item + herladen.
        private async Task SaveNewAsync(Exercise e)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Exercises.Add(e);
            await db.SaveChangesAsync();
            await LoadAsync();
        }

        // Bewerken: opent dialoog met kopie van geselecteerde oefening.
        private void Edit()
        {
            if (Selected is null) return;

            var copy = new Exercise
            {
                Id = Selected.Id,
                Name = Selected.Name,
                Category = Selected.Category
            };

            var win = new View.AddExerciseWindow(copy);
            if (win.ShowDialog() == true && win.Result is Exercise updated)
                _ = SaveEditAsync(updated);
        }

        // Persist van bewerkte data + herladen.
        private async Task SaveEditAsync(Exercise updated)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.Exercises.FirstAsync(x => x.Id == updated.Id);
            entity.Name = updated.Name;
            entity.Category = updated.Category;
            await db.SaveChangesAsync();
            await LoadAsync();
        }

        // Verwijderen: soft-delete (IsDeleted = true) en herladen.
        private async void DeleteAsync()
        {
            if (Selected is null) return;

            if (MessageBox.Show($"Verwijder '{Selected.Name}'?", "Bevestigen",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.Exercises.FirstAsync(x => x.Id == Selected.Id);
            entity.IsDeleted = true;
            await db.SaveChangesAsync();
            await LoadAsync();
        }
    }
}
