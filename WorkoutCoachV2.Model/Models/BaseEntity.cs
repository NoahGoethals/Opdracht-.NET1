
using System;
using System.ComponentModel.DataAnnotations;

namespace WorkoutCoachV2.Model.Models
{
    public abstract class BaseEntity
    {
        // Primaire sleutel (int).
        public int Id { get; set; }

        // Aanmaakmoment (UTC).
        // In de UI tonen we dit als "Created At".
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Laatst gewijzigd (UTC, optioneel).
        // In de UI tonen we dit als "Updated At".
        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        // Soft-delete marker: true = logisch verwijderd (niet fysiek uit DB).
        public bool IsDeleted { get; set; } = false;
    }
}
