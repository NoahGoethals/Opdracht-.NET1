using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutCoachV2.Model.Models
{
    /// <summary>
    /// Koppelt Workout en Exercise en bewaart extra velden (Reps, WeightKg).
    /// Composite key (WorkoutId, ExerciseId) wordt in AppDbContext geconfigureerd.
    /// </summary>
    public class WorkoutExercise
    {
        public int WorkoutId { get; set; }
        public Workout Workout { get; set; } = default!;

        public int ExerciseId { get; set; }
        public Exercise Exercise { get; set; } = default!;

        /// <summary>
        /// Geplande herhalingen voor deze oefening binnen de workout.
        /// </summary>
        public int Reps { get; set; }

        /// <summary>
        /// Gepland gewicht (in kg) voor deze oefening binnen de workout.
        /// Nullable zodat 0/geen gewicht kan.
        /// </summary>
        [Column(TypeName = "float")]
        public double? WeightKg { get; set; } = 0;
    }
}
