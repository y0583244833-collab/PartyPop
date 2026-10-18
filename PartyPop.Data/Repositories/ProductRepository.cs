using Microsoft.EntityFrameworkCore;
using PartyPop.Core.DTOs.Products;
using PartyPop.Core.Entities;
using PartyPop.Core.Enums;
using PartyPop.Core.Interfaces.Repositories;
using PartyPop.Core.Models;

namespace PartyPop.Data.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly PartyPopDbContext _db;

    public ProductRepository(PartyPopDbContext db) => _db = db;

    public Task<Product?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Product?> GetDetailsAsync(int id, CancellationToken ct) =>
        _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<(IReadOnlyList<ProductWithUsage> Items, int TotalCount)> GetCatalogAsync(
        ProductQueryParams query, decimal fullDayFactor, CancellationToken ct)
    {
        // הקטלוג מציג רק מוצרים פעילים
        var products = _db.Products.AsNoTracking().Where(p => p.Status == ProductStatus.Active);

        if (query.CategoryId is not null)
            products = products.Where(p => p.CategoryId == query.CategoryId);

        if (query.MinAge is not null)
            // "מתאים לגיל X": מוצר בלי גיל מינימלי מתאים לכולם
            products = products.Where(p => p.MinAge == null || p.MinAge <= query.MinAge);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(query.Search.Trim())}%";
            products = products.Where(p =>
                EF.Functions.ILike(p.Name, pattern, "\\") ||
                EF.Functions.ILike(p.Description, pattern, "\\"));
        }

        // מחיר לפי המשמרת שנבחרה: ביום שלם המחיר הוא PricePerShift × factor
        var factor = query.Shift == ShiftType.FullDay ? fullDayFactor : 1m;

        if (query.MinPrice is not null)
            products = products.Where(p => p.PricePerShift * factor >= query.MinPrice);
        if (query.MaxPrice is not null)
            products = products.Where(p => p.PricePerShift * factor <= query.MaxPrice);
        if (query.Budget is not null)
            products = products.Where(p => p.PricePerShift * factor <= query.Budget);

        // שלב 8: כאן ייכנס סינון "זמין > 0" לפי date + shift, לפני ה-Count וה-Skip/Take

        var totalCount = await products.CountAsync(ct);

        var desc = query.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
        var sorted = query.SortBy.Equals("price", StringComparison.OrdinalIgnoreCase)
            ? (desc ? products.OrderByDescending(p => p.PricePerShift) : products.OrderBy(p => p.PricePerShift))
            : (desc ? products.OrderByDescending(p => p.Name) : products.OrderBy(p => p.Name));

        // ThenBy(Id): סדר יציב, כדי שמוצר לא "יקפוץ" בין עמודים
        var items = await sorted.ThenBy(p => p.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(p => p.Category)
            .ToListAsync(ct);

        // שלב 8: המונים יגיעו מ-ProductDayUsage באותה שאילתה. כרגע 0
        return (items.Select(p => new ProductWithUsage(p, 0, 0)).ToList(), totalCount);
    }

    // שלב 8
    public Task<ProductWithUsage?> GetWithUsageAsync(int id, DateOnly date, CancellationToken ct) =>
        throw new NotImplementedException("ייכתב בשלב 8 (זמינות)");

    // שלב 9
    public Task<IReadOnlyList<ProductWithUsage>> GetManyWithUsageAsync(
        IEnumerable<int> ids, DateOnly date, CancellationToken ct) =>
        throw new NotImplementedException("ייכתב בשלב 9 (סל)");

    // שלב 10
    public Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct) =>
        throw new NotImplementedException("ייכתב בשלב 10 (הזמנות)");

    public async Task AddAsync(Product product, CancellationToken ct) =>
        await _db.Products.AddAsync(product, ct);

    /// <summary>כדי ש-% או _ בחיפוש ייחשבו כתו רגיל ולא כ-wildcard.</summary>
    private static string EscapeLike(string s) =>
        s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
