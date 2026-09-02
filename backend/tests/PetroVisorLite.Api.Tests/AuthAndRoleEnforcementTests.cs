using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetroVisorLite.Application.Dtos;
using PetroVisorLite.Infrastructure;
using PetroVisorLite.Infrastructure.Auth;
using PetroVisorLite.Infrastructure.Identity;

namespace PetroVisorLite.Api.Tests;

/// <summary>
/// End-to-end API tests: spins up the real API host with an in-memory EF Core provider
/// (swapped in for SQL Server). Login remains available, but demo APIs do not require JWTs.
/// </summary>
public class AuthAndRoleEnforcementTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    private readonly string _dbName = $"AuthTests-{Guid.NewGuid()}";

    public AuthAndRoleEnforcementTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // Prevent the app's own AddInfrastructureServices() from calling UseSqlServer
            // (it would otherwise pick up the real dev connection string from User Secrets,
            // registering conflicting SqlServer + InMemory provider services).
            builder.UseSetting("ConnectionStrings:PetroVisorDb", string.Empty);

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PetroVisorDbContext>));
                if (descriptor is not null) services.Remove(descriptor);

                services.AddDbContext<PetroVisorDbContext>(options =>
                    options.UseInMemoryDatabase(_dbName));
            });
        });
    }

    private async Task SeedUserAsync(string email, string password, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, role);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwtWithRoleClaim()
    {
        await SeedUserAsync("engineer1@test.local", "Test1234!", Roles.Engineer);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto("engineer1@test.local", "Test1234!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.Token));
        Assert.Contains(Roles.Engineer, body.Roles);
    }

    [Fact]
    public void JwtOptions_ResolveKey_GeneratesStableFallbackWhenUnset()
    {
        var key1 = JwtOptions.ResolveKey(null);
        var key2 = JwtOptions.ResolveKey(string.Empty);

        Assert.False(string.IsNullOrWhiteSpace(key1));
        Assert.Equal(key1, key2);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        await SeedUserAsync("viewer1@test.local", "Test1234!", Roles.Viewer);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto("viewer1@test.local", "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WellsEndpoint_WithoutToken_AllowsDemoRead()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/wells");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateWell_WithoutToken_AllowsDemoWrite()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/wells", new WellDto(Guid.Empty, "New Well", "42-000-00000", 0, 0));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateWell_AsEngineer_Succeeds()
    {
        await SeedUserAsync("engineer2@test.local", "Test1234!", Roles.Engineer);
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto("engineer2@test.local", "Test1234!"));
        var token = (await login.Content.ReadFromJsonAsync<LoginResponseDto>())!.Token;
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/wells", new WellDto(Guid.Empty, "New Well", "42-000-00001", 0, 0));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task WellsEndpoint_AsViewer_AllowsRead()
    {
        await SeedUserAsync("viewer3@test.local", "Test1234!", Roles.Viewer);
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto("viewer3@test.local", "Test1234!"));
        var token = (await login.Content.ReadFromJsonAsync<LoginResponseDto>())!.Token;
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/wells");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
