namespace PartyPop.Data;

/// <summary>שמות קבועים לאינדקסים, כדי שה-Configuration וה-catch ב-UnitOfWork ישתמשו באותו שם.</summary>
public static class DbConstraints
{
    public const string UserEmailUnique = "IX_Users_Email";
    public const string CategoryNameUnique = "IX_Categories_Name";
    public const string ProductDayUsageUnique = "IX_ProductDayUsage_Product_Date";
}
