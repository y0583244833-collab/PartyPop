using Microsoft.Extensions.Logging;
using PartyPop.Core.DTOs.Carts;
using PartyPop.Core.Entities;
using PartyPop.Core.Enums;
using PartyPop.Core.Exceptions;
using PartyPop.Core.Interfaces.Repositories;
using PartyPop.Core.Interfaces.Services;

namespace PartyPop.Service.Orders;

/// <summary>
/// הסל של הלקוח: תאריך ומשמרת אחת, ופריטים.
/// הסל לא שומר מקום! הזמינות מוצגת כאזהרה בלבד, והבדיקה האמיתית נעשית באישור ההזמנה.
/// </summary>
public class CartService : ICartService
{
    private const int MaxQuantityPerItem = 100;

    private readonly ICartRepository _carts;
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _uow;
    private readonly IAvailabilityCalculator _calculator;
    private readonly TimeProvider _time;
    private readonly ILogger<CartService> _logger;

    public CartService(ICartRepository carts, IProductRepository products, IUnitOfWork uow,
        IAvailabilityCalculator calculator, TimeProvider time, ILogger<CartService> logger)
    {
        _carts = carts;
        _products = products;
        _uow = uow;
        _calculator = calculator;
        _time = time;
        _logger = logger;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    public async Task<CartResponse> GetMyCartAsync(int userId, CancellationToken ct)
    {
        var cart = await _carts.GetByUserIdAsync(userId, ct);
        if (cart is null)
            return new CartResponse();   // עוד לא נוצר סל: מחזירים סל ריק, בלי ליצור שורה

        var items = await BuildItemsAsync(cart, cart.Items, ct);
        return new CartResponse
        {
            EventDate = cart.EventDate,
            Shift = cart.Shift?.ToString(),
            Items = items,
            EstimatedTotal = items.Sum(i => i.UnitPrice * i.Quantity)
        };
    }

    public async Task SetDateAndShiftAsync(int userId, SetCartRequest request, CancellationToken ct)
    {
        var date = request.EventDate!.Value;
        var shift = request.Shift!.Value;

        if (_calculator.GetShiftStartUtc(date, shift) <= Now)
            throw new BusinessRuleException("התאריך והמשמרת שנבחרו כבר עברו");

        var cart = await GetOrCreateAsync(userId, ct);
        cart.EventDate = date;
        cart.Shift = shift;
        cart.UpdatedAt = Now;

        await SaveAsync(userId, ct);
    }

    public async Task<CartItemResponse> AddItemAsync(int userId, AddCartItemRequest request, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(request.ProductId, ct)
                      ?? throw new NotFoundException("המוצר לא נמצא");
        if (product.Status == ProductStatus.Inactive)
            throw new BusinessRuleException("המוצר אינו זמין להזמנה");

        var cart = await GetOrCreateAsync(userId, ct);

        // מוצר שכבר בסל: מגדילים כמות ולא יוצרים שורה שנייה
        var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (item is null)
        {
            item = new CartItem { ProductId = product.Id, Product = product, Quantity = request.Quantity, AddedAt = Now };
            cart.Items.Add(item);
        }
        else
        {
            if (item.Quantity + request.Quantity > MaxQuantityPerItem)
                throw new BusinessRuleException($"אפשר להזמין עד {MaxQuantityPerItem} יחידות ממוצר");
            item.Quantity += request.Quantity;
        }

        // UpdatedAt על הסל בכל שינוי בפריט, כדי שה-token של הסל ישתנה (סעיף 6.1)
        cart.UpdatedAt = Now;
        await SaveAsync(userId, ct);

        return (await BuildItemsAsync(cart, [item], ct))[0];
    }

    public async Task UpdateItemAsync(int userId, int itemId, UpdateCartItemRequest request, CancellationToken ct)
    {
        var (cart, item) = await GetMyItemAsync(userId, itemId, ct);
        item.Quantity = request.Quantity;
        cart.UpdatedAt = Now;
        await SaveAsync(userId, ct);
    }

    public async Task RemoveItemAsync(int userId, int itemId, CancellationToken ct)
    {
        var (cart, item) = await GetMyItemAsync(userId, itemId, ct);
        _carts.RemoveItem(item);
        cart.UpdatedAt = Now;
        await SaveAsync(userId, ct);
    }

    // ================= עזר =================

    private async Task<Cart> GetOrCreateAsync(int userId, CancellationToken ct)
    {
        var cart = await _carts.GetByUserIdAsync(userId, ct);
        if (cart is not null)
            return cart;

        cart = new Cart { UserId = userId, UpdatedAt = Now };
        await _carts.AddAsync(cart, ct);
        return cart;
    }

    /// <summary>פריט ששייך לסל שלי. פריט של מישהו אחר נראה כמו "לא קיים" (404).</summary>
    private async Task<(Cart Cart, CartItem Item)> GetMyItemAsync(int userId, int itemId, CancellationToken ct)
    {
        var cart = await _carts.GetByUserIdAsync(userId, ct);
        var item = cart?.Items.FirstOrDefault(i => i.Id == itemId);
        if (cart is null || item is null)
            throw new NotFoundException("הפריט לא נמצא בסל");
        return (cart, item);
    }

    private async Task SaveAsync(int userId, CancellationToken ct)
    {
        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            // למשל: הלקוח שינה את הסל בדיוק כשאישר הזמנה בלשונית אחרת
            _logger.LogWarning("Cart concurrency conflict for user {UserId}", userId);
            throw new ConflictException("הסל השתנה בינתיים. נא לרענן ולנסות שוב.");
        }
    }

    /// <summary>
    /// פרטי הפריטים + מחיר + אזהרת זמינות.
    /// המונים של כל המוצרים נטענים בשאילתה אחת (בלי N+1).
    /// </summary>
    private async Task<List<CartItemResponse>> BuildItemsAsync(Cart cart, IEnumerable<CartItem> items, CancellationToken ct)
    {
        var list = items.ToList();

        Dictionary<int, (int Morning, int Afternoon)> usage = [];
        if (cart.EventDate is { } date && list.Count > 0)
        {
            var rows = await _products.GetManyWithUsageAsync(list.Select(i => i.ProductId), date, ct);
            usage = rows.ToDictionary(r => r.Product.Id, r => (r.MorningReserved, r.AfternoonReserved));
        }

        return list.Select(i =>
        {
            var response = new CartItemResponse
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                ImageUrl = i.Product.ImageUrl,
                Quantity = i.Quantity,
                // בלי משמרת מציגים מחיר משמרת רגילה
                UnitPrice = _calculator.GetUnitPrice(i.Product.PricePerShift, cart.Shift ?? ShiftType.Morning)
            };

            if (i.Product.Status == ProductStatus.Inactive)
            {
                response.Available = 0;
                response.Warning = "המוצר כבר לא זמין להזמנה. נא להסיר אותו מהסל";
            }
            else if (cart.EventDate is not null && cart.Shift is { } shift)
            {
                var (morning, afternoon) = usage.GetValueOrDefault(i.ProductId);
                var available = _calculator.GetAvailable(i.Product, morning, afternoon, shift);
                response.Available = available;
                if (i.Quantity > available)
                    response.Warning = $"כרגע זמינות רק {available} יחידות בתאריך ובמשמרת שנבחרו";
            }
            return response;
        }).ToList();
    }
}
