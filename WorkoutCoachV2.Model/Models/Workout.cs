
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkoutCoachV2.Model.Models
{
    public class Workout : BaseEntity
    {
        // Titel van de workout (bv. "Full Body A").
        public string Title { get; set; } = string.Empty;

        // Optioneel: geplande datum van deze workout.
        // In de UI wordt dit label getoond als "Scheduled".
        [Display(Name = "Scheduled")]
        public DateTime? ScheduledOn { get; set; }

        // Oefeningen die bij deze workout horen (via koppelentiteit WorkoutExercise).
        public ICollection<WorkoutExercise> Exercises { get; set; } = new List<WorkoutExercise>();

        // Sessies die uitgevoerd zijn op basis van deze workout (historiek).
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}
