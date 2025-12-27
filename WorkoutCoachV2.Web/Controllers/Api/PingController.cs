using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorkoutCoachV2.Web.Controllers.Api;

[ApiController] // Eenvoudige health-check controller voor JWT-auth test.
[Route("api/[controller]")] // api/ping
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // Alleen bereikbaar met geldig JWT.
public class PingController : ControllerBase
{
    [HttpGet] // GET api/ping: bevestigt dat de API leeft en auth werkt.
    public IActionResult Get()
        => Ok(new { ok = true, utc = DateTime.UtcNow }); // JSON response met status + server-UTC tijd.
}
