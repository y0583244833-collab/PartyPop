using PartyPop.Core.Enums;

namespace PartyPop.Core.DTOs.Products;

public class AvailabilityResponse
{
    public int ProductId { get; set; }
    public DateOnly Date { get; set; }
    public ShiftType Shift { get; set; }
    public int Available { get; set; }
    public decimal Price { get; set; }
}
