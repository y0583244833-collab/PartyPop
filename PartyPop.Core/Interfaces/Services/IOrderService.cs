using PartyPop.Core.DTOs.Common;
using PartyPop.Core.DTOs.Orders;
using PartyPop.Core.Enums;

namespace PartyPop.Core.Interfaces.Services;

public interface IOrderService
{
    Task<OrderResponse> CheckoutAsync(int userId, CheckoutRequest request, CancellationToken ct);
    Task<IReadOnlyList<OrderResponse>> GetMyOrdersAsync(int userId, CancellationToken ct);

    /// <summary>בדיקת בעלות: לקוח רואה רק את שלו, מנהל רואה הכל.</summary>
    Task<OrderResponse> GetByIdAsync(int orderId, int userId, UserRole role, CancellationToken ct);

    Task<PagedResult<OrderResponse>> GetAllAsync(OrderQueryParams query, CancellationToken ct);

    /// <summary>לקוח: ביטול בלבד, עד 48 שעות לפני. מנהל: בכל זמן.</summary>
    Task UpdateStatusAsync(int orderId, int userId, UserRole role, UpdateOrderStatusRequest request, CancellationToken ct);
}
