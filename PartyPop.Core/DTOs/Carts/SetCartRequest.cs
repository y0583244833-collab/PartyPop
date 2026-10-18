using System.ComponentModel.DataAnnotations;
using PartyPop.Core.Enums;

namespace PartyPop.Core.DTOs.Carts;

/// <summary>קביעת התאריך והמשמרת של הסל.</summary>
public class SetCartRequest
{
    [Required]
    public DateOnly? EventDate { get; set; }

    [Required]
    public ShiftType? Shift { get; set; }
}
