using Microsoft.Extensions.Options;
using PartyPop.Core.Entities;
using PartyPop.Core.Enums;
using PartyPop.Core.Interfaces.Services;
using PartyPop.Core.Settings;

namespace PartyPop.Service.Catalog;

/// <summary>חישובים טהורים, בלי מסד. בשלב 6 משמש למחיר יום שלם, ובשלב 8 לזמינות.</summary>
public class AvailabilityCalculator : IAvailabilityCalculator
{
    private readonly PricingSettings _pricing;
    private readonly ShiftSettings _shifts;
    private static readonly TimeZoneInfo IsraelTz = FindIsraelTimeZone();

    public AvailabilityCalculator(IOptions<PricingSettings> pricing, IOptions<ShiftSettings> shifts)
    {
        _pricing = pricing.Value;
        _shifts = shifts.Value;
    }

    /// <summary>יום שלם = 2 משמרות פחות ההנחה. למשל 5% → 1.9</summary>
    public decimal FullDayFactor => 2m * (1m - _pricing.FullDayDiscountPercent / 100m);

    public int GetAvailable(Product product, int morningReserved, int afternoonReserved, ShiftType shift)
    {
        if (product.Status == ProductStatus.Inactive)
            return 0;

        var capacity = product.TotalQuantity - product.OutOfServiceQuantity;
        var used = shift switch
        {
            ShiftType.Morning => morningReserved,
            ShiftType.AfternoonEvening => afternoonReserved,
            // יום שלם תופס את שתי המשמרות, ולכן לפי העמוסה מביניהן
            _ => Math.Max(morningReserved, afternoonReserved)
        };
        return Math.Max(0, capacity - used);
    }

    public decimal GetUnitPrice(decimal pricePerShift, ShiftType shift) =>
        shift == ShiftType.FullDay
            ? Math.Round(pricePerShift * FullDayFactor, 2)
            : pricePerShift;

    public DateTime GetShiftStartUtc(DateOnly date, ShiftType shift)
    {
        var start = shift == ShiftType.AfternoonEvening ? _shifts.AfternoonEveningStart : _shifts.MorningStart;
        var local = date.ToDateTime(start, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, IsraelTz);
    }

    // Linux משתמש בשם IANA, ו-Windows ישן בשם של Windows
    private static TimeZoneInfo FindIsraelTimeZone()
    {
        foreach (var id in new[] { "Asia/Jerusalem", "Israel Standard Time" })
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz))
                return tz;
        return TimeZoneInfo.Utc;
    }
}
