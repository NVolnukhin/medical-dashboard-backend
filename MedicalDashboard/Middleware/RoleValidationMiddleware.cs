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

        // Проверяем доступ ( /notifications )
        if (context.Request.Path.StartsWithSegments("/notifications") && 
            role != "admin")
        {
            _logger.LogWarning($"Access denied. Required role: 'admin', Actual role: '{role}'");
            
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Requires admin role" });
            return;
        }
        
        // Проверяем доступ ( /dead-letters )
        if (context.Request.Path.StartsWithSegments("/dead-letters") && 
            role != "admin")
        {
            _logger.LogWarning($"Access denied. Required role: 'admin', Actual role: '{role}'");
            
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Requires admin role" });
            return;
        }

        // Проверка доступа к эндпоинтам DashboardAPI
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        var method = context.Request.Method.ToUpper();
        
        // Проверяем доступ ( /patients )
        if (path.StartsWith("/patients"))
        {
            if (method == "GET")
            {
                // Все роли имеют доступ (admin только RO - аудит)
                await _next(context);
                return;
            }
            if (method == "POST" || method == "PUT" || method == "DELETE")
            {
                if (role != "doctor")
                {
                    _logger.LogWarning($"Access denied to {path} {method}. Required role: 'doctor', Actual role: '{role}'");
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { error = "Requires doctor role" });
                    return;
                }
            }
        }

        // Проверяем доступ ( /metrics )
        if (path.StartsWith("/metrics"))
        {
            if (method == "GET")
            {
                // Все роли имеют доступ (admin только RO)
                await _next(context);
                return;
            }
            if (method == "POST")
            {
                if (role != "doctor")
                {
                    _logger.LogWarning($"Access denied to {path} {method}. Required role: 'doctor', Actual role: '{role}'");
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { error = "Requires doctor role" });
                    return;
                }
            }
        }

        // Проверяем доступ ( /patient-alerts )
        if (path.StartsWith("/patient-alerts"))
        {
            if (method == "GET")
            {
                // Все роли имеют доступ (admin только RO)
                await _next(context);
                return;
            }
            if (method == "POST" && path.Contains("/ack"))
            {
                // doctor, nurse, admin (audit)
                await _next(context);
                return;
            }
            if (method == "DELETE")
            {
                if (role != "admin")
                {
                    _logger.LogWarning($"Access denied to {path} {method}. Required role: 'admin', Actual role: '{role}'");
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { error = "Requires admin role" });
                    return;
                }
            }
        }

        // Проверяем доступ ( /devices )
        if (path.StartsWith("/devices"))
        {
            if (method == "GET")
            {
                await _next(context);
                return;
            }

            if (method == "POST" && path == "/devices")
            {
                // Только admin
                if (role != "admin")
                {
                    _logger.LogWarning($"Access denied to {path} {method}. Required role: 'admin', Actual role: '{role}'");
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { error = "Requires admin role" });
                    return;
                }
            }

            if (method == "PUT" && path.StartsWith("/devices/"))
            {
                // Только admin
                if (role != "admin")
                {
                    _logger.LogWarning($"Access denied to {path} {method}. Required role: 'admin', Actual role: '{role}'");
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { error = "Requires admin role" });
                    return;
                }
            }

            if (method == "DELETE" && path.StartsWith("/devices/"))
            {
                // Только admin
                if (role != "admin")
                {
                    _logger.LogWarning($"Access denied to {path} {method}. Required role: 'admin', Actual role: '{role}'");
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { error = "Requires admin role" });
                    return;
                }
            }

            if (method == "POST" && path.EndsWith("/attach"))
            {
                // Только doctor
                if (role != "doctor")
                {
                    _logger.LogWarning($"Access denied to {path} {method}. Required role: 'doctor', Actual role: '{role}'");
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { error = "Requires doctor role" });
                    return;
                }
            }

            if (method == "POST" && path.EndsWith("/detach"))
            {
                // Только doctor
                if (role != "doctor")
                {
                    _logger.LogWarning($"Access denied to {path} {method}. Required role: 'doctor', Actual role: '{role}'");
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { error = "Requires doctor role" });
                    return;
                }
            }
        }

        // SignalR hubs — доступны всем ролям
        if (path.StartsWith("/hubs/") || path.StartsWith("/alerts"))
        {
            await _next(context);
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