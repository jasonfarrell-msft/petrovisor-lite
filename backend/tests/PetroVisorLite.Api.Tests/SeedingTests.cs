using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PetroVisorLite.Infrastructure;
using PetroVisorLite.Infrastructure.Identity;
using PetroVisorLite.Infrastructure.Seed;

namespace PetroVisorLite.Api.Tests;

/// <summary>
/// Covers the seeding mechanism: the Seeding:Enabled config flag (and SEED_DEMO_DATA env var
/// override) gating whether Program.cs seeds on startup, plus SeedData.SeedAsync's own
/// idempotency (safe to call repeatedly without duplicating rows).
/// </summary>
public class SeedingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SeedingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> BuildFactory(string dbName, bool? seedingEnabled = null, string? seedDemoDataEnvOverride = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            // Prevent AddInfrastructureServices() from wiring up SqlServer via the real
            // dev connection string; we swap in InMemory instead (same pattern as
            // AuthAndRoleEnforcementTests).
            builder.UseSetting("ConnectionStrings:PetroVisorDb", string.Empty);

            if (seedingEnabled.HasValue)
            {
                builder.UseSetting("Seeding:Enabled", seedingEnabled.Value.ToString());
            }

            if (seedDemoDataEnvOverride is not null)
            {
                builder.UseSetting("SEED_DEMO_DATA", seedDemoDataEnvOverride);
            }

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PetroVisorDbContext>));
                if (descriptor is not null) services.Remove(descriptor);

                services.AddDbContext<PetroVisorDbContext>(options => options.UseInMemoryDatabase(dbName));
            });
        });
    }

    [Fact]
    public async Task Startup_SeedsData_WhenFlagEnabledAndDatabaseEmpty()
    {
        var dbName = $"SeedTests-Enabled-{Guid.NewGuid()}";
        using var factory = BuildFactory(dbName, seedingEnabled: true);

        // Triggers host startup (and therefore Program.cs's seeding block) on first use.
        using var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PetroVisorDbContext>();

        Assert.True(await dbContext.Wells.AnyAsync());
        Assert.True(await dbContext.Facilities.AnyAsync());
        Assert.True(await dbContext.ProductionRecords.AnyAsync());
    }

    [Fact]
    public async Task Startup_SkipsSeeding_WhenFlagDisabled()
    {
        var dbName = $"SeedTests-Disabled-{Guid.NewGuid()}";
        using var factory = BuildFactory(dbName, seedingEnabled: false);

        using var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PetroVisorDbContext>();

        Assert.False(await dbContext.Wells.AnyAsync());
        Assert.False(await dbContext.Facilities.AnyAsync());
    }

    [Fact]
    public async Task Startup_SeedsData_WhenSeedDemoDataEnvVarOverridesFlagToTrue()
    {
        var dbName = $"SeedTests-EnvOverrideTrue-{Guid.NewGuid()}";
        // Config flag says disabled, but SEED_DEMO_DATA env var override should win.
        using var factory = BuildFactory(dbName, seedingEnabled: false, seedDemoDataEnvOverride: "true");

        using var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PetroVisorDbContext>();

        Assert.True(await dbContext.Wells.AnyAsync());
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent_WhenCalledTwice()
    {
        var dbName = $"SeedTests-Idempotent-{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<PetroVisorDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        using var factory = BuildFactory(dbName);
        using var scope = factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await using var dbContext = new PetroVisorDbContext(options);

        await SeedData.SeedAsync(dbContext, roleManager, userManager, NullLogger.Instance);
        var wellCountAfterFirstSeed = await dbContext.Wells.CountAsync();
        var recordCountAfterFirstSeed = await dbContext.ProductionRecords.CountAsync();

        Assert.True(wellCountAfterFirstSeed > 0);

        // Calling again must not duplicate rows — SeedData.SeedAsync checks for existing Wells first.
        await SeedData.SeedAsync(dbContext, roleManager, userManager, NullLogger.Instance);

        Assert.Equal(wellCountAfterFirstSeed, await dbContext.Wells.CountAsync());
        Assert.Equal(recordCountAfterFirstSeed, await dbContext.ProductionRecords.CountAsync());
    }
}
