using Microsoft.EntityFrameworkCore;
using PartyPop.Core.Entities;
using PartyPop.Core.Enums;
using PartyPop.Core.Exceptions;
using PartyPop.Data;

namespace PartyPop.IntegrationTests;

/// <summary>
/// ההוכחה שמנגנון ה-Optimistic Concurrency עובד (סעיף 16.1 בתוכנית).
/// כל בדיקה משתמשת בתאריך משלה, כדי שהבדיקות לא ישפיעו זו על זו.
/// </summary>
public class ConcurrencyTests : IClassFixture<DatabaseFixture>
{
    // מוצר 1 מגיע מה-Seed (HasData) שבתוך ה-Migration: 5 יחידות
    private const int ProductId = 1;

    private readonly DatabaseFixture _db;

    public ConcurrencyTests(DatabaseFixture db) => _db = db;

    [Fact]
    public async Task TwoContexts_SameUsageRow_FirstSaves_SecondThrows()
    {
        // Arrange: שורת מונים עם 0 תפוסים
        var date = new DateOnly(2027, 1, 1);
        await using (var seed = _db.CreateContext())
        {
            seed.ProductDayUsages.Add(new ProductDayUsage { ProductId = ProductId, EventDate = date });
            await seed.SaveChangesAsync();
        }

        // שני לקוחות קוראים את אותה שורה. כל אחד רואה 0 וחושב שיש מקום
        await using var firstCustomer = _db.CreateContext();
        await using var secondCustomer = _db.CreateContext();
        var first = await firstCustomer.ProductDayUsages.SingleAsync(u => u.ProductId == ProductId && u.EventDate == date);
        var second = await secondCustomer.ProductDayUsages.SingleAsync(u => u.ProductId == ProductId && u.EventDate == date);
        first.MorningReserved += 2;
        second.MorningReserved += 2;

        // Act + Assert: הראשון מצליח, השני נכשל על ה-token (xmin)
        await firstCustomer.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondCustomer.SaveChangesAsync());

        // אין הזמנת יתר: 2 ולא 4
        await using var check = _db.CreateContext();
        var saved = await check.ProductDayUsages.AsNoTracking().SingleAsync(u => u.ProductId == ProductId && u.EventDate == date);
        Assert.Equal(2, saved.MorningReserved);
    }

    [Fact]
    public async Task TwoContexts_CreateFirstUsageRow_SecondGetsConcurrencyConflict()
    {
        // מקרה הקצה: אין עדיין שורה, ושני לקוחות יוצרים אותה במקביל → Unique Index
        var date = new DateOnly(2027, 1, 2);
        await using var firstCustomer = _db.CreateContext();
        await using var secondCustomer = _db.CreateContext();
        firstCustomer.ProductDayUsages.Add(new ProductDayUsage { ProductId = ProductId, EventDate = date, MorningReserved = 1 });
        secondCustomer.ProductDayUsages.Add(new ProductDayUsage { ProductId = ProductId, EventDate = date, MorningReserved = 1 });

        await new UnitOfWork(firstCustomer).SaveChangesAsync(CancellationToken.None);

        // ה-UnitOfWork מתרגם את הפרת האינדקס הזה לאותה חריגה כמו התנגשות token
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            new UnitOfWork(secondCustomer).SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task DuplicateEmail_IsNotTranslatedToConcurrencyConflict()
    {
        // בדיקה שלילית: Unique אחר (אימייל) לא נחשב "התנגשות concurrency"
        var email = $"{Guid.NewGuid():N}@test.com";
        await using (var first = _db.CreateContext())
        {
            first.Users.Add(NewUser(email));
            await new UnitOfWork(first).SaveChangesAsync(CancellationToken.None);
        }

        await using var second = _db.CreateContext();
        second.Users.Add(NewUser(email));
        var ex = await Record.ExceptionAsync(() => new UnitOfWork(second).SaveChangesAsync(CancellationToken.None));

        Assert.IsNotType<ConcurrencyConflictException>(ex);
        Assert.IsType<ConflictException>(ex);   // מתורגם ל-409 "האימייל כבר רשום"
    }

    [Fact]
    public async Task StaleUsage_WholeOrderRolledBack()
    {
        var date = new DateOnly(2027, 1, 3);
        var (userId, cartId) = await SeedCustomerWithCartAsync(date, quantity: 1);
        await using (var seed = _db.CreateContext())
        {
            seed.ProductDayUsages.Add(new ProductDayUsage { ProductId = ProductId, EventDate = date });
            await seed.SaveChangesAsync();
        }

        // 1. context1 טוען את הסל ואת שורת המונים
        await using var context1 = _db.CreateContext();
        var cart = await context1.Carts.Include(c => c.Items).SingleAsync(c => c.Id == cartId);
        var usage = await context1.ProductDayUsages.SingleAsync(u => u.ProductId == ProductId && u.EventDate == date);

        // 2. לקוח אחר (context2) מעדכן את אותה שורת מונים ושומר
        await using (var context2 = _db.CreateContext())
        {
            var other = await context2.ProductDayUsages.SingleAsync(u => u.ProductId == ProductId && u.EventDate == date);
            other.MorningReserved += 1;
            await context2.SaveChangesAsync();
        }

        // 3. context1 מנסה לאשר הזמנה מלאה בטרנזקציה, עם שורת מונים "ישנה"
        var uow = new UnitOfWork(context1);
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            uow.ExecuteInTransactionAsync(async ct =>
            {
                usage.MorningReserved += 1;
                context1.Orders.Add(NewOrder(userId, date));
                context1.CartItems.RemoveRange(cart.Items);
                cart.EventDate = null;
                cart.Shift = null;
                cart.UpdatedAt = DateTime.UtcNow;
                await uow.SaveChangesAsync(ct);
                return true;
            }, CancellationToken.None));

        // 4. שום דבר לא נשמר: אין הזמנה, והסל לא השתנה
        await using var check = _db.CreateContext();
        Assert.False(await check.Orders.AnyAsync(o => o.UserId == userId));
        var savedCart = await check.Carts.AsNoTracking().Include(c => c.Items).SingleAsync(c => c.Id == cartId);
        Assert.Equal(date, savedCart.EventDate);
        Assert.Single(savedCart.Items);
    }

    [Fact]
    public async Task CartChangedDuringCheckout_SecondSaveFails()
    {
        var date = new DateOnly(2027, 1, 4);
        var (_, cartId) = await SeedCustomerWithCartAsync(date, quantity: 1);

        // context1 מתחיל לאשר: טוען את הסל
        await using var context1 = _db.CreateContext();
        var cart = await context1.Carts.Include(c => c.Items).SingleAsync(c => c.Id == cartId);

        // באותו זמן הלקוח משנה כמות בלשונית אחרת (ומעדכן את UpdatedAt של הסל)
        await using (var context2 = _db.CreateContext())
        {
            var cart2 = await context2.Carts.Include(c => c.Items).SingleAsync(c => c.Id == cartId);
            cart2.Items.First().Quantity = 3;
            cart2.UpdatedAt = DateTime.UtcNow;
            await context2.SaveChangesAsync();
        }

        // context1 מרוקן את הסל: ה-token של שורת הסל כבר לא תואם
        context1.CartItems.RemoveRange(cart.Items);
        cart.EventDate = null;
        cart.UpdatedAt = DateTime.UtcNow;
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            new UnitOfWork(context1).SaveChangesAsync(CancellationToken.None));
    }

    // ================= עזר =================

    private static User NewUser(string email) => new()
    {
        FirstName = "Test", LastName = "User", Email = email, PasswordHash = "x",
        Phone = "0500000000", Role = UserRole.Customer, CreatedAt = DateTime.UtcNow
    };

    private static Order NewOrder(int userId, DateOnly date) => new()
    {
        UserId = userId, CreatedAt = DateTime.UtcNow, EventDate = date, Shift = ShiftType.Morning,
        EventAddress = "x", ContactPhone = "0500000000", TotalPrice = 100, Status = OrderStatus.Confirmed,
        Items = [new OrderItem { ProductId = ProductId, Quantity = 1, UnitPrice = 100 }]
    };

    private async Task<(int UserId, int CartId)> SeedCustomerWithCartAsync(DateOnly date, int quantity)
    {
        await using var seed = _db.CreateContext();
        var user = NewUser($"{Guid.NewGuid():N}@test.com");
        var cart = new Cart
        {
            User = user, EventDate = date, Shift = ShiftType.Morning, UpdatedAt = DateTime.UtcNow,
            Items = [new CartItem { ProductId = ProductId, Quantity = quantity, AddedAt = DateTime.UtcNow }]
        };
        seed.Carts.Add(cart);
        await seed.SaveChangesAsync();
        return (user.Id, cart.Id);
    }
}
