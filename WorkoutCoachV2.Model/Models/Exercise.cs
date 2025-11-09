// Oefening met naam/categorie en relaties naar workouts & sessies.

using System.Collections.Generic;

namespace WorkoutCoachV2.Model.Models
{
    public class Exercise : BaseEntity
    {
        // Verplichte naam (bv. "Bench Press").
        public string Name { get; set; } = "";

        // Optionele categorie (bv. "Chest").
        public string? Category { get; set; }

        // Optionele notities over de oefening.
        public string? Notes { get; set; }

        // Koppeling naar workouts via koppelentiteit.
        public ICollection<WorkoutExercise> InWorkouts { get; set; } = new List<WorkoutExercise>();

        // Sets waarin deze oefening effectief werd uitgevoerd.
        public ICollection<SessionSet> SessionSets { get; set; } = new List<SessionSet>();
    }
}
