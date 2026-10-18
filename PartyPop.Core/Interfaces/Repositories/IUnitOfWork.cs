namespace PartyPop.Core.Interfaces.Repositories;

public interface IUnitOfWork
{
    /// <summary>זורק ConcurrencyConflictException בהתנגשות.</summary>
    Task SaveChangesAsync(CancellationToken ct);

    /// <summary>
    /// מריץ את הפעולה בטרנזקציה אחת. כשל → Rollback וזריקה הלאה.
    /// ה-catch ב-Service עוטף את הקריאה הזו, לא קוד שבתוך ה-lambda.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct);
}
