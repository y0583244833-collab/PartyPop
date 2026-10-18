using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PartyPop.Core.DTOs.Carts;
using PartyPop.Core.Enums;
using PartyPop.Core.Exceptions;
using PartyPop.Core.Interfaces.Repositories;
using PartyPop.Core.Models;
using PartyPop.Service.Orders;
using PartyPop.Tests.Helpers;

namespace PartyPop.Tests.Services;

/// <summary>אי אפשר להכניס לסל יותר יחידות ממה שזמין.</summary>
public class CartStockTests
{
    private const int CustomerId = 7;

    private readonly Mock<ICartRepository> _carts = new();
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 12, 1, 10, 0, 0, TimeSpan.Zero));

    private CartService CreateService() => new(_carts.Object, _products.Object, _uow.Object,
        TestFactory.Calculator(), _time, Mock.Of<ILogger<CartService>>());

    /// <summary>מוצר עם 4 יחידות, מתוכן 3 כבר תפוסות בבוקר של תאריך האירוע.</summary>
    private void Given(Core.Entities.Cart cart, Core.Entities.Product product, int morningReserved)
    {
        _carts.Setup(c => c.GetByUserIdAsync(CustomerId, It.IsAny<CancellationToken>())).ReturnsAsync(cart);
        _products.Setup(p => p.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _products.Setup(p => p.GetManyWithUsageAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync([new ProductWithUsage(product, morningReserved, 0)]);
    }

    [Fact]
    public async Task AddItem_WithinStock_IsSaved()
    {
        var product = TestFactory.Product(total: 4);
        Given(TestFactory.Cart(CustomerId, ShiftType.Morning, TestFactory.EventDate), product, morningReserved: 3);

        var item = await CreateService().AddItemAsync(CustomerId, new AddCartItemRequest { ProductId = 1, Quantity = 1 }, default);

        Assert.Equal(1, item.Quantity);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddItem_MoreThanAvailable_IsRejected()
    {
        var product = TestFactory.Product(total: 4);
        Given(TestFactory.Cart(CustomerId, ShiftType.Morning, TestFactory.EventDate), product, morningReserved: 3);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService().AddItemAsync(CustomerId, new AddCartItemRequest { ProductId = 1, Quantity = 2 }, default));

        Assert.Contains("1", ex.Message);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddItem_NoDateYet_LimitedByWorkingUnits()
    {
        // 4 יחידות, אחת תקולה: בלי תאריך אפשר עד 3
        var product = TestFactory.Product(total: 4, outOfService: 1);
        Given(TestFactory.Cart(CustomerId, null, null), product, morningReserved: 0);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService().AddItemAsync(CustomerId, new AddCartItemRequest { ProductId = 1, Quantity = 66 }, default));
    }
}
