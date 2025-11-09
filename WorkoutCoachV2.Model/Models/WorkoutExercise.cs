// Koppelt Workout en Exercise; bewaart plan-velden (Reps, WeightKg).
// Composite key (WorkoutId, ExerciseId) staat ingesteld in AppDbContext.

using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutCoachV2.Model.Models
{
    public class WorkoutExercise
    {
        // FK naar workout + navigatie.
        public int WorkoutId { get; set; }
        public Workout Workout { get; set; } = default!;

        // FK naar oefening + navigatie.
        public int ExerciseId { get; set; }
        public Exercise Exercise { get; set; } = default!;

        // Geplande herhalingen binnen de workout.
        public int Reps { get; set; }

        // Gepland gewicht (kg); null/0 betekent vrij invullen.
        [Column(TypeName = "float")]
        public double? WeightKg { get; set; } = 0;
    }
}
