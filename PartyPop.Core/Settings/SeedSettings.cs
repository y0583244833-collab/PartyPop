namespace PartyPop.Core.Settings;

/// <summary>פרטי המנהל ולקוח הדמו. הערכים נשמרים ב-User Secrets בלבד.</summary>
public class SeedSettings
{
    public const string SectionName = "SeedSettings";

    public SeedUser Admin { get; set; } = new();
    public SeedUser DemoCustomer { get; set; } = new();
}

public class SeedUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
