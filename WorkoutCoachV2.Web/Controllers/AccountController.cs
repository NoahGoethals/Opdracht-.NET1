using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WorkoutCoachV2.Model.Models;
using WorkoutCoachV2.Web.Models;

namespace WorkoutCoachV2.Web.Controllers
{
    // Verzorgt login, registratie, logout en access denied voor de web-app (Identity).
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager; // Doet het effectieve inloggen/uitloggen.
        private readonly UserManager<ApplicationUser> _userManager; // Zoekt/maakt users en beheert rollen.

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager; // Dependency injection van sign-in logica.
            _userManager = userManager; // Dependency injection van user beheer.
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl; // Onthoudt waar terug naartoe na login.
            return View(new LoginViewModel()); // Toont het loginformulier.
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl; // Houdt returnUrl beschikbaar bij fouten.

            if (!ModelState.IsValid)
                return View(vm); // Validatiefouten: form opnieuw tonen.

            var user = await _userManager.FindByEmailAsync(vm.Email); // Zoekt user op email.
            if (user == null)
            {
                ModelState.AddModelError("", "Gebruiker niet gevonden."); // User bestaat niet.
                return View(vm);
            }

            if (user.IsBlocked)
            {
                ModelState.AddModelError("", "Deze gebruiker is geblokkeerd."); // Geblokkeerde accounts mogen niet inloggen.
                return View(vm);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                vm.Password,
                vm.RememberMe,
                lockoutOnFailure: false); // Controleert wachtwoord en maakt auth cookie.

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Ongeldige login."); // Wachtwoord fout.
                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl); // Veilig terugsturen naar interne pagina.

            return RedirectToAction("Index", "Home"); // Default na login.
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel()); // Toont registratieformulier.
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm); // Validatiefouten: form opnieuw tonen.

            var user = new ApplicationUser
            {
                UserName = vm.Email, // Loginnaam = email.
                Email = vm.Email,
                DisplayName = vm.DisplayName, // Extra eigenschap in eigen user class.
                IsBlocked = false // Nieuwe users starten actief.
            };

            var result = await _userManager.CreateAsync(user, vm.Password); // Maakt user aan + hashed password.
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description); // Identity fouten tonen in UI.

                return View(vm);
            }

            await _userManager.AddToRoleAsync(user, "User"); // Nieuwe user krijgt automatisch de User-rol.

            await _signInManager.SignInAsync(user, isPersistent: false); // Auto inloggen na registratie.
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync(); // Verwijdert auth cookie.
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View(); // Wordt gebruikt wanneer autorisatie faalt.
        }
    }
}
