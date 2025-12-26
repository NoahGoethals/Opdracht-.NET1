// Koppelt Workout en Exercise; bewaart plan-velden (Reps, WeightKg).
// Composite key (WorkoutId, ExerciseId) staat ingesteld in AppDbContext.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutCoachV2.Model.Models
{
    public class WorkoutExercise
    {
        // FK naar workout + navigatie.
        [Required]
        public int WorkoutId { get; set; }
        public Workout Workout { get; set; } = default!;

        // FK naar oefening + navigatie.
        [Required]
        public int ExerciseId { get; set; }
        public Exercise Exercise { get; set; } = default!;

        // Geplande herhalingen binnen de workout.
        [Range(1, 200)]
        public int Reps { get; set; } = 5;

        // Gepland gewicht (kg); null betekent "vrij invullen".
        [Range(0, 1000)]
        [Column(TypeName = "float")]
        public double? WeightKg { get; set; } = 0;
    }
}
