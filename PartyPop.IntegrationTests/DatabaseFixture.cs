using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PartyPop.Data;

namespace PartyPop.IntegrationTests;

/// <summary>
/// מסד PostgreSQL אמיתי ונפרד לבדיקות: נוצר עם MigrateAsync בתחילת ההרצה ונמחק בסוף.
/// xmin קיים רק ב-PostgreSQL אמיתי, ולכן אי אפשר לבדוק את זה עם Moq או InMemory.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; }

    public DatabaseFixture()
    {
        // ה-Connection String מ-User Secrets או ממשתנה סביבה, לא מהקוד
        var config = new ConfigurationBuilder()
            .AddUserSecrets<DatabaseFixture>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var baseConnection = config.GetConnectionString("TestDb")
            ?? throw new InvalidOperationException(
                "חסר ConnectionStrings:TestDb. הגדירי אותו עם dotnet user-secrets (ראו STEP12.md).");

        // שם ייחודי לכל הרצה, כדי שלא ייגע במסד האמיתי
        ConnectionString = new NpgsqlConnectionStringBuilder(baseConnection)
        {
            Database = $"partypop_test_{Guid.NewGuid():N}"
        }.ConnectionString;
    }

    public PartyPopDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<PartyPopDbContext>().UseNpgsql(ConnectionString).Options);

    // MigrateAsync ולא EnsureCreated: כך גם ה-Migrations האמיתיים נבדקים
    public async Task InitializeAsync()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }
}
