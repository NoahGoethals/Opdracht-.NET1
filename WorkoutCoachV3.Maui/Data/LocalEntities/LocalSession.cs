// DataAnnotations bevat attributen zoals [Required] om validatie + DB constraints te sturen.
using System.ComponentModel.DataAnnotations;

namespace WorkoutCoachV3.Maui.Data.LocalEntities;

// Lokale sessie (workoutlog) die offline aangemaakt/bewerkt kan worden.
public class LocalSession : BaseLocalEntity
{
    // Titel is verplicht (bv. "Push Day", "Leg Day").
    [Required]
    public string Title { get; set; } = "";

    // Datum van de sessie (standaard vandaag).
    public DateTime Date { get; set; } = DateTime.Today;

    // Optionele beschrijving (bv. doel van de training).
    public string? Description { get; set; }

    // Link naar een server-workout (RemoteId) als deze sessie gebaseerd is op een bestaande workout.
    public int? WorkoutRemoteId { get; set; }

    // Start/eind van de sessie in UTC voor correcte tijd over timezones heen.
    public DateTime? StartedUtc { get; set; }
    public DateTime? EndedUtc { get; set; }
    // Algemene notities over de volledige sessie.
    public string? Notes { get; set; }
}
