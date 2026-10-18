using System.ComponentModel.DataAnnotations;

namespace PartyPop.Core.DTOs.Orders;

/// <summary>התאריך, המשמרת והפריטים נלקחים מהסל.</summary>
public class CheckoutRequest
{
    [Required, StringLength(200)]
    public string EventAddress { get; set; } = string.Empty;

    [Required, Phone, StringLength(20)]
    public string ContactPhone { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; set; }
}
