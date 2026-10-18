namespace PartyPop.Core.DTOs.Carts;

public class CartResponse
{
    public DateOnly? EventDate { get; set; }
    public string? Shift { get; set; }
    public IReadOnlyList<CartItemResponse> Items { get; set; } = [];

    /// <summary>מחיר משוער. המחיר הסופי מחושב באישור ההזמנה.</summary>
    public decimal EstimatedTotal { get; set; }
}

public class CartItemResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>מתמלא רק אם יש לסל תאריך ומשמרת.</summary>
    public int? Available { get; set; }

    /// <summary>אזהרה בלבד: הסל לא שומר מקום.</summary>
    public string? Warning { get; set; }
}
