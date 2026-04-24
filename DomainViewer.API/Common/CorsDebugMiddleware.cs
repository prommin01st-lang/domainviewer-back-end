namespace DomainViewer.API.Common;

public class CorsDebugMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorsDebugMiddleware> _logger;

    public CorsDebugMiddleware(RequestDelegate next, ILogger<CorsDebugMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var origin = context.Request.Headers["Origin"].ToString();
        var requestMethod = context.Request.Method;
        var path = context.Request.Path;

        _logger.LogInformation("[CORS-DEBUG] {Method} {Path} | Origin: {Origin}", requestMethod, path, string.IsNullOrEmpty(origin) ? "(none)" : origin);

        // Echo back the origin for all CORS requests
        if (!string.IsNullOrEmpty(origin))
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = origin;
            context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, PATCH, OPTIONS";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-Requested-With";
            context.Response.Headers["Access-Control-Expose-Headers"] = "X-Pagination";
        }

        // Handle preflight immediately
        if (requestMethod == "OPTIONS")
        {
            _logger.LogInformation("[CORS-DEBUG] Preflight request handled");
            context.Response.StatusCode = 204;
            return;
        }

        await _next(context);

        var hasCorsHeader = context.Response.Headers.ContainsKey("Access-Control-Allow-Origin");
        _logger.LogInformation("[CORS-DEBUG] Response {StatusCode} | Has CORS header: {HasCors}", context.Response.StatusCode, hasCorsHeader);
    }
}
