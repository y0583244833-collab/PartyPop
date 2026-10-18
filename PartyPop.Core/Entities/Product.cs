using PartyPop.Core.Enums;

namespace PartyPop.Core.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    /// <summary>מחיר למשמרת של 6 שעות. מחיר יום שלם מחושב ב-Service.</summary>
    public decimal PricePerShift { get; set; }

    /// <summary>כמה יחידות יש בבעלות החברה.</summary>
    public int TotalQuantity { get; set; }

    /// <summary>כמה יחידות מתוכן תקולות או בתיקון.</summary>
    public int OutOfServiceQuantity { get; set; }

    public string? ImageUrl { get; set; }
    public int? MinAge { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<ProductDayUsage> DayUsages { get; set; } = new List<ProductDayUsage>();
}
