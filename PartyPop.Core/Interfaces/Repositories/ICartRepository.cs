using PartyPop.Core.Entities;

namespace PartyPop.Core.Interfaces.Repositories;

public interface ICartRepository
{
    /// <summary>עם tracking, כולל Items ו-Product של כל פריט.</summary>
    Task<Cart?> GetByUserIdAsync(int userId, CancellationToken ct);

    Task AddAsync(Cart cart, CancellationToken ct);
    void RemoveItem(CartItem item);
}
