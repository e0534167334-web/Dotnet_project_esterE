namespace Baby_Clinic.Middleware;

public class ExceptionHandlingMiddleware
{
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
            // הלקוח סגר את החיבור: זו לא שגיאה של השרת ואין למי להחזיר תשובה
            _logger.LogInformation("Request {Method} {Path} was cancelled by the client",
                context.Request.Method, context.Request.Path);
        }
        catch (Exception ex)
        {
            // ה-middleware הזה ראשון ב-pipeline, ולכן ה-scope של CorrelationId כבר נסגר כאן; פותחים scope משלו
            var correlationId = context.Items[CorrelationIdMiddleware.ItemKey] as string ?? context.TraceIdentifier;
            using (_logger.BeginScope(new Dictionary<string, object> { [CorrelationIdMiddleware.ItemKey] = correlationId }))
            {
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            }

            if (context.Response.HasStarted)
                throw;

            // JSON אחיד בלי stack trace ובלי פרטי החריגה
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                statusCode = StatusCodes.Status500InternalServerError,
                message = "An unexpected error occurred. Please try again later.",
                correlationId
            });
        }
    }
}
