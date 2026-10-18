using PartyPop.Core.Enums;

namespace PartyPop.Core.Entities;

public class Cart
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateOnly? EventDate { get; set; }
    public ShiftType? Shift { get; set; }

    /// <summary>מתעדכן בכל פעולה על הסל או על פריט בו, כדי שה-Version ישתנה.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Concurrency token. ממופה ל-xmin ב-Data (Fluent API).</summary>
    public uint Version { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
