using AutoMapper;
using Microsoft.Extensions.Options;
using PartyPop.Core.Entities;
using PartyPop.Core.Enums;
using PartyPop.Core.Settings;
using PartyPop.Service.Catalog;
using PartyPop.Service.Mapping;

namespace PartyPop.Tests.Helpers;

/// <summary>אובייקטים אמיתיים (לא Mock) שמשותפים לבדיקות: מחשבון, AutoMapper ונתונים.</summary>
public static class TestFactory
{
    public static readonly DateOnly EventDate = new(2026, 12, 20);

    public static AvailabilityCalculator Calculator() =>
        new(Options.Create(new PricingSettings { FullDayDiscountPercent = 5 }),
            Options.Create(new ShiftSettings()));

    public static IMapper Mapper() =>
        new MapperConfiguration(cfg => cfg.AddMaps(typeof(UserProfile).Assembly)).CreateMapper();

    public static Product Product(int id = 1, int total = 3, int outOfService = 0,
        decimal price = 100, ProductStatus status = ProductStatus.Active) => new()
    {
        Id = id,
        Name = $"מוצר {id}",
        PricePerShift = price,
        TotalQuantity = total,
        OutOfServiceQuantity = outOfService,
        Status = status
    };

    /// <summary>סל עם תאריך ומשמרת, ופריט אחד לכל מוצר.</summary>
    public static Cart Cart(int userId, ShiftType? shift, DateOnly? date, params (Product Product, int Quantity)[] items)
    {
        var cart = new Cart { Id = 1, UserId = userId, EventDate = date, Shift = shift };
        var itemId = 1;
        foreach (var (product, quantity) in items)
            cart.Items.Add(new CartItem
            {
                Id = itemId++, CartId = cart.Id, Cart = cart,
                ProductId = product.Id, Product = product, Quantity = quantity
            });
        return cart;
    }
}
