using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkoutCoachV2.Model.Models
{
    public class Exercise : BaseEntity
    {
        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string Category { get; set; } = "";

        public string? Notes { get; set; }

        // Per-user data
        public string? OwnerId { get; set; }
        public ApplicationUser? Owner { get; set; }

        // Navigaties
        public ICollection<WorkoutExercise> InWorkouts { get; set; } = new List<WorkoutExercise>();
        public ICollection<SessionSet> SessionSets { get; set; } = new List<SessionSet>();
    }
}
