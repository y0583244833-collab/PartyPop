using PartyPop.Core.Exceptions;

namespace PartyPop.API.Models;

/// <summary>מבנה JSON אחיד לכל תשובת שגיאה.</summary>
public class ErrorResponse
{
    public int Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>שגיאות ולידציה לפי שדה (רק ב-400 של ולידציה).</summary>
    public IDictionary<string, string[]>? Errors { get; init; }

    /// <summary>פריטים לא זמינים (רק ב-409 של אישור הזמנה).</summary>
    public IReadOnlyList<UnavailableItem>? UnavailableItems { get; init; }
}
