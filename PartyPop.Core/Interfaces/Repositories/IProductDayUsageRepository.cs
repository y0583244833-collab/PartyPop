using PartyPop.Core.Entities;

namespace PartyPop.Core.Interfaces.Repositories;

public interface IProductDayUsageRepository
{
    /// <summary>עם tracking: השורות יתעדכנו באותה טרנזקציה.</summary>
    Task<IReadOnlyList<ProductDayUsage>> GetForUpdateAsync(
        IEnumerable<int> productIds, DateOnly date, CancellationToken ct);

    Task AddAsync(ProductDayUsage usage, CancellationToken ct);
}
