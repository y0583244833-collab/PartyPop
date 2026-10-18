using PartyPop.Core.Entities;

namespace PartyPop.Core.Interfaces.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct);
    Task<bool> HasProductsAsync(int id, CancellationToken ct);
    Task AddAsync(Category category, CancellationToken ct);
    void Remove(Category category);
}
