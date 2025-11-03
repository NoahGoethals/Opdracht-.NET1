using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
    public class ExercisesViewModel : BaseViewModel
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ObservableCollection<Exercise> Items { get; } = new();
        private Exercise? _selected;
        public Exercise? Selected { get => _selected; set => Set(ref _selected, value); }

        private string _search = "";
        public string Search { get => _search; set { if (value != _search) { _search = value; Raise(); _ = LoadAsync(); } } }

        public RelayCommand AddCmd { get; }
        public RelayCommand EditCmd { get; }
        public RelayCommand DeleteCmd { get; }
        public RelayCommand RefreshCmd { get; }

        public ExercisesViewModel(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;

            AddCmd = new RelayCommand(_ => Add());
            EditCmd = new RelayCommand(_ => Edit(), _ => Selected != null);
            DeleteCmd = new RelayCommand(_ => DeleteAsync(), _ => Selected != null);
            RefreshCmd = new RelayCommand(_ => _ = LoadAsync());
        }

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

        private void Add()
        {
            var win = new View.AddExerciseWindow(); 
            if (win.ShowDialog() == true && win.Result is Exercise e)
            {
                _ = SaveNewAsync(e);
            }
        }

        private async Task SaveNewAsync(Exercise e)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Exercises.Add(e);
            await db.SaveChangesAsync();
            await LoadAsync();
        }

        private void Edit()
        {
            if (Selected is null) return;
            var copy = new Exercise { Id = Selected.Id, Name = Selected.Name, Category = Selected.Category };
            var win = new View.AddExerciseWindow(copy);
            if (win.ShowDialog() == true && win.Result is Exercise updated)
            {
                _ = SaveEditAsync(updated);
            }
        }

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

        private async void DeleteAsync()
        {
            if (Selected is null) return;
            if (MessageBox.Show($"Verwijder '{Selected.Name}'?", "Bevestigen",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.Exercises.FirstAsync(x => x.Id == Selected.Id);
            entity.IsDeleted = true;
            await db.SaveChangesAsync();
            await LoadAsync();
        }
    }
}
