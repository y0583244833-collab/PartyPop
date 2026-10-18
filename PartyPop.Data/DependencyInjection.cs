using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PartyPop.Core.Interfaces.Repositories;
using PartyPop.Data.Repositories;
using PartyPop.Data.Seed;

namespace PartyPop.Data;

public static class DependencyInjection
{
    /// <summary>רישום שכבת Data. נקרא מ-Program.cs.</summary>
    public static IServiceCollection AddDataLayer(this IServiceCollection services, string connectionString)
    {
        // בלי EnableRetryOnFailure: הוא מתנגש עם טרנזקציה ידנית (סעיף 6.1)
        services.AddDbContext<PartyPopDbContext>(options => options.UseNpgsql(connectionString));

        // הכל Scoped, כמו ה-DbContext: מופע אחד לכל בקשה
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<DbSeeder>();

        return services;
    }

    /// <summary>
    /// מריץ Migrations שעוד לא רצו, ואז יוצר את משתמשי ה-Seed.
    /// נקרא בעליית המערכת ב-Development.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PartyPopDbContext>();
        await db.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync();
    }
}
