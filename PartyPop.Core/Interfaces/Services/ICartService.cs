using PartyPop.Core.DTOs.Carts;

namespace PartyPop.Core.Interfaces.Services;

public interface ICartService
{
    Task<CartResponse> GetMyCartAsync(int userId, CancellationToken ct);
    Task SetDateAndShiftAsync(int userId, SetCartRequest request, CancellationToken ct);
    Task<CartItemResponse> AddItemAsync(int userId, AddCartItemRequest request, CancellationToken ct);
    Task UpdateItemAsync(int userId, int itemId, UpdateCartItemRequest request, CancellationToken ct);
    Task RemoveItemAsync(int userId, int itemId, CancellationToken ct);
}
