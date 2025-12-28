// DataAnnotations bevat attributen zoals [Required] om te garanderen dat relaties ingevuld zijn.
using System.ComponentModel.DataAnnotations;

namespace WorkoutCoachV3.Maui.Data.LocalEntities;

// Eén set binnen een sessie: koppelt sessie + oefening met reps/gewicht/etc.
public class LocalSessionSet : BaseLocalEntity
{
    // Verwijst naar de lokale sessie (Guid) waartoe deze set behoort.
    [Required]
    public Guid SessionLocalId { get; set; }

    // Verwijst naar de lokale oefening (Guid) die in deze set uitgevoerd werd.
    [Required]
    public Guid ExerciseLocalId { get; set; }

    // Volgnummer van de set binnen dezelfde oefening (standaard 1).
    public int SetNumber { get; set; } = 1;

    // Aantal herhalingen en gewicht voor deze set.
    public int Reps { get; set; }
    public double Weight { get; set; }

    // Optionele RPE (Rate of Perceived Exertion) en een korte notitie per set.
    public int? Rpe { get; set; }
    public string? Note { get; set; }

    // Aanduiding of de set effectief is afgewerkt (handig in UI checkboxes).
    public bool Completed { get; set; }
}
