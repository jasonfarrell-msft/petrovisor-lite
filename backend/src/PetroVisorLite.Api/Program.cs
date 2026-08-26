using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using PetroVisorLite.Api.DependencyInjection;
using PetroVisorLite.Application.DependencyInjection;
using PetroVisorLite.Infrastructure;
using PetroVisorLite.Infrastructure.DependencyInjection;
using PetroVisorLite.Infrastructure.Identity;
using PetroVisorLite.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
var allowedCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? builder.Configuration["CORS_ALLOWED_ORIGINS"]?
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("PetroVisorFrontend", policy =>
    {
        policy.WithOrigins(allowedCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "PetroVisor Lite API", Version = "v1" });

    // Enables the Swagger UI "Authorize" button for bearer JWTs.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a JWT access token obtained from POST /api/auth/login.",
    });
    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer"), new List<string>() },
    });
});

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

// Apply pending migrations and (optionally) seed demo data on startup.
// Seeding is gated by the Seeding:Enabled config flag (bindable from appsettings or the
// SEED_DEMO_DATA / Seeding__Enabled environment variable) rather than ASPNETCORE_ENVIRONMENT,
// so it can be explicitly opted into for a demo/staging deployment without renaming the
// environment. It defaults to false (see appsettings.json) so Production never reseeds by
// accident; appsettings.Development.json defaults it to true for local dev convenience.
// SeedData.SeedAsync is itself idempotent (checks for existing rows), so it is safe to run on
// every startup when the flag is on.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetService<PetroVisorDbContext>();

    if (dbContext is not null)
    {
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync();
        }

        var seedDemoDataEnv = builder.Configuration["SEED_DEMO_DATA"];
        var seedingEnabled = !string.IsNullOrWhiteSpace(seedDemoDataEnv)
            ? bool.TryParse(seedDemoDataEnv, out var seedDemoDataFlag) && seedDemoDataFlag
            : builder.Configuration.GetValue<bool>("Seeding:Enabled");

        if (seedingEnabled)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            await SeedData.SeedAsync(dbContext, roleManager, userManager, logger);
        }
    }
    else
    {
        // No connection string could be resolved from ConnectionStrings:PetroVisorDb or the
        // SQL_SERVER_FQDN/SQL_DATABASE_NAME env vars, so AddInfrastructureServices never registered
        // the DbContext. Log and continue rather than crash-looping the whole app.
        app.Logger.LogWarning(
            "PetroVisorDbContext is not registered (no SQL connection string could be resolved) — " +
            "skipping database migration/seeding. Set ConnectionStrings:PetroVisorDb via " +
            "'dotnet user-secrets set \"ConnectionStrings:PetroVisorDb\" \"...\"' locally, or ensure " +
            "SQL_SERVER_FQDN/SQL_DATABASE_NAME are set in Azure.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("PetroVisorFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>Marker partial class to support WebApplicationFactory-based integration testing later.</summary>
public partial class Program { }
