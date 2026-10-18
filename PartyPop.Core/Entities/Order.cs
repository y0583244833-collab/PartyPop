using PartyPop.Core.Enums;

namespace PartyPop.Core.Entities;

public class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateOnly EventDate { get; set; }
    public ShiftType Shift { get; set; }
    public string EventAddress { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;

    /// <summary>מחושב בשרת.</summary>
    public decimal TotalPrice { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Confirmed;
    public DateTime? CancelledAt { get; set; }
    public string? Notes { get; set; }

    /// <summary>Concurrency token. ממופה ל-xmin ב-Data (Fluent API).</summary>
    public uint Version { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
