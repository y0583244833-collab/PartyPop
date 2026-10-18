using PartyPop.Core.DTOs.Products;
using PartyPop.Core.Entities;
using PartyPop.Core.Models;

namespace PartyPop.Core.Interfaces.Repositories;

public interface IProductRepository
{
    /// <summary>עם tracking, לעדכון.</summary>
    Task<Product?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>AsNoTracking, כולל קטגוריה. לתצוגה.</summary>
    Task<Product?> GetDetailsAsync(int id, CancellationToken ct);

    /// <summary>
    /// קטלוג: סינון, מיון ודפדוף בתוך השאילתה.
    /// fullDayFactor משמש לסינון budget/מחיר כשהמשמרת FullDay.
    /// </summary>
    Task<(IReadOnlyList<ProductWithUsage> Items, int TotalCount)> GetCatalogAsync(
        ProductQueryParams query, decimal fullDayFactor, CancellationToken ct);

    /// <summary>מוצר אחד + המונים לתאריך (שאילתה אחת).</summary>
    Task<ProductWithUsage?> GetWithUsageAsync(int id, DateOnly date, CancellationToken ct);

    /// <summary>כמה מוצרים + המונים לתאריך (שאילתה אחת). לאזהרות בסל.</summary>
    Task<IReadOnlyList<ProductWithUsage>> GetManyWithUsageAsync(
        IEnumerable<int> ids, DateOnly date, CancellationToken ct);

    /// <summary>עם tracking: לאישור הזמנה בתוך הטרנזקציה.</summary>
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct);

    Task AddAsync(Product product, CancellationToken ct);
}
