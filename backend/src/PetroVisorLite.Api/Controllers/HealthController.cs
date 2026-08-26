using Microsoft.AspNetCore.Mvc;

namespace PetroVisorLite.Api.Controllers;

/// <summary>Simple liveness/health endpoint to prove the API host builds and runs.</summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "Healthy" });
}
