using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PartyPop.Core.Entities;
using PartyPop.Core.Interfaces.Repositories;
using PartyPop.Service.Orders;
using PartyPop.Tests.Helpers;

namespace PartyPop.Tests.Services;

/// <summary>כל ה-Mocks של OrderService במקום אחד.</summary>
public abstract class OrderServiceTestBase
{
    protected const int CustomerId = 7;

    protected readonly Mock<IOrderRepository> Orders = new();
    protected readonly Mock<ICartRepository> Carts = new();
    protected readonly Mock<IProductDayUsageRepository> Usages = new();
    protected readonly Mock<IUnitOfWork> Uow = new();
    protected readonly Mock<ILogger<OrderService>> Logger = new();

    // "עכשיו" קבוע: הבדיקה לא תלויה בשעה שבה מריצים אותה
    protected readonly FakeTimeProvider Time = new(new DateTimeOffset(2026, 12, 1, 10, 0, 0, TimeSpan.Zero));

    protected readonly List<ProductDayUsage> UsageRows = [];
    protected Order? SavedOrder;

    protected OrderServiceTestBase()
    {
        // ה-Mock של הטרנזקציה מריץ את ה-delegate בפועל, כדי שהלוגיקה שבתוכו תיבדק
        Uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<int>>>(), It.IsAny<CancellationToken>()))
           .Returns<Func<CancellationToken, Task<int>>, CancellationToken>((work, ct) => work(ct));
        Uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()))
           .Returns<Func<CancellationToken, Task<bool>>, CancellationToken>((work, ct) => work(ct));

        Usages.Setup(u => u.GetForUpdateAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((IEnumerable<int> ids, DateOnly date, CancellationToken _) =>
                  UsageRows.Where(r => r.EventDate == date && ids.Contains(r.ProductId)).ToList());

        Orders.Setup(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
              .Callback<Order, CancellationToken>((order, _) =>
              {
                  order.Id = 100;
                  order.User = new User { FirstName = "Demo", LastName = "Customer" };
                  SavedOrder = order;
              });
        Orders.Setup(o => o.GetDetailsAsync(100, It.IsAny<CancellationToken>()))
              .ReturnsAsync(() => SavedOrder);
    }

    protected OrderService CreateService() => new(
        Orders.Object, Carts.Object, Usages.Object, Uow.Object,
        TestFactory.Calculator(), TestFactory.Mapper(), Time, Logger.Object);

    protected void GivenCart(Cart cart) =>
        Carts.Setup(c => c.GetByUserIdAsync(cart.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(cart);

    protected ProductDayUsage GivenUsage(int productId, int morning, int afternoon)
    {
        var row = new ProductDayUsage
        {
            ProductId = productId, EventDate = TestFactory.EventDate,
            MorningReserved = morning, AfternoonReserved = afternoon
        };
        UsageRows.Add(row);
        return row;
    }

    /// <summary>LogWarning הוא Extension Method, ולכן מאמתים את Log עצמו.</summary>
    protected void VerifyWarningLogged(Times times) =>
        Logger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), times);
}
