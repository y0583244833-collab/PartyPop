using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using PartyPop.API.Models;
using PartyPop.Core.Exceptions;

namespace PartyPop.API.Middleware;

/// <summary>
/// ראשון ב-Pipeline. תופס כל חריגה, כותב לוג ומחזיר JSON אחיד.
/// </summary>
public class ExceptionHandlingMiddleware
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping   // עברית קריאה ב-JSON
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // הלקוח ניתק באמצע. אין למי להחזיר תשובה.
            _logger.LogInformation("Request was cancelled by the client");
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            await HandleAsync(context, ex);
        }
        catch (Exception ex)
        {
            // כבר התחלנו לשלוח תשובה, ואי אפשר לשנות אותה. רושמים וממשיכים לזרוק.
            _logger.LogError(ex, "Unhandled exception after the response has started");
            throw;
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        // CorrelationIdMiddleware כבר יצא, אבל המזהה עדיין שמור ב-Items
        var correlationId = context.Items[CorrelationIdMiddleware.ItemKey] as string ?? context.TraceIdentifier;

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            [CorrelationIdMiddleware.ItemKey] = correlationId
        });

        var (status, message) = ex switch
        {
            UnauthorizedException => (StatusCodes.Status401Unauthorized, ex.Message),
            NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
            ForbiddenException => (StatusCodes.Status403Forbidden, ex.Message),
            BusinessRuleException => (StatusCodes.Status400BadRequest, ex.Message),
            ConflictException => (StatusCodes.Status409Conflict, ex.Message),
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "אירעה שגיאה בשרת. נא לנסות שוב מאוחר יותר.")
        };

        // רמות לוג: דחייה עסקית צפויה = Information, התנגשות = Warning, כשל = Error
        switch (ex)
        {
            case ConcurrencyConflictException:
                _logger.LogWarning(ex, "Concurrency conflict was not handled in the Service layer");
                break;
            case UnauthorizedException or NotFoundException or ForbiddenException or BusinessRuleException or ConflictException:
                _logger.LogInformation("Request rejected with {StatusCode}: {Reason}", status, ex.Message);
                break;
            default:
                _logger.LogError(ex, "Unhandled exception");
                break;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";

        var body = new ErrorResponse
        {
            Status = status,
            Message = message,
            CorrelationId = correlationId,
            UnavailableItems = (ex as ConflictException)?.UnavailableItems
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
