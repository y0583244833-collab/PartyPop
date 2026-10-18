namespace PartyPop.Core.Exceptions;

/// <summary>→ 401. למשל אימייל או סיסמה שגויים בהתחברות.</summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "אימייל או סיסמה שגויים") : base(message) { }
}
