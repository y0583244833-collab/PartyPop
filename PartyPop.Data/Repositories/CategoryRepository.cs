using Microsoft.EntityFrameworkCore;
using PartyPop.Core.Entities;
using PartyPop.Core.Interfaces.Repositories;

namespace PartyPop.Data.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly PartyPopDbContext _db;

    public CategoryRepository(PartyPopDbContext db) => _db = db;

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct) =>
        await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    // עם tracking: משמש לעדכון ולמחיקה
    public Task<Category?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct) =>
        _db.Categories.AnyAsync(c => c.Name == name && (excludeId == null || c.Id != excludeId), ct);

    public Task<bool> HasProductsAsync(int id, CancellationToken ct) =>
        _db.Products.AnyAsync(p => p.CategoryId == id, ct);

    public async Task AddAsync(Category category, CancellationToken ct) =>
        await _db.Categories.AddAsync(category, ct);

    public void Remove(Category category) => _db.Categories.Remove(category);
}
