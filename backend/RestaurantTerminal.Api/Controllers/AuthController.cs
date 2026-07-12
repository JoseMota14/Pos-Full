using Microsoft.AspNetCore.Mvc;

namespace RestaurantTerminal.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("login")]
    public ActionResult<object> Login()
    {
        return Ok(new { token = "prototype-local-session", displayName = "Prototype User" });
    }
}
