using Microsoft.EntityFrameworkCore;
using PetroVisorLite.Application.Interfaces;
using PetroVisorLite.Domain;

namespace PetroVisorLite.Infrastructure.Repositories;

public class ProductionRecordRepository : IProductionRecordRepository
{
    private readonly PetroVisorDbContext _db;

    public ProductionRecordRepository(PetroVisorDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductionRecord>> GetByWellIdAsync(Guid wellId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        var query = _db.ProductionRecords.AsNoTracking().Where(pr => pr.WellId == wellId);
        if (from.HasValue) query = query.Where(pr => pr.Date >= from.Value);
        if (to.HasValue) query = query.Where(pr => pr.Date <= to.Value);
        return await query.OrderBy(pr => pr.Date).ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<ProductionRecord> records, CancellationToken cancellationToken = default)
    {
        _db.ProductionRecords.AddRange(records);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
