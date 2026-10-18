using System.ComponentModel.DataAnnotations;
using PartyPop.Core.Enums;

namespace PartyPop.Core.DTOs.Orders;

public class UpdateOrderStatusRequest
{
    /// <summary>לקוח: רק Cancelled. מנהל: Cancelled או Completed.</summary>
    [Required]
    public OrderStatus? Status { get; set; }
}
