using Microsoft.Extensions.DependencyInjection;
using PartyPop.Core.Interfaces.Services;
using PartyPop.Service.Auth;
using PartyPop.Service.Catalog;
using PartyPop.Service.Mapping;
using PartyPop.Service.Orders;

namespace PartyPop.Service;

public static class DependencyInjection
{
    /// <summary>רישום שכבת Service. נקרא מ-Program.cs.</summary>
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        // סורק את כל ה-Profiles בפרויקט Service
        services.AddAutoMapper(typeof(UserProfile).Assembly);

        // Singleton בטוח: אין להם תלות ב-Scoped
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();

        // חישוב טהור שתלוי רק ב-IOptions, ולכן גם הוא Singleton
        services.AddSingleton<IAvailabilityCalculator, AvailabilityCalculator>();

        // Scoped: תלויים ב-Repositories, שתלויים ב-DbContext
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}
