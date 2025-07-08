using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Middleware;

public class RoleValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RoleValidationMiddleware> _logger;

    public RoleValidationMiddleware(RequestDelegate next, ILogger<RoleValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        // Получаем роль 
        var role = context.User.Claims
            .FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/Role")?.Value ?? "guest"
            .ToLower(); 
        _logger.LogInformation($"роль - {role}");
        
        if (role == "developer")
        {
            await _next(context);
            return;
        }
        
        // Пропускаем публичные эндпоинты
        if (IsPublicEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }
        
        if (string.IsNullOrEmpty(role))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Role claim is required" });
            return;
        }

        // Проверяем доступ (пример для /patients)
        if (context.Request.Path.StartsWithSegments("/patients") && 
            role != "doctor")
        {
            _logger.LogWarning($"Access denied. Required role: doctor, Actual role: {role}");
            
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Requires doctor role" });
            
            return;
        }

        await _next(context);
    }

    private bool IsPublicEndpoint(PathString path)
    {
        return path.StartsWithSegments("/identity/login") ||
               path.StartsWithSegments("/identity/refresh-token") ||
               path.StartsWithSegments("/identity/revoke-token") ||
               path.StartsWithSegments("/identity/update-password") ||
               path.StartsWithSegments("/password-recovery/request") ||
               path.StartsWithSegments("/password-recovery/confirm");
    }
}