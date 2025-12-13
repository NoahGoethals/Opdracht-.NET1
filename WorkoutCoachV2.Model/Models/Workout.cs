using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkoutCoachV2.Model.Models
{
    public class Workout : BaseEntity
    {
        public string Title { get; set; } = "";

        [Display(Name = "Scheduled")]
        public DateTime? ScheduledOn { get; set; }

        // Per-user data
        public string? OwnerId { get; set; }
        public ApplicationUser? Owner { get; set; }

        public ICollection<WorkoutExercise> Exercises { get; set; } = new List<WorkoutExercise>();
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}
