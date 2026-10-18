using PartyPop.Core.Entities;
using PartyPop.Core.Enums;

namespace PartyPop.Core.Interfaces.Services;

/// <summary>חישובי זמינות ומחיר טהורים, בלי מסד. קל לבדוק בבדיקות יחידה.</summary>
public interface IAvailabilityCalculator
{
    int GetAvailable(Product product, int morningReserved, int afternoonReserved, ShiftType shift);
    decimal GetUnitPrice(decimal pricePerShift, ShiftType shift);
    DateTime GetShiftStartUtc(DateOnly date, ShiftType shift);
}
