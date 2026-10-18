using PartyPop.Core.Entities;

namespace PartyPop.Core.Models;

/// <summary>
/// תוצאת שאילתה אחת: מוצר + המונים שלו לתאריך מסוים (0 אם אין שורה).
/// מאפשר לחשב זמינות בלי N+1.
/// </summary>
public record ProductWithUsage(Product Product, int MorningReserved, int AfternoonReserved);
