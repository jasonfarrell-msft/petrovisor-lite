using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetroVisorLite.Application.Interfaces;
using PetroVisorLite.Infrastructure.Auth;
using PetroVisorLite.Infrastructure.Identity;
using PetroVisorLite.Infrastructure.Repositories;
using PetroVisorLite.Infrastructure.Services;

namespace PetroVisorLite.Infrastructure.DependencyInjection;

/// <summary>Registers Infrastructure-layer services: DbContext, Identity, JWT auth, and repositories.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Local dev: SQL Server LocalDB, e.g.
        //   "Server=(localdb)\\mssqllocaldb;Database=PetroVisorLite;Trusted_Connection=True;TrustServerCertificate=True"
        // or a Dockerized SQL Server (see docker-compose.yml at the repo root), e.g.
        //   "Server=localhost,1433;Database=PetroVisorLite;User Id=sa;Password=***;TrustServerCertificate=True"
        // Set the real value via 'dotnet user-secrets set "ConnectionStrings:PetroVisorDb" "..."' locally.
        // In Azure, source it from Key Vault via Managed Identity — never commit a real connection string.
        var connectionString = ResolveConnectionString(configuration);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<PetroVisorDbContext>(options => options.UseSqlServer(connectionString));
        }

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<PetroVisorDbContext>()
            .AddDefaultTokenProviders();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .PostConfigure(options =>
            {
                options.Key = JwtOptions.ResolveKey(options.Key);
            });
        services.AddOptions<AzureAiFoundryOptions>()
            .Bind(configuration.GetSection(AzureAiFoundryOptions.SectionName));
        services.AddSingleton<TokenCredential>(
            _ => new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddHttpClient<IQueryIntentClassifier, AzureAiFoundryQueryIntentClassifier>();

        services.AddScoped<IWellRepository, WellRepository>();
        services.AddScoped<IFacilityRepository, FacilityRepository>();
        services.AddScoped<IProductionRecordRepository, ProductionRecordRepository>();
        services.AddScoped<ICsvImportService, CsvImportService>();

        return services;
    }

    /// <summary>
    /// Resolves the SQL connection string. Prefers an explicit ConnectionStrings:PetroVisorDb value
    /// (local dev via user-secrets, or a full override). If that isn't set, builds one from the
    /// SQL_SERVER_FQDN / SQL_DATABASE_NAME env vars using Azure AD Managed Identity auth — this is
    /// how the Container App is wired, since SQL is AAD-only auth in Azure (no SQL login/password).
    /// </summary>
    private static string? ResolveConnectionString(IConfiguration configuration)
    {
        var explicitConnectionString = configuration.GetConnectionString("PetroVisorDb");
        if (!string.IsNullOrWhiteSpace(explicitConnectionString))
        {
            return explicitConnectionString;
        }

        var sqlServerFqdn = configuration["SQL_SERVER_FQDN"];
        var sqlDatabaseName = configuration["SQL_DATABASE_NAME"];
        if (string.IsNullOrWhiteSpace(sqlServerFqdn) || string.IsNullOrWhiteSpace(sqlDatabaseName))
        {
            return null;
        }

        var clientId = configuration["AZURE_CLIENT_ID"];
        var authClause = string.IsNullOrWhiteSpace(clientId)
            ? "Authentication=Active Directory Managed Identity;"
            : $"Authentication=Active Directory Managed Identity;User Id={clientId};";

        return $"Server=tcp:{sqlServerFqdn},1433;Database={sqlDatabaseName};{authClause}TrustServerCertificate=False;Encrypt=True;";
    }
}
