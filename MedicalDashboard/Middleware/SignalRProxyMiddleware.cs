using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Middleware;

public class SignalRProxyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SignalRProxyMiddleware> _logger;
    private readonly string _targetUrl;

    public SignalRProxyMiddleware(RequestDelegate next, ILogger<SignalRProxyMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        
        // Получаем URL целевого сервиса из конфигурации
        var environment = configuration["ASPNETCORE_ENVIRONMENT"];
        if (environment == "Development")
        {
            _targetUrl = "http://localhost:5259";
        }
        else
        {
            _targetUrl = "http://dashboard-api:80";
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower();
        
        // Проверяем, является ли запрос SignalR
        if (path != null && path.StartsWith("/hubs/metrics"))
        {
            _logger.LogInformation("SignalR запрос: {Path} {Method}", path, context.Request.Method);
            
            // Обработка preflight OPTIONS запросов
            if (context.Request.Method == "OPTIONS")
            {
                await HandleOptionsRequest(context);
                return;
            }
            
            // Проверяем, является ли это WebSocket upgrade запросом
            var isWebSocketUpgrade = context.Request.Headers.ContainsKey("Upgrade") && 
                                   context.Request.Headers["Upgrade"].ToString().ToLower().Contains("websocket");
            
            if (isWebSocketUpgrade || context.WebSockets.IsWebSocketRequest)
            {
                _logger.LogInformation("Обрабатываем как WebSocket запрос");
                await HandleWebSocketRequest(context);
            }
            else
            {
                _logger.LogInformation("Обрабатываем как HTTP запрос");
                await HandleHttpRequest(context);
            }
        }
        else
        {
            await _next(context);
        }
    }

    private async Task HandleOptionsRequest(HttpContext context)
    {
        var origin = context.Request.Headers["Origin"].FirstOrDefault();
        if (!string.IsNullOrEmpty(origin))
        {
            var allowedOrigins = new[]
            {
                "http://localhost:3000",
                "http://localhost:4200", 
                "http://localhost:8080",
                "http://localhost:5000",
                "http://localhost:5173",
                "http://localhost:3001",
                "http://localhost:3002"
            };
            
            if (allowedOrigins.Contains(origin))
            {
                context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
                context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
                context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-Requested-With";
                context.Response.StatusCode = 200;
            }
        }
        
        await Task.CompletedTask;
    }

    private async Task HandleWebSocketRequest(HttpContext context)
    {
        try
        {
            _logger.LogInformation("WebSocket соединение принято от клиента");
            
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            
            // Создаем ClientWebSocket для подключения к DashboardAPI
            using var clientWebSocket = new ClientWebSocket();
            
            // Устанавливаем только Host заголовок для DashboardAPI
            clientWebSocket.Options.SetRequestHeader("Host", "dashboard-api:80");
            _logger.LogInformation("Установлен Host заголовок: dashboard-api:80");
            
            // Копируем только безопасные заголовки (НЕ WebSocket заголовки)
            var safeHeaders = new[] { "User-Agent", "X-Requested-With" };
            foreach (var headerName in safeHeaders)
            {
                var headerValue = context.Request.Headers[headerName].FirstOrDefault();
                if (!string.IsNullOrEmpty(headerValue))
                {
                    clientWebSocket.Options.SetRequestHeader(headerName, headerValue);
                    _logger.LogDebug("Установлен заголовок {Header}: {Value}", headerName, headerValue);
                }
            }
            
            // Логируем все заголовки для отладки
            _logger.LogInformation("Заголовки запроса:");
            _logger.LogInformation("  Host: dashboard-api:80");
            foreach (var headerName in safeHeaders)
            {
                var headerValue = context.Request.Headers[headerName].FirstOrDefault();
                if (!string.IsNullOrEmpty(headerValue))
                {
                    _logger.LogInformation("  {Header}: {Value}", headerName, headerValue);
                }
            }
            
            var targetUri = new Uri($"ws://dashboard-api:80{context.Request.Path}{context.Request.QueryString}");
            _logger.LogInformation("Подключение к: {TargetUri}", targetUri);
            
            await clientWebSocket.ConnectAsync(targetUri, CancellationToken.None);
            _logger.LogInformation("WebSocket соединение с сервером установлено");
            
            // Проксируем данные между клиентом и сервером
            await ProxyWebSocketData(webSocket, clientWebSocket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке WebSocket запроса");
        }
    }

    private async Task HandleHttpRequest(HttpContext context)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            // Копируем заголовки, исключая проблемные
            foreach (var header in context.Request.Headers)
            {
                if (!header.Key.StartsWith("host") && 
                    header.Key != "connection" &&
                    header.Key != "upgrade")
                {
                    try
                    {
                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Не удалось установить заголовок {Header}: {Error}", header.Key, ex.Message);
                    }
                }
            }

            var targetUri = $"{_targetUrl}{context.Request.Path}{context.Request.QueryString}";
            _logger.LogInformation("HTTP запрос к: {TargetUri}", targetUri);

            HttpResponseMessage response;
            
            if (context.Request.Method == "POST")
            {
                // Читаем тело запроса в память
                using var memoryStream = new MemoryStream();
                await context.Request.Body.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                
                using var content = new StreamContent(memoryStream);
                response = await httpClient.PostAsync(targetUri, content);
            }
            else
            {
                response = await httpClient.GetAsync(targetUri);
            }

            // Копируем ответ
            context.Response.StatusCode = (int)response.StatusCode;
            
            // Копируем заголовки ответа, но исключаем CORS заголовки
            foreach (var header in response.Headers)
            {
                if (!header.Key.StartsWith("access-control-"))
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }
            }
            foreach (var header in response.Content.Headers)
            {
                if (!header.Key.StartsWith("access-control-"))
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }
            }

            // Устанавливаем CORS заголовки для Gateway
            var origin = context.Request.Headers["Origin"].FirstOrDefault();
            if (!string.IsNullOrEmpty(origin))
            {
                // Проверяем, что origin разрешен
                var allowedOrigins = new[]
                {
                    "http://localhost:3000",
                    "http://localhost:4200", 
                    "http://localhost:8080",
                    "http://localhost:5000",
                    "http://localhost:5173",
                    "http://localhost:3001",
                    "http://localhost:3002"
                };
                
                if (allowedOrigins.Contains(origin))
                {
                    context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                    context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
                }
            }

            // Копируем содержимое ответа
            var responseContent = await response.Content.ReadAsByteArrayAsync();
            await context.Response.Body.WriteAsync(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке HTTP запроса");
            context.Response.StatusCode = 500;
        }
    }

    private async Task ProxyWebSocketData(WebSocket clientSocket, ClientWebSocket serverSocket)
    {
        var clientBuffer = new byte[4096];
        var serverBuffer = new byte[4096];
        var cancellationTokenSource = new CancellationTokenSource();
        
        _logger.LogInformation("Начинаем проксирование WebSocket данных");

        // Задача для передачи данных от клиента к серверу
        var clientToServer = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Запущена задача передачи данных от клиента к серверу");
                while (clientSocket.State == WebSocketState.Open && serverSocket.State == WebSocketState.Open)
                {
                    _logger.LogDebug("Ожидание данных от клиента...");
                    var result = await clientSocket.ReceiveAsync(new ArraySegment<byte>(clientBuffer), cancellationTokenSource.Token);
                    _logger.LogDebug("Получено {Count} байт от клиента, тип: {MessageType}", result.Count, result.MessageType);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("Клиент закрыл соединение");
                        await serverSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", CancellationToken.None);
                        break;
                    }
                    
                    if (result.Count > 0)
                    {
                        await serverSocket.SendAsync(new ArraySegment<byte>(clientBuffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
                        _logger.LogDebug("Отправлено {Count} байт серверу", result.Count);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Операция передачи данных от клиента к серверу отменена");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при передаче данных от клиента к серверу");
            }
            finally
            {
                cancellationTokenSource.Cancel();
            }
        });

        // Задача для передачи данных от сервера к клиенту
        var serverToClient = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Запущена задача передачи данных от сервера к клиенту");
                while (clientSocket.State == WebSocketState.Open && serverSocket.State == WebSocketState.Open)
                {
                    _logger.LogDebug("Ожидание данных от сервера...");
                    var result = await serverSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), cancellationTokenSource.Token);
                    _logger.LogDebug("Получено {Count} байт от сервера, тип: {MessageType}", result.Count, result.MessageType);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("Сервер закрыл соединение");
                        await clientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closed", CancellationToken.None);
                        break;
                    }
                    
                    if (result.Count > 0)
                    {
                        await clientSocket.SendAsync(new ArraySegment<byte>(serverBuffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
                        _logger.LogDebug("Отправлено {Count} байт клиенту", result.Count);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Операция передачи данных от сервера к клиенту отменена");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при передаче данных от сервера к клиенту");
            }
            finally
            {
                cancellationTokenSource.Cancel();
            }
        });

        // Ждем завершения любой из задач
        await Task.WhenAny(clientToServer, serverToClient);
        _logger.LogInformation("WebSocket проксирование завершено");
    }
} 