using System.ComponentModel.DataAnnotations;

namespace PartyPop.Core.DTOs.Products;

public class ProductCreateRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Range(typeof(decimal), "0.01", "99999999")]
    public decimal PricePerShift { get; set; }

    [Range(1, 1000)]
    public int TotalQuantity { get; set; }

    [Range(0, 120)]
    public int? MinAge { get; set; }
}
