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

/// <summary>Liveness endpoint that returns the currently deployed image and tag.</summary>
[ApiController]
[Route("healthz")]
public class HealthzController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public HealthzController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var imageName = _configuration["IMAGE_NAME"];
        if (string.IsNullOrWhiteSpace(imageName))
        {
            imageName = "unknown";
        }
        return Ok(new { status = "Healthy", image = imageName });
    }
}
