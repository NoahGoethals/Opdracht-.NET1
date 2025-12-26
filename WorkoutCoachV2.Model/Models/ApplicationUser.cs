// Identity-gebruiker met extra schermnaam en blokkeer-vlag.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace WorkoutCoachV2.Model.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Weergavenaam voor in de UI (los van UserName/Email).
        [Required]
        [MaxLength(50)]
        public string DisplayName { get; set; } = "";

        // Indien true mag de gebruiker niet meer inloggen/handelen.
        public bool IsBlocked { get; set; } = false;
    }
}
