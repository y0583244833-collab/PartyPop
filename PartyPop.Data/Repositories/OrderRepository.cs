using Microsoft.EntityFrameworkCore;
using PartyPop.Core.DTOs.Orders;
using PartyPop.Core.Entities;
using PartyPop.Core.Interfaces.Repositories;

namespace PartyPop.Data.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly PartyPopDbContext _db;

    public OrderRepository(PartyPopDbContext db) => _db = db;

    public Task<Order?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetDetailsAsync(int id, CancellationToken ct) =>
        _db.Orders.AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Order>> GetByUserIdAsync(int userId, CancellationToken ct) =>
        await _db.Orders.AsNoTracking()
            .Where(o => o.UserId == userId)
            .Include(o => o.User)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .OrderByDescending(o => o.EventDate).ThenByDescending(o => o.Id)
            .AsSplitQuery()   // בלי "פיצוץ" שורות של JOIN על כמה אוספים
            .ToListAsync(ct);

    /// <summary>היסטוריה למנהל: סינון, מיון ודפדוף בתוך השאילתה.</summary>
    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(OrderQueryParams query, CancellationToken ct)
    {
        var orders = _db.Orders.AsNoTracking();

        if (query.FromDate is not null)
            orders = orders.Where(o => o.EventDate >= query.FromDate);
        if (query.ToDate is not null)
            orders = orders.Where(o => o.EventDate <= query.ToDate);
        if (query.UserId is not null)
            orders = orders.Where(o => o.UserId == query.UserId);
        if (!string.IsNullOrWhiteSpace(query.Email))
            orders = orders.Where(o => o.User.Email == query.Email.Trim());
        if (query.Status is not null)
            orders = orders.Where(o => o.Status == query.Status);
        if (query.Shift is not null)
            orders = orders.Where(o => o.Shift == query.Shift);
        // הזמנות שיש בהן לפחות פריט אחד מהמוצר / מהקטגוריה
        if (query.ProductId is not null)
            orders = orders.Where(o => o.Items.Any(i => i.ProductId == query.ProductId));
        if (query.CategoryId is not null)
            orders = orders.Where(o => o.Items.Any(i => i.Product.CategoryId == query.CategoryId));

        var totalCount = await orders.CountAsync(ct);

        var desc = query.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
        var sorted = query.SortBy.ToLowerInvariant() switch
        {
            "createdat" => desc ? orders.OrderByDescending(o => o.CreatedAt) : orders.OrderBy(o => o.CreatedAt),
            "totalprice" => desc ? orders.OrderByDescending(o => o.TotalPrice) : orders.OrderBy(o => o.TotalPrice),
            _ => desc ? orders.OrderByDescending(o => o.EventDate) : orders.OrderBy(o => o.EventDate)
        };

        var items = await sorted.ThenBy(o => o.Id)   // סדר יציב בין עמודים
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(o => o.User)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .AsSplitQuery()
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(Order order, CancellationToken ct) =>
        await _db.Orders.AddAsync(order, ct);
}
