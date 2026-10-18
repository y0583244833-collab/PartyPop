using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using PartyPop.API.Middleware;
using PartyPop.API.Models;
using PartyPop.Core.Settings;

namespace PartyPop.API.Extensions;

public static class ServiceCollectionExtensions
{
    public const string CorsPolicyName = "Client";

    /// <summary>קישור מחלקות ה-Settings מ-Core לקונפיגורציה (IOptions).</summary>
    public static IServiceCollection AddAppOptions(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment env)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<ShiftSettings>(configuration.GetSection(ShiftSettings.SectionName));
        services.Configure<PricingSettings>(configuration.GetSection(PricingSettings.SectionName));
        services.Configure<SeedSettings>(configuration.GetSection(SeedSettings.SectionName));
        services.Configure<FileStorageSettings>(configuration.GetSection(FileStorageSettings.SectionName));

        // תיקיית התמונות נקבעת לפי wwwroot של הפרויקט
        services.PostConfigure<FileStorageSettings>(o =>
        {
            if (string.IsNullOrWhiteSpace(o.RootPath))
                o.RootPath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        });

        return services;
    }

    /// <summary>שגיאת ולידציה (ModelState) מחזירה 400 באותו JSON אחיד של ה-Middleware.</summary>
    public static IMvcBuilder AddUnifiedValidationResponse(this IMvcBuilder builder)
    {
        builder.ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        e => e.Key,
                        e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());

                var body = new ErrorResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "הנתונים שנשלחו אינם תקינים",
                    CorrelationId = context.HttpContext.Items[CorrelationIdMiddleware.ItemKey] as string
                                    ?? context.HttpContext.TraceIdentifier,
                    Errors = errors
                };
                return new BadRequestObjectResult(body);
            };
        });
        return builder;
    }

    /// <summary>Swagger עם כפתור Authorize ל-JWT (ישמש משלב 5).</summary>
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "PartyPop API", Version = "v1" });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "הדביקי כאן את ה-Token (בלי המילה Bearer)"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });
        return services;
    }

    /// <summary>CORS ללקוח ה-React. הכתובות מגיעות מהקונפיגורציה.</summary>
    public static IServiceCollection AddClientCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options =>
            options.AddPolicy(CorsPolicyName, policy =>
                policy.WithOrigins(origins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .WithExposedHeaders(CorrelationIdMiddleware.HeaderName, "Location")));
        return services;
    }
}
