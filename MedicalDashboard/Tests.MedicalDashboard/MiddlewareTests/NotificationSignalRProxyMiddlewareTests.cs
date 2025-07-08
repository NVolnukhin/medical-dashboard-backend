using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Middleware;
using Moq;
using Xunit;

namespace Tests.MedicalDashboard.MiddlewareTests;

public class NotificationSignalRProxyMiddlewareTests
{
    private readonly Mock<ILogger<NotificationSignalRProxyMiddleware>> _loggerMock = new();
    private readonly Mock<IConfiguration> _configMock = new();

    private DefaultHttpContext CreateHttpContext(string path, string method = "GET")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        return context;
    }

    [Fact]
    public async Task InvokeAsync_CallsNext_ForNonAlertsPath()
    {
        var context = CreateHttpContext("/api/other");
        var nextCalled = false;
        var middleware = new NotificationSignalRProxyMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, _loggerMock.Object, _configMock.Object);
        await middleware.InvokeAsync(context);
        Xunit.Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_HandlesOptionsRequest()
    {
        var context = CreateHttpContext("/alerts", "OPTIONS");
        var middleware = new NotificationSignalRProxyMiddleware(_ => Task.CompletedTask, _loggerMock.Object, _configMock.Object);
        await middleware.InvokeAsync(context);
        Xunit.Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_HandlesWebSocketRequest()
    {
        var context = CreateHttpContext("/alerts");
        context.Request.Headers["Upgrade"] = "websocket";
        context.Features.Set(Mock.Of<IHttpWebSocketFeature>(f => f.IsWebSocketRequest == true && f.AcceptAsync(null) == Task.FromResult((WebSocket)null)));
        var middleware = new NotificationSignalRProxyMiddleware(_ => Task.CompletedTask, _loggerMock.Object, _configMock.Object);
        await middleware.InvokeAsync(context);
    }
}