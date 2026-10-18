using Moq;
using PartyPop.Core.DTOs.Orders;
using PartyPop.Core.Entities;
using PartyPop.Core.Enums;
using PartyPop.Core.Exceptions;
using PartyPop.Tests.Helpers;

namespace PartyPop.Tests.Services;

/// <summary>ביטול: 48 שעות, מנהל, סטטוסים ובעלות.</summary>
public class CancelOrderTests : OrderServiceTestBase
{
    private const int OrderId = 5;
    private static readonly UpdateOrderStatusRequest Cancel = new() { Status = OrderStatus.Cancelled };

    // משמרת בוקר 20/12 מתחילה ב-08:00 בישראל = 06:00 UTC
    private static readonly DateTimeOffset ShiftStartUtc = new(2026, 12, 20, 6, 0, 0, TimeSpan.Zero);

    private Order GivenOrder(ShiftType shift = ShiftType.Morning, OrderStatus status = OrderStatus.Confirmed,
        int quantity = 2)
    {
        var order = new Order
        {
            Id = OrderId, UserId = CustomerId, EventDate = TestFactory.EventDate, Shift = shift, Status = status,
            Items = [new OrderItem { ProductId = 1, Quantity = quantity }]
        };
        Orders.Setup(o => o.GetByIdAsync(OrderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        return order;
    }

    private Task CancelAs(int userId, UserRole role) =>
        CreateService().UpdateStatusAsync(OrderId, userId, role, Cancel, CancellationToken.None);

    [Fact]
    public async Task MoreThan48Hours_Succeeds_AndReleasesUnits()
    {
        Time.SetUtcNow(ShiftStartUtc.AddHours(-49));
        var order = GivenOrder();
        var usage = GivenUsage(1, morning: 2, afternoon: 0);

        await CancelAs(CustomerId, UserRole.Customer);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.NotNull(order.CancelledAt);
        Assert.Equal(0, usage.MorningReserved);
    }

    [Fact]
    public async Task Exactly48Hours_Succeeds()
    {
        Time.SetUtcNow(ShiftStartUtc.AddHours(-48));   // הגבול
        var order = GivenOrder();
        GivenUsage(1, 2, 0);

        await CancelAs(CustomerId, UserRole.Customer);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public async Task LessThan48Hours_IsRejected()
    {
        Time.SetUtcNow(ShiftStartUtc.AddHours(-48).AddMinutes(1));
        var order = GivenOrder();

        await Assert.ThrowsAsync<BusinessRuleException>(() => CancelAs(CustomerId, UserRole.Customer));
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public async Task Admin_CanCancelLessThan48Hours()
    {
        Time.SetUtcNow(ShiftStartUtc.AddHours(-1));
        var order = GivenOrder();
        GivenUsage(1, 2, 0);

        await CancelAs(userId: 1, UserRole.Admin);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Completed)]
    public async Task AlreadyCancelledOrCompleted_IsRejected(OrderStatus status)
    {
        GivenOrder(status: status);
        await Assert.ThrowsAsync<BusinessRuleException>(() => CancelAs(CustomerId, UserRole.Customer));
    }

    [Fact]
    public async Task CancelFullDay_ReleasesBothCounters()
    {
        GivenOrder(shift: ShiftType.FullDay, quantity: 2);
        var usage = GivenUsage(1, morning: 3, afternoon: 2);

        await CancelAs(CustomerId, UserRole.Customer);

        Assert.Equal(1, usage.MorningReserved);
        Assert.Equal(0, usage.AfternoonReserved);
    }

    [Fact]
    public async Task OtherCustomer_CannotCancel()
    {
        GivenOrder();
        await Assert.ThrowsAsync<ForbiddenException>(() => CancelAs(userId: 99, UserRole.Customer));
    }

    [Fact]
    public async Task OtherCustomer_CannotViewOrder()
    {
        Orders.Setup(o => o.GetDetailsAsync(OrderId, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Order { Id = OrderId, UserId = CustomerId, User = new User() });

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateService().GetByIdAsync(OrderId, 99, UserRole.Customer, CancellationToken.None));
    }

    [Fact]
    public async Task ConcurrentCancel_ThrowsConflict_AndLogsWarning()
    {
        GivenOrder();
        GivenUsage(1, 2, 0);
        Uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .ThrowsAsync(new ConcurrencyConflictException());

        await Assert.ThrowsAsync<ConflictException>(() => CancelAs(CustomerId, UserRole.Customer));
        VerifyWarningLogged(Times.Once());
    }
}
