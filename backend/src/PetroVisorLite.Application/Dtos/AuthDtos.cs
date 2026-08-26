namespace PetroVisorLite.Application.Dtos;

public record LoginRequestDto(string Email, string Password);

public record LoginResponseDto(string Token, string Email, IReadOnlyList<string> Roles, DateTime ExpiresAtUtc);

public record RegisterRequestDto(string Email, string Password, string Role);
