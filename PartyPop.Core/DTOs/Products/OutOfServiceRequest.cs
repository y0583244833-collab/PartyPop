using System.ComponentModel.DataAnnotations;

namespace PartyPop.Core.DTOs.Products;

public class OutOfServiceRequest
{
    /// <summary>הבדיקה מול TotalQuantity נעשית ב-Service.</summary>
    [Range(0, int.MaxValue)]
    public int OutOfServiceQuantity { get; set; }
}
