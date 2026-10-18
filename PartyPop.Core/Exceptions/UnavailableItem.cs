namespace PartyPop.Core.Exceptions;

/// <summary>פריט בסל שאין ממנו מספיק יחידות. מוחזר ברשימת unavailableItems ב-409.</summary>
public record UnavailableItem(int CartItemId, string ProductName, int Requested, int Available);
