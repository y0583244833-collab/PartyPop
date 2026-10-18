namespace PartyPop.Data.Configurations;

internal static class SeedConstants
{
    // ערך קבוע: DateTime.UtcNow ב-HasData יוצר Migration חדש בכל פעם
    public static readonly DateTime CreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
