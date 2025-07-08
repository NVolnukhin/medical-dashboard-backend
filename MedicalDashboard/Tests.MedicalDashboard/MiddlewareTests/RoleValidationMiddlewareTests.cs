using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Middleware;
using Moq;
using Xunit;

namespace Tests.MedicalDashboard.MiddlewareTests;

public class RoleValidationMiddlewareTests
{
    private readonly Mock<ILogger<RoleValidationMiddleware>> _loggerMock = new();

    private DefaultHttpContext CreateHttpContext(string path, string method = "GET", string role = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        if (role != null)
        {
            var claims = new[] { new Claim(ClaimTypes.Role, role) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            context.User = new ClaimsPrincipal(identity);
        }
        return context;
    }

    [Xunit.Theory]
    [InlineData("/identity/login")]
    [InlineData("/identity/refresh-token")]
    [InlineData("/password-recovery/request")]
    public async Task Invoke_Allows_Public_Endpoints(string path)
    {
        var context = CreateHttpContext(path);
        var nextCalled = false;
        var middleware = new RoleValidationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, _loggerMock.Object);
        await middleware.Invoke(context);
        Xunit.Assert.True(nextCalled);
    }

    [Fact]
    public async Task Invoke_Developer_Always_Allows()
    {
        var context = CreateHttpContext("/any", role: "developer");
        var nextCalled = false;
        var middleware = new RoleValidationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, _loggerMock.Object);
        await middleware.Invoke(context);
        Xunit.Assert.True(nextCalled);
    }

    [Fact]
    public async Task Invoke_Admin_Required_For_Notifications()
    {
        var context = CreateHttpContext("/notifications", role: "doctor");
        var middleware = new RoleValidationMiddleware(_ => Task.CompletedTask, _loggerMock.Object);
        await middleware.Invoke(context);
        Xunit.Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_Doctor_Allowed_For_Patients_Post()
    {
        var context = CreateHttpContext("/patients", "POST", role: "doctor");
        var nextCalled = false;
        var middleware = new RoleValidationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, _loggerMock.Object);
        await middleware.Invoke(context);
        Xunit.Assert.True(nextCalled);
    }

    [Fact]
    public async Task Invoke_NonDoctor_Forbidden_For_Patients_Post()
    {
        var context = CreateHttpContext("/patients", "POST", role: "nurse");
        var middleware = new RoleValidationMiddleware(_ => Task.CompletedTask, _loggerMock.Object);
        await middleware.Invoke(context);
        Xunit.Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_Allows_SignalR_Hubs()
    {
        var context = CreateHttpContext("/hubs/metrics", role: "guest");
        var nextCalled = false;
        var middleware = new RoleValidationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, _loggerMock.Object);
        await middleware.Invoke(context);
        Xunit.Assert.True(nextCalled);
    }
}