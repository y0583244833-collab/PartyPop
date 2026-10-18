using PartyPop.Core.DTOs.Orders;
using PartyPop.Core.Entities;

namespace PartyPop.Core.Interfaces.Repositories;

public interface IOrderRepository
{
    /// <summary>עם tracking, כולל Items. לביטול / עדכון סטטוס.</summary>
    Task<Order?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>AsNoTracking, כולל Items, Product ו-User. לתצוגה.</summary>
    Task<Order?> GetDetailsAsync(int id, CancellationToken ct);

    Task<IReadOnlyList<Order>> GetByUserIdAsync(int userId, CancellationToken ct);

    /// <summary>היסטוריה למנהל: סינון, מיון ודפדוף בתוך השאילתה.</summary>
    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(OrderQueryParams query, CancellationToken ct);

    Task AddAsync(Order order, CancellationToken ct);
}
