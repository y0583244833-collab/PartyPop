using System.ComponentModel.DataAnnotations;

namespace PartyPop.Core.DTOs.Carts;

public class UpdateCartItemRequest
{
    [Range(1, 100)]
    public int Quantity { get; set; }
}
