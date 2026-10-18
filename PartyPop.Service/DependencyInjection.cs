using Microsoft.Extensions.DependencyInjection;
using PartyPop.Core.Interfaces.Services;
using PartyPop.Service.Auth;
using PartyPop.Service.Mapping;

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

        // Scoped: תלויים ב-Repositories, שתלויים ב-DbContext
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
