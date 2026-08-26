using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PetroVisorLite.Application.Dtos;
using PetroVisorLite.Application.Interfaces;
using PetroVisorLite.Infrastructure.Identity;

namespace PetroVisorLite.Api.Controllers;

/// <summary>Login/registration against ASP.NET Core Identity, issuing JWTs on success.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(UserManager<ApplicationUser> userManager, IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtTokenService.CreateToken(user.Id, user.UserName ?? user.Email!, user.Email!, roles);

        return Ok(new LoginResponseDto(token, user.Email!, roles.ToList(), DateTime.UtcNow.AddMinutes(60)));
    }

    /// <summary>
    /// Registers a new user with the given role ("Engineer" or "Viewer"). Optional convenience
    /// endpoint — seeded demo users already exist for local testing via /api/auth/login.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Register([FromBody] RegisterRequestDto request)
    {
        if (request.Role != Roles.Engineer && request.Role != Roles.Viewer)
        {
            return BadRequest(new { message = $"Role must be '{Roles.Engineer}' or '{Roles.Viewer}'." });
        }

        var user = new ApplicationUser { UserName = request.Email, Email = request.Email, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        await _userManager.AddToRoleAsync(user, request.Role);

        var token = _jwtTokenService.CreateToken(user.Id, user.UserName!, user.Email!, new[] { request.Role });
        return Ok(new LoginResponseDto(token, user.Email!, new[] { request.Role }, DateTime.UtcNow.AddMinutes(60)));
    }
}
