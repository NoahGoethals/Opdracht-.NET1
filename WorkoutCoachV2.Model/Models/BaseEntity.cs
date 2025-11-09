// Basis-entity met Id, timestamps en soft-delete.

using System;

namespace WorkoutCoachV2.Model.Models
{
    public abstract class BaseEntity
    {
        // Primaire sleutel (int).
        public int Id { get; set; }

        // UTC aanmaakmoment.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // UTC laatst gewijzigd (optioneel).
        public DateTime? UpdatedAt { get; set; }

        // Soft-delete marker: true = logisch verwijderd.
        public bool IsDeleted { get; set; } = false;
    }
}
