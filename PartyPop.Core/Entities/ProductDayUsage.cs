namespace PartyPop.Core.Entities;

/// <summary>
/// שורה אחת לכל מוצר + תאריך: כמה יחידות תפוסות בכל חצי יום.
/// זו הישות שמתחרים עליה, ועליה יושב ה-concurrency token.
/// </summary>
public class ProductDayUsage
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public DateOnly EventDate { get; set; }

    /// <summary>יחידות תפוסות 08:00–14:00.</summary>
    public int MorningReserved { get; set; }

    /// <summary>יחידות תפוסות 14:00–20:00.</summary>
    public int AfternoonReserved { get; set; }

    /// <summary>Concurrency token. ממופה ל-xmin ב-Data (Fluent API).</summary>
    public uint Version { get; set; }
}
