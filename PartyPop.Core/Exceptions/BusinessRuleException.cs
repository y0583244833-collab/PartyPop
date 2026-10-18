namespace PartyPop.Core.Exceptions;

/// <summary>הפרת כלל עסקי (למשל פחות מ-48 שעות לביטול) → 400</summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
