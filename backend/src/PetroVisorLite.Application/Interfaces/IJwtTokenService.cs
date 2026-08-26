namespace PetroVisorLite.Application.Interfaces;

/// <summary>Issues JWT access tokens for authenticated users. Implemented in Infrastructure.</summary>
public interface IJwtTokenService
{
    /// <summary>Creates a signed JWT containing the user's id, username/email, and role claims.</summary>
    string CreateToken(string userId, string userName, string email, IEnumerable<string> roles);
}
