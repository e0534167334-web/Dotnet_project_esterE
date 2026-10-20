using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Baby_Clinic.Middleware;

public partial class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
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
        var correlationId = GetOrCreate(context);
        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        // כל שורת לוג בתוך הבקשה (Controllers, שירותים, EF) תכלול את המזהה
        using (_logger.BeginScope(new Dictionary<string, object> { [ItemKey] = correlationId }))
        {
            // רק method ונתיב: בלי query string ובלי גוף, כדי שסיסמאות ו-tokens לא יגיעו ללוג
            _logger.LogInformation("Incoming request {Method} {Path}", context.Request.Method, context.Request.Path);

            var stopwatch = Stopwatch.StartNew();
            await _next(context);

            _logger.LogDebug("Request {Method} {Path} finished with {StatusCode} in {ElapsedMs} ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }

    // מזהה מהלקוח מתקבל רק אם הוא קצר ובתווים בטוחים, כדי שלא יוכל לזייף שורות בלוג
    private static string GetOrCreate(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        return SafeIdRegex().IsMatch(incoming) ? incoming : Guid.NewGuid().ToString();
    }

    [GeneratedRegex("^[A-Za-z0-9-]{1,64}$")]
    private static partial Regex SafeIdRegex();
}
