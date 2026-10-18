namespace PartyPop.Core.Exceptions;

/// <summary>→ 403</summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "אין הרשאה לבצע פעולה זו") : base(message) { }
}
