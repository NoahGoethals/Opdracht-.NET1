using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkoutCoachV2.Model.Models
{
    public class Exercise : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "";

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Per-user data
        public string? OwnerId { get; set; }
        public ApplicationUser? Owner { get; set; }

        // Navigaties
        public ICollection<WorkoutExercise> InWorkouts { get; set; } = new List<WorkoutExercise>();
        public ICollection<SessionSet> SessionSets { get; set; } = new List<SessionSet>();
    }
}
