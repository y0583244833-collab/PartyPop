using PartyPop.Core.Entities;
using PartyPop.Core.Enums;

namespace PartyPop.Core.Interfaces.Services;

/// <summary>חישובי זמינות ומחיר טהורים, בלי מסד. קל לבדוק בבדיקות יחידה.</summary>
public interface IAvailabilityCalculator
{
    /// <summary>מכפיל מחיר ליום שלם: 2 × (1 − הנחה). משמש לסינון ומיון לפי מחיר בשאילתה.</summary>
    decimal FullDayFactor { get; }

    int GetAvailable(Product product, int morningReserved, int afternoonReserved, ShiftType shift);
    decimal GetUnitPrice(decimal pricePerShift, ShiftType shift);
    DateTime GetShiftStartUtc(DateOnly date, ShiftType shift);
}
