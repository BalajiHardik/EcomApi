namespace EcomApi.Middleware;

public class SessionAuthMiddleware
{
    private readonly RequestDelegate _next;
    public SessionAuthMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.ToString().ToLower();

        // Allow anonymous endpoints: auth, swagger
        if (path.StartsWith("/api/auth") || path.StartsWith("/swagger") || path == "/" || path.StartsWith("/favicon.ico"))
        {
            await _next(context);
            return;
        }

        var userId = context.Session.GetInt32("UserId");
        var role = context.Session.GetString("Role");

        if (userId == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized - Please login");
            return;
        }

        // Admin-only endpoints start with /api/admin
        if (path.StartsWith("/api/admin") && role != "Admin")
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Forbidden - Admins only");
            return;
        }

        await _next(context);
    }
}
