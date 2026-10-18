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
        // הקטלוג מציג רק מוצרים פעילים (מנהל יכול לבקש גם מושבתים)
        var products = _db.Products.AsNoTracking();
        if (!query.IncludeInactive)
            products = products.Where(p => p.Status == ProductStatus.Active);

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

        // מוצר + המונים של התאריך (Left Join). בלי תאריך: המונים 0
        var rows = query.Date is { } date
            ? WithUsage(products, date)
            : products.Select(p => new UsageRow { Product = p, Morning = 0, Afternoon = 0 });

        // "זמין > 0" בתוך ה-Where, לפני Count ו-Skip/Take, כדי שהדפדוף וה-totalCount יהיו נכונים
        if (query.Date is not null && query.Shift is { } shift)
            rows = shift switch
            {
                ShiftType.Morning => rows.Where(r =>
                    r.Product.TotalQuantity - r.Product.OutOfServiceQuantity - r.Morning > 0),
                ShiftType.AfternoonEvening => rows.Where(r =>
                    r.Product.TotalQuantity - r.Product.OutOfServiceQuantity - r.Afternoon > 0),
                // יום שלם: לפי המשמרת העמוסה מבין השתיים
                _ => rows.Where(r =>
                    r.Product.TotalQuantity - r.Product.OutOfServiceQuantity
                    - (r.Morning > r.Afternoon ? r.Morning : r.Afternoon) > 0)
            };

        var totalCount = await rows.CountAsync(ct);

        var desc = query.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
        var sorted = query.SortBy.Equals("price", StringComparison.OrdinalIgnoreCase)
            ? (desc ? rows.OrderByDescending(r => r.Product.PricePerShift) : rows.OrderBy(r => r.Product.PricePerShift))
            : (desc ? rows.OrderByDescending(r => r.Product.Name) : rows.OrderBy(r => r.Product.Name));

        // ThenBy(Id): סדר יציב, כדי שמוצר לא "יקפוץ" בין עמודים
        var page = await sorted.ThenBy(r => r.Product.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new { r.Product, CategoryName = r.Product.Category.Name, r.Morning, r.Afternoon })
            .ToListAsync(ct);

        var items = page.Select(x =>
        {
            x.Product.Category = new Category { Id = x.Product.CategoryId, Name = x.CategoryName };
            return new ProductWithUsage(x.Product, x.Morning, x.Afternoon);
        }).ToList();

        return (items, totalCount);
    }

    public async Task<ProductWithUsage?> GetWithUsageAsync(int id, DateOnly date, CancellationToken ct)
    {
        var row = await WithUsage(_db.Products.AsNoTracking().Where(p => p.Id == id), date)
            .FirstOrDefaultAsync(ct);
        return row is null ? null : new ProductWithUsage(row.Product, row.Morning, row.Afternoon);
    }

    public async Task<IReadOnlyList<ProductWithUsage>> GetManyWithUsageAsync(
        IEnumerable<int> ids, DateOnly date, CancellationToken ct)
    {
        var idList = ids.Distinct().ToList();
        var rows = await WithUsage(_db.Products.AsNoTracking().Where(p => idList.Contains(p.Id)), date)
            .ToListAsync(ct);
        return rows.Select(r => new ProductWithUsage(r.Product, r.Morning, r.Afternoon)).ToList();
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct)
    {
        var idList = ids.Distinct().ToList();
        return await _db.Products.Where(p => idList.Contains(p.Id)).ToListAsync(ct);
    }

    public async Task AddAsync(Product product, CancellationToken ct) =>
        await _db.Products.AddAsync(product, ct);

    /// <summary>
    /// Left Join ל-ProductDayUsage של התאריך, בשאילתה אחת (בלי N+1).
    /// אין שורה לתאריך → שני המונים 0.
    /// </summary>
    private IQueryable<UsageRow> WithUsage(IQueryable<Product> products, DateOnly date) =>
        from p in products
        join u in _db.ProductDayUsages.Where(u => u.EventDate == date)
            on p.Id equals u.ProductId into usages
        from u in usages.DefaultIfEmpty()
        select new UsageRow
        {
            Product = p,
            Morning = u == null ? 0 : u.MorningReserved,
            Afternoon = u == null ? 0 : u.AfternoonReserved
        };

    /// <summary>כדי ש-% או _ בחיפוש ייחשבו כתו רגיל ולא כ-wildcard.</summary>
    private static string EscapeLike(string s) =>
        s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private sealed class UsageRow
    {
        public Product Product { get; init; } = null!;
        public int Morning { get; init; }
        public int Afternoon { get; init; }
    }
}
