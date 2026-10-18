namespace PartyPop.Core.DTOs.Products;

public class ProductResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal PricePerShift { get; set; }
    public decimal FullDayPrice { get; set; }
    public int TotalQuantity { get; set; }
    public int OutOfServiceQuantity { get; set; }
    public string? ImageUrl { get; set; }
    public int? MinAge { get; set; }
    public string Status { get; set; } = string.Empty;

    /// <summary>מתמלא רק כשמסננים לפי תאריך ומשמרת.</summary>
    public int? Available { get; set; }
}
