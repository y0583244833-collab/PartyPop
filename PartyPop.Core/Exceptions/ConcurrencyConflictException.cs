namespace PartyPop.Core.Exceptions;

/// <summary>
/// נזרקת מ-Data כשמישהו שמר לפנינו (token לא תואם / Unique Index של ProductDayUsage).
/// נתפסת ב-Service ומתורגמת ל-ConflictException. אם לא נתפסה, ה-Middleware מחזיר 409.
/// </summary>
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(Exception? innerException = null)
        : base("הנתונים השתנו על ידי פעולה אחרת. נא לנסות שוב.", innerException) { }
}
