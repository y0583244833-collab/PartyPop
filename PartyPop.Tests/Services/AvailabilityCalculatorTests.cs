using PartyPop.Core.Enums;
using PartyPop.Tests.Helpers;

namespace PartyPop.Tests.Services;

public class AvailabilityCalculatorTests
{
    private readonly PartyPop.Service.Catalog.AvailabilityCalculator _calc = TestFactory.Calculator();

    [Fact]
    public void FullDayPrice_IsTwoShiftsMinusFivePercent()
    {
        // 100 × 2 × 0.95 = 190
        Assert.Equal(190m, _calc.GetUnitPrice(100m, ShiftType.FullDay));
        Assert.Equal(100m, _calc.GetUnitPrice(100m, ShiftType.Morning));
    }

    [Fact]
    public void FullDayPrice_IsRoundedToTwoDecimals()
    {
        // 33.33 × 1.9 = 63.327 → 63.33
        Assert.Equal(63.33m, _calc.GetUnitPrice(33.33m, ShiftType.FullDay));
    }

    [Theory]
    [InlineData(ShiftType.Morning, 3)]            // 5 − 2
    [InlineData(ShiftType.AfternoonEvening, 4)]   // 5 − 1
    [InlineData(ShiftType.FullDay, 3)]            // 5 − max(2, 1)
    public void Available_DependsOnShift(ShiftType shift, int expected)
    {
        var product = TestFactory.Product(total: 5);
        Assert.Equal(expected, _calc.GetAvailable(product, morningReserved: 2, afternoonReserved: 1, shift));
    }

    [Fact]
    public void FullDay_WhenMorningIsFull_IsNotAvailable()
    {
        var product = TestFactory.Product(total: 2);
        Assert.Equal(0, _calc.GetAvailable(product, morningReserved: 2, afternoonReserved: 0, ShiftType.FullDay));
        Assert.Equal(2, _calc.GetAvailable(product, morningReserved: 2, afternoonReserved: 0, ShiftType.AfternoonEvening));
    }

    [Fact]
    public void OutOfServiceUnits_AreNotCounted()
    {
        var product = TestFactory.Product(total: 5, outOfService: 2);
        Assert.Equal(3, _calc.GetAvailable(product, 0, 0, ShiftType.Morning));
    }

    [Fact]
    public void InactiveProduct_HasZeroAvailability()
    {
        var product = TestFactory.Product(total: 5, status: PartyPop.Core.Enums.ProductStatus.Inactive);
        Assert.Equal(0, _calc.GetAvailable(product, 0, 0, ShiftType.Morning));
    }

    [Fact]
    public void ShiftStart_UsesIsraelTime()
    {
        // דצמבר: שעון חורף, UTC+2. בוקר 08:00 בישראל = 06:00 UTC
        var start = _calc.GetShiftStartUtc(new DateOnly(2026, 12, 20), ShiftType.Morning);
        Assert.Equal(new DateTime(2026, 12, 20, 6, 0, 0, DateTimeKind.Utc), start);
    }
}
