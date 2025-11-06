using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using WorkoutCoachV2.Model.Data;     
using WorkoutCoachV2.Model.Models;  

namespace WorkoutCoachV2.App.View
{
    public partial class EditWorkoutExercisesWindow : Window
    {
        private readonly int _workoutId;

        private ObservableCollection<Exercise> _available = new();
        private ObservableCollection<WorkoutExercise> _inWorkout = new();

        public EditWorkoutExercisesWindow(int workoutId)
        {
            InitializeComponent();
            _workoutId = workoutId;
            Loaded += async (_, __) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            using var scope = App.HostApp.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var workout = await db.Workouts
                .Include(w => w.Exercises)
                    .ThenInclude(we => we.Exercise)
                .FirstAsync(w => w.Id == _workoutId);

            var all = await db.Exercises
                .AsNoTracking()
                .OrderBy(e => e.Name)
                .ToListAsync();

            var usedIds = workout.Exercises.Select(we => we.ExerciseId).ToHashSet();

            _inWorkout = new ObservableCollection<WorkoutExercise>(
                workout.Exercises.OrderBy(we => we.Exercise.Name));

            _available = new ObservableCollection<Exercise>(all.Where(e => !usedIds.Contains(e.Id)));

            lbAvailable.ItemsSource = _available;
            dgInWorkout.ItemsSource = _inWorkout;
        }

        private static async Task<int> GetSuggestedRepsAsync(int exerciseId)
        {
            using var scope = App.HostApp.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var last = await db.WorkoutExercises
                .Where(we => we.ExerciseId == exerciseId)
                .OrderByDescending(we => we.WorkoutId)   
                .Select(we => (int?)we.Reps)
                .FirstOrDefaultAsync();

            return last ?? 5;
        }

        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (lbAvailable.SelectedItem is not Exercise ex) return;

            var defaultReps = await GetSuggestedRepsAsync(ex.Id);

            var dlg = new AskRepsWindow(defaultReps) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;

            var we = new WorkoutExercise
            {
                WorkoutId = _workoutId,
                ExerciseId = ex.Id,
                Exercise = ex,
                Reps = dlg.Reps
            };

            _inWorkout.Add(we);
            _available.Remove(ex);
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (dgInWorkout.SelectedItem is WorkoutExercise we)
            {
                if (we.Exercise != null) _available.Add(we.Exercise);
                _inWorkout.Remove(we);
            }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            using var scope = App.HostApp.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var workout = await db.Workouts
                .Include(w => w.Exercises)
                .FirstAsync(w => w.Id == _workoutId);

            var keep = _inWorkout.Select(x => x.ExerciseId).ToHashSet();
            var toRemove = workout.Exercises.Where(x => !keep.Contains(x.ExerciseId)).ToList();
            if (toRemove.Count > 0) db.WorkoutExercises.RemoveRange(toRemove);

            foreach (var item in _inWorkout)
            {
                var existing = workout.Exercises.FirstOrDefault(x => x.ExerciseId == item.ExerciseId);
                if (existing == null)
                {
                    workout.Exercises.Add(new WorkoutExercise
                    {
                        WorkoutId = _workoutId,
                        ExerciseId = item.ExerciseId,
                        Reps = item.Reps
                    });
                }
                else
                {
                    existing.Reps = item.Reps;
                    db.Entry(existing).State = EntityState.Modified;
                }
            }

            await db.SaveChangesAsync();
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) => Close();

        private void tbFilterLeft_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var term = tbFilterLeft.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(lbAvailable.ItemsSource);
            view.Filter = o =>
            {
                if (o is Exercise ex)
                    return string.IsNullOrEmpty(term) || ex.Name.ToLowerInvariant().Contains(term);
                return true;
            };
        }
    }
}
