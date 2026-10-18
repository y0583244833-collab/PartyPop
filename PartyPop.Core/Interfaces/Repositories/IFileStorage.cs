namespace PartyPop.Core.Interfaces.Repositories;

public interface IFileStorage
{
    /// <summary>שומר בשם ייחודי ומחזיר URL יחסי (למשל /images/abc.jpg).</summary>
    Task<string> SaveAsync(Stream content, string extension, CancellationToken ct);

    void Delete(string relativeUrl);
}
