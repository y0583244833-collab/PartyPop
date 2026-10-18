namespace PartyPop.Core.DTOs.Orders;

public class OrderResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateOnly EventDate { get; set; }
    public string Shift { get; set; } = string.Empty;
    public string EventAddress { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CancelledAt { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<OrderItemResponse> Items { get; set; } = [];
}

public class OrderItemResponse
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
