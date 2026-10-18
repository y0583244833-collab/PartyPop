using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using NLog;
using NLog.Web;
using PartyPop.API.Extensions;
using PartyPop.API.Middleware;
using PartyPop.Data;

// לוגר ראשוני: תופס שגיאות שקורות עוד לפני שהמערכת עלתה
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ---------- Logging: NLog מתחת ל-ILogger הרגיל ----------
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // ---------- Configuration (IOptions) ----------
    builder.Services.AddAppOptions(builder.Configuration, builder.Environment);

    // ---------- Data ----------
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is missing. Set it with dotnet user-secrets.");
    builder.Services.AddDataLayer(connectionString);

    // ---------- שירותים כלליים ----------
    // Singleton בטוח: TimeProvider לא מחזיק שום Scoped
    builder.Services.AddSingleton(TimeProvider.System);

    // ---------- API ----------
    builder.Services
        .AddControllers()
        .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());   // "Morning" ולא 0
            o.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        })
        .AddUnifiedValidationResponse();

    builder.Services.AddSwaggerWithJwt();
    builder.Services.AddClientCors(builder.Configuration);

    // ---------- Auth (JWT יוגדר בשלב 5) ----------
    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization();

    var app = builder.Build();

    // ---------- עלייה בלי התערבות ידנית ----------
    if (app.Environment.IsDevelopment())
    {
        await app.Services.MigrateDatabaseAsync();
    }

    // ---------- Pipeline (הסדר חשוב!) ----------
    app.UseMiddleware<ExceptionHandlingMiddleware>();   // 1. ראשון: תופס הכל
    app.UseMiddleware<CorrelationIdMiddleware>();       // 2. מזהה + לוג לכל בקשה

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();                               // 3.
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();                          // 4.
    app.UseStaticFiles();
    // 5. תמונות מ-wwwroot
    app.UseCors("Client");   // 6. לפני Authentication
    app.UseAuthentication();                            // 7. מי את?
    app.UseAuthorization();                             // 8. מה מותר לך?
    app.MapControllers();                               // 9.

    logger.Info("PartyPop API started");
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)   // HostAbortedException = dotnet ef, לא שגיאה
{
    logger.Error(ex, "PartyPop API stopped because of an exception");
    throw;
}
finally
{
    // מוודא שהלוגים האחרונים נכתבים לקובץ
    LogManager.Shutdown();
}

public partial class Program { }
