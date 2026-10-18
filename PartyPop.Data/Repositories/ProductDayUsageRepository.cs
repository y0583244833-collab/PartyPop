using Microsoft.EntityFrameworkCore;
using PartyPop.Core.Entities;
using PartyPop.Core.Interfaces.Repositories;

namespace PartyPop.Data.Repositories;

public class ProductDayUsageRepository : IProductDayUsageRepository
{
    private readonly PartyPopDbContext _db;

    public ProductDayUsageRepository(PartyPopDbContext db) => _db = db;

    // עם tracking: השורות מתעדכנות ונשמרות עם ה-token שלהן
    public async Task<IReadOnlyList<ProductDayUsage>> GetForUpdateAsync(
        IEnumerable<int> productIds, DateOnly date, CancellationToken ct)
    {
        var ids = productIds.Distinct().ToList();
        return await _db.ProductDayUsages
            .Where(u => u.EventDate == date && ids.Contains(u.ProductId))
            .ToListAsync(ct);
    }

    public async Task AddAsync(ProductDayUsage usage, CancellationToken ct) =>
        await _db.ProductDayUsages.AddAsync(usage, ct);
}
