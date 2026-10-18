using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PartyPop.API.Middleware;
using PartyPop.API.Models;
using PartyPop.Core.Settings;

namespace PartyPop.API.Extensions;

public static class JwtExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

        // HS256 דורש מפתח של 256 ביט לפחות
        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
            throw new InvalidOperationException(
                "Jwt:Key is missing or shorter than 32 characters. Set it with dotnet user-secrets.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // בלי זה .NET ממפה את "role" לשם ארוך, ו-[Authorize(Roles = "Admin")] לא עובד
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    RoleClaimType = "role",
                    NameClaimType = "sub"
                };

                // 401 ו-403 מחזירים את אותו JSON אחיד של ה-Middleware
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await WriteErrorAsync(context.HttpContext, StatusCodes.Status401Unauthorized, "נדרשת התחברות");
                    },
                    OnForbidden = context =>
                        WriteErrorAsync(context.HttpContext, StatusCodes.Status403Forbidden, "אין הרשאה לבצע פעולה זו")
                };
            });

        services.AddAuthorization();
        return services;
    }

    private static Task WriteErrorAsync(HttpContext context, int status, string message)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        var body = new ErrorResponse
        {
            Status = status,
            Message = message,
            CorrelationId = context.Items[CorrelationIdMiddleware.ItemKey] as string ?? context.TraceIdentifier
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(body, ExceptionHandlingMiddleware.JsonOptions));
    }
}
