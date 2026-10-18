namespace PartyPop.Core.Exceptions;

/// <summary>→ 404</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
