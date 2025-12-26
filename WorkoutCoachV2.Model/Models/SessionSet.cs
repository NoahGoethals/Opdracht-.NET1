// Represent a performed set inside a session; link Session × Exercise with reps/weight.

using System.ComponentModel.DataAnnotations;

namespace WorkoutCoachV2.Model.Models
{
    public class SessionSet : BaseEntity
    {
        // Reference the parent session.
        [Required]
        public int SessionId { get; set; }
        public Session Session { get; set; } = default!;

        // Reference the performed exercise.
        [Required]
        public int ExerciseId { get; set; }
        public Exercise Exercise { get; set; } = default!;

        // Order this set within the session (1..n).
        [Range(1, 200)]
        public int SetNumber { get; set; } = 1;

        // Store performance metrics.
        [Range(1, 500)]
        public int Reps { get; set; } = 5;

        [Range(0, 1000)]
        public double Weight { get; set; } = 0;

        // Capture optional intensity and notes.
        [Range(0, 10)]
        public double? Rpe { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }
    }
}
