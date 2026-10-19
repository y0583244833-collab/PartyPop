//using System.Text.Encodings.Web;
//using System.Text.Json.Serialization;
//using NLog;
//using NLog.Web;
//using PartyPop.API.Extensions;
//using PartyPop.API.Middleware;
//using PartyPop.Data;
//using PartyPop.Service;

//// לוגר ראשוני: תופס שגיאות שקורות עוד לפני שהמערכת עלתה
//var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

//try
//{
//    var builder = WebApplication.CreateBuilder(args);

//    // ---------- Logging: NLog מתחת ל-ILogger הרגיל ----------
//    builder.Logging.ClearProviders();
//    builder.Host.UseNLog();

//    // ---------- Configuration (IOptions) ----------
//    builder.Services.AddAppOptions(builder.Configuration, builder.Environment);

//    // ---------- Data ----------
//    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
//        ?? throw new InvalidOperationException(
//            "Connection string 'DefaultConnection' is missing. Set it with dotnet user-secrets.");
//    builder.Services.AddDataLayer(connectionString);

//    // ---------- Service ----------
//    builder.Services.AddServiceLayer();

//    // ---------- שירותים כלליים ----------
//    // Singleton בטוח: TimeProvider לא מחזיק שום Scoped
//    builder.Services.AddSingleton(TimeProvider.System);

//    // ---------- API ----------
//    builder.Services
//        .AddControllers()
//        .AddJsonOptions(o =>
//        {
//            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());   // "Morning" ולא 0
//            o.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
//        })
//        .AddUnifiedValidationResponse();

//    builder.Services.AddSwaggerWithJwt();
//    builder.Services.AddClientCors(builder.Configuration);

//    // ---------- Auth: JWT ----------
//    builder.Services.AddJwtAuthentication(builder.Configuration);




//    var app = builder.Build();

//    // ---------- עלייה בלי התערבות ידנית ----------
//    if (app.Environment.IsDevelopment())
//    {
//        await app.Services.InitializeDatabaseAsync();   // Migrations + מנהל ולקוח דמו
//    }

//    // ---------- Pipeline (הסדר חשוב!) ----------
//    app.UseMiddleware<ExceptionHandlingMiddleware>();   // 1. ראשון: תופס הכל
//    app.UseMiddleware<CorrelationIdMiddleware>();       // 2. מזהה + לוג לכל בקשה

//    if (app.Environment.IsDevelopment())
//    {
//        app.UseSwagger();                               // 3.
//        app.UseSwaggerUI();
//    }

//    app.UseHttpsRedirection();                          // 4.
//    app.UseStaticFiles();                               // 5. תמונות מ-wwwroot
//    app.UseCors("Client");   // 6. לפני Authentication
//    app.UseAuthentication();                            // 7. מי את?
//    app.UseAuthorization();                             // 8. מה מותר לך?
//    app.MapControllers();                               // 9.

//    logger.Info("PartyPop API started");
//    app.Run();
//}
//catch (Exception ex) when (ex is not HostAbortedException)   // HostAbortedException = dotnet ef, לא שגיאה
//{
//    logger.Error(ex, "PartyPop API stopped because of an exception");
//    throw;
//}
//finally
//{
//    // מוודא שהלוגים האחרונים נכתבים לקובץ
//    LogManager.Shutdown();
//}

//public partial class Program { }
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using NLog;
using NLog.Web;
using PartyPop.API.Extensions;
using PartyPop.API.Middleware;
using PartyPop.Data;
using PartyPop.Service;

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

    // ---------- Service ----------
    builder.Services.AddServiceLayer();

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

    // ---------- CORS: הלקוח המקומי והלקוח ב-Render ----------
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Client", policy =>
            policy.WithOrigins(
                    "http://localhost:5173",
                    "https://partypop-client.onrender.com")
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    });

    // ---------- Auth: JWT ----------
    builder.Services.AddJwtAuthentication(builder.Configuration);

    var app = builder.Build();

    // ---------- עלייה בלי התערבות ידנית ----------
    // רץ גם ב-Render: Migrations + מנהל ולקוח דמו
    await app.Services.InitializeDatabaseAsync();

    // ---------- Pipeline (הסדר חשוב!) ----------
    app.UseMiddleware<ExceptionHandlingMiddleware>();   // 1. ראשון: תופס הכל
    app.UseMiddleware<CorrelationIdMiddleware>();       // 2. מזהה + לוג לכל בקשה

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();                               // 3.
        app.UseSwaggerUI();
    }

    // app.UseHttpsRedirection();   // מבוטל: ב-Render זה שובר את בקשת ה-preflight
    app.UseStaticFiles();                               // 4. תמונות מ-wwwroot
    app.UseCors("Client");                              // 5. לפני Authentication
    app.UseAuthentication();                            // 6. מי את?
    app.UseAuthorization();                             // 7. מה מותר לך?
    app.MapControllers();                               // 8.

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
