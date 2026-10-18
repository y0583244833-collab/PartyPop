using System.ComponentModel.DataAnnotations;
using PartyPop.Core.DTOs.Common;
using PartyPop.Core.Enums;

namespace PartyPop.Core.DTOs.Orders;

public class OrderQueryParams : PagingParams
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int? UserId { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    public OrderStatus? Status { get; set; }
    public ShiftType? Shift { get; set; }
    public int? ProductId { get; set; }
    public int? CategoryId { get; set; }

    /// <summary>eventDate / createdAt / totalPrice</summary>
    [RegularExpression("^(?i)(eventDate|createdAt|totalPrice)$")]
    public string SortBy { get; set; } = "eventDate";
}
