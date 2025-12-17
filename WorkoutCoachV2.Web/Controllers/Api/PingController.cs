using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorkoutCoachV2.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class PingController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public IActionResult Get()
        => Ok(new { ok = true, user = User.Identity?.Name });
}
