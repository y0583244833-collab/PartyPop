using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PartyPop.Data;

public static class DependencyInjection
{
    /// <summary>רישום שכבת Data. נקרא מ-Program.cs.</summary>
    public static IServiceCollection AddDataLayer(this IServiceCollection services, string connectionString)
    {
        // בלי EnableRetryOnFailure: הוא מתנגש עם טרנזקציה ידנית (סעיף 6.1)
        services.AddDbContext<PartyPopDbContext>(options => options.UseNpgsql(connectionString));

        // ה-Repositories וה-UnitOfWork יירשמו כאן בשלבים הבאים (Scoped)

        return services;
    }

    /// <summary>מריץ Migrations שעוד לא רצו. נקרא בעליית המערכת ב-Development.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PartyPopDbContext>();
        await db.Database.MigrateAsync();
    }
}
