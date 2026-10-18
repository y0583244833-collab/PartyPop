using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PartyPop.API.Middleware;

/// <summary>
/// 1. קורא X-Correlation-Id מהבקשה או מייצר חדש.
/// 2. שומר ב-HttpContext.Items ומחזיר ב-Header של התשובה.
/// 3. פותח Logging Scope, כך שהמזהה מופיע בכל שורת לוג של הבקשה.
/// 4. כותב לוג התחלה וסיום לכל בקשה.
/// </summary>
public partial class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateId(context);
        context.Items[ItemKey] = correlationId;

        // ה-Header נוסף רגע לפני שליחת התשובה, גם בתשובת שגיאה
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        var method = context.Request.Method;
        var path = context.Request.Path.Value;   // רק Path, בלי Query String
        var stopwatch = Stopwatch.StartNew();

        // לוג הסיום נכתב אחרי שהתשובה נשלחה, כשהסטטוס כבר סופי (גם אם ה-Middleware של השגיאות שינה אותו)
        context.Response.OnCompleted(() =>
        {
            using (BeginScope(correlationId))
            {
                _logger.LogInformation("HTTP {Method} {Path} finished {StatusCode} in {ElapsedMs} ms",
                    method, path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
            }
            return Task.CompletedTask;
        });

        using (BeginScope(correlationId))
        {
            _logger.LogInformation("HTTP {Method} {Path} started", method, path);
            await _next(context);
        }
    }

    private IDisposable? BeginScope(string correlationId) =>
        _logger.BeginScope(new Dictionary<string, object> { [ItemKey] = correlationId });

    /// <summary>מקבל מזהה מהלקוח רק אם הוא קצר ובטוח, כדי שלא יזריקו טקסט ללוג.</summary>
    private static string GetOrCreateId(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        return !string.IsNullOrEmpty(incoming) && SafeId().IsMatch(incoming)
            ? incoming
            : Guid.NewGuid().ToString();
    }

    [GeneratedRegex("^[a-zA-Z0-9-]{1,64}$")]
    private static partial Regex SafeId();
}
