using Microsoft.AspNetCore.Identity;

namespace PetroVisorLite.Infrastructure.Identity;

/// <summary>
/// Application user extending ASP.NET Core Identity's <see cref="IdentityUser"/>.
/// Add domain-specific profile fields here as needed (e.g. DisplayName).
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}

/// <summary>Role names used throughout the application. Kept as constants to avoid typos in [Authorize(Roles = ...)] attributes.</summary>
public static class Roles
{
    public const string Engineer = "Engineer";
    public const string Viewer = "Viewer";
}
