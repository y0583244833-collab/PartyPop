using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PartyPop.Core.Entities;
using PartyPop.Core.Enums;
using PartyPop.Core.Interfaces.Services;
using PartyPop.Core.Settings;

namespace PartyPop.Data.Seed;

/// <summary>
/// יוצר את המנהל היחיד ואת לקוח הדמו בעליית המערכת, רק אם הם לא קיימים.
/// לא ב-HasData, כי BCrypt מייצר Hash שונה בכל הרצה (ואז Migration חדש בכל פעם).
/// </summary>
public class DbSeeder
{
    private readonly PartyPopDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly SeedSettings _settings;
    private readonly TimeProvider _time;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(PartyPopDbContext db, IPasswordHasher hasher, IOptions<SeedSettings> settings,
        TimeProvider time, ILogger<DbSeeder> logger)
    {
        _db = db;
        _hasher = hasher;
        _settings = settings.Value;
        _time = time;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedUserAsync(_settings.Admin, UserRole.Admin, ct);
        await SeedUserAsync(_settings.DemoCustomer, UserRole.Customer, ct);
    }

    private async Task SeedUserAsync(SeedUser seed, UserRole role, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(seed.Email) || string.IsNullOrWhiteSpace(seed.Password))
        {
            _logger.LogWarning("Seed user for role {Role} is not configured in User Secrets. Skipping.", role);
            return;
        }

        var email = seed.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return;

        var user = new User
        {
            FirstName = seed.FirstName,
            LastName = seed.LastName,
            Email = email,
            Phone = seed.Phone,
            PasswordHash = _hasher.Hash(seed.Password),
            Role = role,
            CreatedAt = _time.GetUtcNow().UtcDateTime
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seed user {UserId} created with role {Role}", user.Id, role);
    }
}
