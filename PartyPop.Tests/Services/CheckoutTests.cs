using Moq;
using PartyPop.Core.DTOs.Orders;
using PartyPop.Core.Entities;
using PartyPop.Core.Enums;
using PartyPop.Core.Exceptions;
using PartyPop.Tests.Helpers;

namespace PartyPop.Tests.Services;

/// <summary>המשאב המוגבל: אישור הזמנה (סעיף 16 בתוכנית).</summary>
public class CheckoutTests : OrderServiceTestBase
{
    private static readonly CheckoutRequest Request = new()
    {
        EventAddress = "רחוב הרב קוק 5, בני ברק",
        ContactPhone = "0521234567"
    };

    [Fact]
    public async Task EnoughUnits_CreatesConfirmedOrder_AndEmptiesCart()
    {
        var product = TestFactory.Product(total: 3, price: 100);
        var cart = TestFactory.Cart(CustomerId, ShiftType.Morning, TestFactory.EventDate, (product, 2));
        GivenCart(cart);
        var usage = GivenUsage(product.Id, morning: 1, afternoon: 0);

        var result = await CreateService().CheckoutAsync(CustomerId, Request, CancellationToken.None);

        Assert.Equal("Confirmed", result.Status);
        Assert.Equal(200m, result.TotalPrice);
        Assert.Equal(3, usage.MorningReserved);               // 1 + 2
        Assert.Equal(0, usage.AfternoonReserved);
        Assert.Null(cart.EventDate);                          // הסל התרוקן
        Assert.Null(cart.Shift);
        Carts.Verify(c => c.RemoveItem(It.IsAny<CartItem>()), Times.Once);
        Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotEnoughUnits_ThrowsConflictWithList_AndSavesNothing()
    {
        var product = TestFactory.Product(total: 3);
        GivenCart(TestFactory.Cart(CustomerId, ShiftType.Morning, TestFactory.EventDate, (product, 2)));
        GivenUsage(product.Id, morning: 2, afternoon: 0);   // נשארה יחידה אחת

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService().CheckoutAsync(CustomerId, Request, CancellationToken.None));

        var item = Assert.Single(ex.UnavailableItems!);
        Assert.Equal(2, item.Requested);
        Assert.Equal(1, item.Available);
        Orders.Verify(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FullDay_WhenMorningIsTaken_IsRejected()
    {
        var product = TestFactory.Product(total: 2);
        GivenCart(TestFactory.Cart(CustomerId, ShiftType.FullDay, TestFactory.EventDate, (product, 1)));
        GivenUsage(product.Id, morning: 2, afternoon: 0);   // הבוקר מלא, הצהריים פנוי

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService().CheckoutAsync(CustomerId, Request, CancellationToken.None));
    }

    [Fact]
    public async Task FullDay_RaisesBothCounters_AndUsesFullDayPrice()
    {
        var product = TestFactory.Product(total: 2, price: 100);
        GivenCart(TestFactory.Cart(CustomerId, ShiftType.FullDay, TestFactory.EventDate, (product, 1)));
        // אין שורת מונים לתאריך: ה-Service יוצר אותה
        Usages.Setup(u => u.AddAsync(It.IsAny<ProductDayUsage>(), It.IsAny<CancellationToken>()))
              .Callback<ProductDayUsage, CancellationToken>((row, _) => UsageRows.Add(row));

        var result = await CreateService().CheckoutAsync(CustomerId, Request, CancellationToken.None);

        var row = Assert.Single(UsageRows);
        Assert.Equal(1, row.MorningReserved);
        Assert.Equal(1, row.AfternoonReserved);
        Assert.Equal(190m, result.TotalPrice);
    }

    [Fact]
    public async Task OutOfServiceUnits_AreNotAvailable()
    {
        var product = TestFactory.Product(total: 3, outOfService: 2);
        GivenCart(TestFactory.Cart(CustomerId, ShiftType.Morning, TestFactory.EventDate, (product, 2)));

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService().CheckoutAsync(CustomerId, Request, CancellationToken.None));
        Assert.Equal(1, ex.UnavailableItems![0].Available);
    }

    [Fact]
    public async Task ProductDeactivatedAfterAddedToCart_IsUnavailableWithZero()
    {
        var product = TestFactory.Product(total: 3, status: ProductStatus.Inactive);
        GivenCart(TestFactory.Cart(CustomerId, ShiftType.Morning, TestFactory.EventDate, (product, 1)));

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService().CheckoutAsync(CustomerId, Request, CancellationToken.None));
        Assert.Equal(0, ex.UnavailableItems![0].Available);
        Orders.Verify(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EmptyCart_ThrowsBusinessRule()
    {
        GivenCart(TestFactory.Cart(CustomerId, ShiftType.Morning, TestFactory.EventDate));
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService().CheckoutAsync(CustomerId, Request, CancellationToken.None));
    }

    [Fact]
    public async Task CartWithoutDate_ThrowsBusinessRule()
    {
        GivenCart(TestFactory.Cart(CustomerId, null, null, (TestFactory.Product(), 1)));
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService().CheckoutAsync(CustomerId, Request, CancellationToken.None));
    }

    [Fact]
    public async Task PastDate_ThrowsBusinessRule()
    {
        GivenCart(TestFactory.Cart(CustomerId, ShiftType.Morning, new DateOnly(2026, 11, 1), (TestFactory.Product(), 1)));
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService().CheckoutAsync(CustomerId, Request, CancellationToken.None));
    }

    [Fact]
    public async Task ConcurrencyConflict_ThrowsConflictTryAgain_AndLogsWarning()
    {
        var product = TestFactory.Product(total: 3);
        GivenCart(TestFactory.Cart(CustomerId, ShiftType.Morning, TestFactory.EventDate, (product, 1)));
        GivenUsage(product.Id, 0, 0);
        // מישהו שמר לפנינו: ה-UnitOfWork זורק את החריגה של Core
        Uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .ThrowsAsync(new ConcurrencyConflictException());

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService().CheckoutAsync(CustomerId, Request, CancellationToken.None));

        Assert.Contains("נא לנסות שוב", ex.Message);
        Assert.Null(ex.UnavailableItems);
        VerifyWarningLogged(Times.Once());
    }
}
