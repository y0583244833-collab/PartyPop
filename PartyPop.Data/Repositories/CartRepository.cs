using Microsoft.EntityFrameworkCore;
using PartyPop.Core.Entities;
using PartyPop.Core.Interfaces.Repositories;

namespace PartyPop.Data.Repositories;

public class CartRepository : ICartRepository
{
    private readonly PartyPopDbContext _db;

    public CartRepository(PartyPopDbContext db) => _db = db;

    public Task<Cart?> GetByUserIdAsync(int userId, CancellationToken ct) =>
        _db.Carts
            .Include(c => c.Items.OrderBy(i => i.AddedAt))
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

    public async Task AddAsync(Cart cart, CancellationToken ct) =>
        await _db.Carts.AddAsync(cart, ct);

    public void RemoveItem(CartItem item) => _db.CartItems.Remove(item);
}
