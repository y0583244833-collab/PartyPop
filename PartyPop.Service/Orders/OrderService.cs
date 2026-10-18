using AutoMapper;
using Microsoft.Extensions.Logging;
using PartyPop.Core.DTOs.Common;
using PartyPop.Core.DTOs.Orders;
using PartyPop.Core.Entities;
using PartyPop.Core.Enums;
using PartyPop.Core.Exceptions;
using PartyPop.Core.Interfaces.Repositories;
using PartyPop.Core.Interfaces.Services;

namespace PartyPop.Service.Orders;

public class OrderService : IOrderService
{
    private static readonly TimeSpan CancellationWindow = TimeSpan.FromHours(48);

    private readonly IOrderRepository _orders;
    private readonly ICartRepository _carts;
    private readonly IProductDayUsageRepository _usages;
    private readonly IUnitOfWork _uow;
    private readonly IAvailabilityCalculator _calculator;
    private readonly IMapper _mapper;
    private readonly TimeProvider _time;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IOrderRepository orders, ICartRepository carts, IProductDayUsageRepository usages,
        IUnitOfWork uow, IAvailabilityCalculator calculator, IMapper mapper, TimeProvider time,
        ILogger<OrderService> logger)
    {
        _orders = orders;
        _carts = carts;
        _usages = usages;
        _uow = uow;
        _calculator = calculator;
        _mapper = mapper;
        _time = time;
        _logger = logger;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    // ================= אישור הזמנה (Checkout) =================

    /// <summary>
    /// הכל בטרנזקציה אחת (סעיף 6.1): בדיקת זמינות על השורות שנטענו בתוכה,
    /// עדכון המונים, יצירת ההזמנה וריקון הסל. הכל או כלום.
    /// </summary>
    public async Task<OrderResponse> CheckoutAsync(int userId, CheckoutRequest request, CancellationToken ct)
    {
        int orderId;
        DateOnly? date = null;
        ShiftType? shift = null;
        List<int> productIds = [];

        try
        {
            // ה-catch עוטף את הקריאה ל-ExecuteInTransactionAsync, ולא קוד שבתוך ה-lambda
            orderId = await _uow.ExecuteInTransactionAsync(async ct =>
            {
                // 1. טוענים את הסל (עם tracking, כולל המוצרים)
                var cart = await _carts.GetByUserIdAsync(userId, ct);
                if (cart is null || cart.Items.Count == 0)
                    throw new BusinessRuleException("הסל ריק");
                if (cart.EventDate is null || cart.Shift is null)
                    throw new BusinessRuleException("יש לבחור תאריך ומשמרת לסל לפני אישור ההזמנה");

                date = cart.EventDate.Value;
                shift = cart.Shift.Value;
                if (_calculator.GetShiftStartUtc(date.Value, shift.Value) <= Now)
                    throw new BusinessRuleException("התאריך והמשמרת שבסל כבר עברו. נא לבחור תאריך חדש");

                var items = cart.Items.ToList();
                productIds = items.Select(i => i.ProductId).ToList();

                // 2. שורות המונים של התאריך, עם tracking: הן יתעדכנו ויישמרו עם ה-token
                var usageByProduct = (await _usages.GetForUpdateAsync(productIds, date.Value, ct))
                    .ToDictionary(u => u.ProductId);

                // 3. בדיקת זמינות על אותם אובייקטים שיישמרו
                var unavailable = new List<UnavailableItem>();
                foreach (var item in items)
                {
                    usageByProduct.TryGetValue(item.ProductId, out var usage);
                    var available = _calculator.GetAvailable(item.Product,
                        usage?.MorningReserved ?? 0, usage?.AfternoonReserved ?? 0, shift.Value);
                    if (item.Quantity > available)
                        unavailable.Add(new UnavailableItem(item.Id, item.Product.Name, item.Quantity, available));
                }
                if (unavailable.Count > 0)
                    // שום דבר לא נשמר: Rollback, והסל נשאר כמו שהוא
                    throw new ConflictException("חלק מהפריטים אינם זמינים", unavailable);

                // 4. עדכון המונים (יום שלם מעלה את שניהם). אין שורה לתאריך → יוצרים
                foreach (var item in items)
                {
                    if (!usageByProduct.TryGetValue(item.ProductId, out var usage))
                    {
                        usage = new ProductDayUsage { ProductId = item.ProductId, EventDate = date.Value };
                        await _usages.AddAsync(usage, ct);
                        usageByProduct[item.ProductId] = usage;
                    }
                    if (shift is ShiftType.Morning or ShiftType.FullDay)
                        usage.MorningReserved += item.Quantity;
                    if (shift is ShiftType.AfternoonEvening or ShiftType.FullDay)
                        usage.AfternoonReserved += item.Quantity;
                }

                // 5. ההזמנה. המחיר מחושב בשרת ונשמר ב-UnitPrice
                var order = new Order
                {
                    UserId = userId,
                    CreatedAt = Now,
                    EventDate = date.Value,
                    Shift = shift.Value,
                    EventAddress = request.EventAddress.Trim(),
                    ContactPhone = request.ContactPhone.Trim(),
                    Notes = request.Notes?.Trim(),
                    Status = OrderStatus.Confirmed,
                    Items = items.Select(i => new OrderItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = _calculator.GetUnitPrice(i.Product.PricePerShift, shift.Value)
                    }).ToList()
                };
                order.TotalPrice = order.Items.Sum(i => i.UnitPrice * i.Quantity);
                await _orders.AddAsync(order, ct);

                // 6. ריקון הסל. עדכון שורת Cart מכניס את ה-token של הסל ל-WHERE
                foreach (var item in items)
                    _carts.RemoveItem(item);
                cart.EventDate = null;
                cart.Shift = null;
                cart.UpdatedAt = Now;

                // 7. שמירה אחת
                await _uow.SaveChangesAsync(ct);
                return order.Id;
            }, ct);
        }
        catch (ConcurrencyConflictException)
        {
            // הבדיקה עברה, אבל מישהו שמר לפנינו. לא זרימה רגילה → Warning
            _logger.LogWarning(
                "Checkout concurrency conflict: user {UserId}, date {EventDate}, shift {Shift}, products {ProductIds}",
                userId, date, shift, productIds);
            throw new ConflictException("חלק מהמוצרים נתפסו הרגע על ידי לקוח אחר. נא לנסות שוב.");
        }

        _logger.LogInformation("Order {OrderId} created by user {UserId}", orderId, userId);
        return await GetResponseAsync(orderId, ct);
    }

    // ================= קריאה =================

    public async Task<IReadOnlyList<OrderResponse>> GetMyOrdersAsync(int userId, CancellationToken ct) =>
        _mapper.Map<List<OrderResponse>>(await _orders.GetByUserIdAsync(userId, ct));

    public async Task<OrderResponse> GetByIdAsync(int orderId, int userId, UserRole role, CancellationToken ct)
    {
        var order = await _orders.GetDetailsAsync(orderId, ct)
                    ?? throw new NotFoundException("ההזמנה לא נמצאה");

        // בעלות: לקוח רואה רק את ההזמנות שלו
        if (role != UserRole.Admin && order.UserId != userId)
            throw new ForbiddenException("אין לך הרשאה לצפות בהזמנה הזו");

        return _mapper.Map<OrderResponse>(order);
    }

    /// <summary>היסטוריה למנהל.</summary>
    public async Task<PagedResult<OrderResponse>> GetAllAsync(OrderQueryParams query, CancellationToken ct)
    {
        if (query.FromDate is not null && query.ToDate is not null && query.FromDate > query.ToDate)
            throw new BusinessRuleException("תאריך ההתחלה מאוחר מתאריך הסיום");

        var (items, totalCount) = await _orders.GetPagedAsync(query, ct);
        return new PagedResult<OrderResponse>
        {
            Items = _mapper.Map<List<OrderResponse>>(items),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    // ================= ביטול / עדכון סטטוס =================

    public async Task UpdateStatusAsync(int orderId, int userId, UserRole role,
        UpdateOrderStatusRequest request, CancellationToken ct)
    {
        var newStatus = request.Status!.Value;
        var isAdmin = role == UserRole.Admin;

        try
        {
            await _uow.ExecuteInTransactionAsync(async ct =>
            {
                var order = await _orders.GetByIdAsync(orderId, ct)
                            ?? throw new NotFoundException("ההזמנה לא נמצאה");

                if (!isAdmin && order.UserId != userId)
                    throw new ForbiddenException("אין לך הרשאה לשנות את ההזמנה הזו");
                if (!isAdmin && newStatus != OrderStatus.Cancelled)
                    throw new ForbiddenException("לקוח יכול רק לבטל הזמנה");
                if (newStatus == OrderStatus.Confirmed)
                    throw new BusinessRuleException("אי אפשר להחזיר הזמנה לסטטוס Confirmed");
                if (order.Status != OrderStatus.Confirmed)
                    throw new BusinessRuleException($"ההזמנה כבר במצב {order.Status}, ואי אפשר לשנות אותה");

                if (newStatus == OrderStatus.Cancelled)
                {
                    // 48 שעות לפני תחילת המשמרת, לפי שעון ישראל. בדיוק 48 שעות = מותר. מנהל: בכל זמן
                    var shiftStart = _calculator.GetShiftStartUtc(order.EventDate, order.Shift);
                    if (!isAdmin && shiftStart - Now < CancellationWindow)
                        throw new BusinessRuleException("אפשר לבטל רק עד 48 שעות לפני תחילת המשמרת");

                    await ReleaseUnitsAsync(order, ct);
                    order.Status = OrderStatus.Cancelled;
                    order.CancelledAt = Now;
                }
                else
                {
                    order.Status = OrderStatus.Completed;   // מנהל בלבד
                }

                // ה-token של Order (xmin): לקוח מבטל ומנהל מסמן "הושלם" באותו רגע → רק אחד מצליח
                await _uow.SaveChangesAsync(ct);
                return true;
            }, ct);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Order {OrderId} status update concurrency conflict (user {UserId})", orderId, userId);
            throw new ConflictException("ההזמנה עודכנה בינתיים על ידי פעולה אחרת. נא לרענן ולנסות שוב.");
        }

        _logger.LogInformation("Order {OrderId} status changed to {Status} by user {UserId}", orderId, newStatus, userId);
    }

    /// <summary>ביטול מחזיר את היחידות למלאי (ביום שלם: משני המונים).</summary>
    private async Task ReleaseUnitsAsync(Order order, CancellationToken ct)
    {
        var usages = (await _usages.GetForUpdateAsync(order.Items.Select(i => i.ProductId), order.EventDate, ct))
            .ToDictionary(u => u.ProductId);

        foreach (var item in order.Items)
        {
            if (!usages.TryGetValue(item.ProductId, out var usage))
                continue;   // לא אמור לקרות: כל הזמנה יוצרת שורת מונים
            if (order.Shift is ShiftType.Morning or ShiftType.FullDay)
                usage.MorningReserved = Math.Max(0, usage.MorningReserved - item.Quantity);
            if (order.Shift is ShiftType.AfternoonEvening or ShiftType.FullDay)
                usage.AfternoonReserved = Math.Max(0, usage.AfternoonReserved - item.Quantity);
        }
    }

    private async Task<OrderResponse> GetResponseAsync(int orderId, CancellationToken ct) =>
        _mapper.Map<OrderResponse>(await _orders.GetDetailsAsync(orderId, ct));
}
