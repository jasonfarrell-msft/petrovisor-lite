using Microsoft.EntityFrameworkCore;
using PetroVisorLite.Application.Interfaces;
using PetroVisorLite.Domain;

namespace PetroVisorLite.Infrastructure.Repositories;

public class WellRepository : IWellRepository
{
    private readonly PetroVisorDbContext _db;

    public WellRepository(PetroVisorDbContext db) => _db = db;

    public async Task<Well?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Wells.Include(w => w.Facility).FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Well>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Wells.Include(w => w.Facility).AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(Well well, CancellationToken cancellationToken = default)
    {
        _db.Wells.Add(well);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Well well, CancellationToken cancellationToken = default)
    {
        _db.Wells.Update(well);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var well = await _db.Wells.FindAsync(new object?[] { id }, cancellationToken);
        if (well is not null)
        {
            _db.Wells.Remove(well);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
