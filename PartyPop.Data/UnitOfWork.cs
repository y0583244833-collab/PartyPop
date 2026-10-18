using Microsoft.EntityFrameworkCore;
using Npgsql;
using PartyPop.Core.Exceptions;
using PartyPop.Core.Interfaces.Repositories;

namespace PartyPop.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly PartyPopDbContext _db;

    public UnitOfWork(PartyPopDbContext db) => _db = db;

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        // הסדר חשוב: DbUpdateConcurrencyException יורשת מ-DbUpdateException
        catch (DbUpdateConcurrencyException ex)
        {
            // ה-token (xmin) לא תאם: מישהו שמר לפנינו
            throw new ConcurrencyConflictException(ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: DbConstraints.ProductDayUsageUnique
        })
        {
            // שני לקוחות יצרו במקביל את השורה הראשונה של אותו מוצר ותאריך
            throw new ConcurrencyConflictException(ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: DbConstraints.UserEmailUnique
        })
        {
            // שתי הרשמות במקביל עם אותו אימייל: הבדיקה ב-Service עברה בשתיהן, המסד עצר את השנייה
            throw new ConflictException("האימייל כבר רשום במערכת");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: DbConstraints.CategoryNameUnique
        })
        {
            // אותו רעיון: שני מנהלים יצרו במקביל קטגוריה עם אותו שם
            throw new ConflictException("כבר קיימת קטגוריה בשם הזה");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation
        })
        {
            // מחיקת קטגוריה שנוסף לה מוצר בין הבדיקה למחיקה (Restrict במסד)
            throw new ConflictException("לא ניתן למחוק: יש רשומות שתלויות בה");
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await action(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
