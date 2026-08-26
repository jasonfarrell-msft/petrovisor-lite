using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PetroVisorLite.Infrastructure.Auth;
using System.Text;

namespace PetroVisorLite.Api.DependencyInjection;

/// <summary>Configures JWT bearer authentication using the "Jwt" configuration section (see <see cref="JwtOptions"/>).</summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Key may be empty at startup time before configuration is bound from
                // User Secrets/Key Vault; a random fallback key means validation simply
                // fails closed (no tokens will validate) rather than throwing at startup.
                var keyBytes = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(jwtOptions.Key) ? Guid.NewGuid().ToString() : jwtOptions.Key);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        services.AddAuthorization();

        return services;
    }
}
