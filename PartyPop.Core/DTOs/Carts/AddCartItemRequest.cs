using System.ComponentModel.DataAnnotations;

namespace PartyPop.Core.DTOs.Carts;

public class AddCartItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; } = 1;
}
