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

    // שלב 11
    public Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(OrderQueryParams query, CancellationToken ct) =>
        throw new NotImplementedException("ייכתב בשלב 11 (ניהול הזמנות)");

    public async Task AddAsync(Order order, CancellationToken ct) =>
        await _db.Orders.AddAsync(order, ct);
}
