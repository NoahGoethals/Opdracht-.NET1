namespace WorkoutCoachV2.Web.Models
{
    // Model voor de errorpagina: bevat RequestId zodat een foutmelding kan gelinkt worden aan een specifieke request/log.
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        // Geeft aan of de UI de RequestId moet tonen (alleen als die effectief bestaat).
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
