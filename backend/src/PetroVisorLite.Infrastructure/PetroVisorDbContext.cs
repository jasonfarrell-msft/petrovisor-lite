using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PetroVisorLite.Domain;
using PetroVisorLite.Infrastructure.Identity;

namespace PetroVisorLite.Infrastructure;

/// <summary>
/// Main EF Core DbContext. Combines domain entities (Well, Facility,
/// ProductionRecord) with ASP.NET Core Identity (ApplicationUser + roles)
/// in a single context/schema for simplicity in this "Lite" scope.
/// </summary>
public class PetroVisorDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public PetroVisorDbContext(DbContextOptions<PetroVisorDbContext> options) : base(options)
    {
    }

    public DbSet<Well> Wells => Set<Well>();
    public DbSet<ProductionRecord> ProductionRecords => Set<ProductionRecord>();
    public DbSet<Facility> Facilities => Set<Facility>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Facility>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Name).IsRequired().HasMaxLength(200);
            entity.Property(f => f.Type).IsRequired().HasMaxLength(100);

            entity.HasMany(f => f.Wells)
                .WithOne(w => w.Facility)
                .HasForeignKey(w => w.FacilityId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Well>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.Property(w => w.Name).IsRequired().HasMaxLength(200);
            entity.Property(w => w.ApiNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(w => w.ApiNumber).IsUnique();

            entity.HasMany(w => w.ProductionRecords)
                .WithOne(pr => pr.Well)
                .HasForeignKey(pr => pr.WellId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductionRecord>(entity =>
        {
            entity.HasKey(pr => pr.Id);
            entity.Property(pr => pr.Date).IsRequired();

            // Common query pattern: production history for a well ordered/filtered by date.
            entity.HasIndex(pr => new { pr.WellId, pr.Date }).IsUnique();
        });
    }
}
