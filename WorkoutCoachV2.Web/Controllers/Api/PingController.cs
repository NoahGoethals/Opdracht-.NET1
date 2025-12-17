using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorkoutCoachV2.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PingController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
        => Ok(new { ok = true, utc = DateTime.UtcNow });
}
