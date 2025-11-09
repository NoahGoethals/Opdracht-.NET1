// Sessie (trainingmoment) met datum, titel, beschrijving en sets.

using System;
using System.Collections.Generic;

namespace WorkoutCoachV2.Model.Models
{
    public class Session : BaseEntity
    {
        // Titel van de sessie (bv. "Push Day").
        public string Title { get; set; } = "";

        // Datum waarop de sessie plaatsvond.
        public DateTime Date { get; set; } = DateTime.Today;

        // Optionele beschrijving/notities.
        public string? Description { get; set; }

        // Uitgevoerde sets binnen deze sessie.
        public ICollection<SessionSet> Sets { get; set; } = new List<SessionSet>();
    }
}
