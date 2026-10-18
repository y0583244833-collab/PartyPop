namespace PartyPop.Core.Interfaces.Services;

/// <summary>
/// הצפנת סיסמאות. הממשק ב-Core, המימוש (BCrypt) ב-Service.
/// כך גם AuthService וגם DbSeeder משתמשים באותו מנגנון, ואפשר לדמות אותו בבדיקות.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
