using Microsoft.EntityFrameworkCore;
using PetroVisorLite.Application.Interfaces;
using PetroVisorLite.Domain;

namespace PetroVisorLite.Infrastructure.Repositories;

public class FacilityRepository : IFacilityRepository
{
    private readonly PetroVisorDbContext _db;

    public FacilityRepository(PetroVisorDbContext db) => _db = db;

    public async Task<Facility?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Facilities.Include(f => f.Wells).FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Facility>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Facilities.Include(f => f.Wells).AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(Facility facility, CancellationToken cancellationToken = default)
    {
        _db.Facilities.Add(facility);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Facility facility, CancellationToken cancellationToken = default)
    {
        _db.Facilities.Update(facility);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var facility = await _db.Facilities.FindAsync(new object?[] { id }, cancellationToken);
        if (facility is not null)
        {
            _db.Facilities.Remove(facility);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
