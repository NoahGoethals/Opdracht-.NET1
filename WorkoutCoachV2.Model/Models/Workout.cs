// Workout-definitie (planning/template) met optionele datum en gekoppelde oefeningen.

using System;
using System.Collections.Generic;

namespace WorkoutCoachV2.Model.Models
{
    public class Workout : BaseEntity
    {
        // Titel van de workout (bv. "Full Body A").
        public string Title { get; set; } = "";

        // Optioneel: geplande datum.
        public DateTime? ScheduledOn { get; set; }

        // Oefeningen (via koppelentiteit) die bij deze workout horen.
        public ICollection<WorkoutExercise> Exercises { get; set; } = new List<WorkoutExercise>();

        // Historiek van sessies die uit deze workout voortkomen (niet verplicht).
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}
