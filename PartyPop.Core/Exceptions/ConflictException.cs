namespace PartyPop.Core.Exceptions;

/// <summary>→ 409. יכולה להחזיק רשימת פריטים לא זמינים (אישור הזמנה).</summary>
public class ConflictException : Exception
{
    public IReadOnlyList<UnavailableItem>? UnavailableItems { get; }

    public ConflictException(string message, IReadOnlyList<UnavailableItem>? unavailableItems = null)
        : base(message)
    {
        UnavailableItems = unavailableItems;
    }
}
