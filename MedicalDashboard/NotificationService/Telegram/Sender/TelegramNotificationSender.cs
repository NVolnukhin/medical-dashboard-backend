using Microsoft.Extensions.Options;
using NotificationService.Config;
using NotificationService.Enums;
using NotificationService.Interfaces;
using NotificationService.Services.Retry;
using Shared.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace NotificationService.Telegram.Sender;

public class TelegramNotificationSender : INotificationSender
{
    private readonly TelegramSettings _telegramSettings;
    private readonly ILogger<TelegramNotificationSender> _logger;
    private readonly IRetryService _retryService;
    private readonly HttpClient _httpClient;

    public TelegramNotificationSender(
        IOptions<TelegramSettings> telegramSettings,
        ILogger<TelegramNotificationSender> logger,
        IRetryService retryService,
        HttpClient httpClient)
    {
        _telegramSettings = telegramSettings.Value;
        _logger = logger;
        _retryService = retryService;
        _httpClient = httpClient;
    }

    public NotificationType Type => NotificationType.Telegram;

    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInfo($"Подготовка к отправке сообщения в Telegram для получателя: {recipient}");

            await _retryService.ExecuteWithRetryAsync(
                async () => 
                {
                    await SendTelegramMessageAsync(subject, body, cancellationToken);
                    return Task.CompletedTask;
                },
                $"Отправка сообщения в Telegram для получателя {recipient}",
                cancellationToken);

            _logger.LogSuccess($"Сообщение успешно отправлено в Telegram для получателя {recipient}");
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка отправки сообщения в Telegram для получателя {recipient}", ex);
            throw;
        }
    }

    private async Task SendTelegramMessageAsync(string subject, string body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_telegramSettings.BotToken))
        {
            throw new InvalidOperationException("Bot token не настроен");
        }

        if (string.IsNullOrEmpty(_telegramSettings.ChatId))
        {
            throw new InvalidOperationException("Chat ID не настроен");
        }

        var message = $"*{subject}*\n\n{body}";

        var requestData = new
        {
            chat_id = _telegramSettings.ChatId,
            text = message,
            parse_mode = "Markdown"
        };

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"https://api.telegram.org/bot{_telegramSettings.BotToken}/sendMessage";

        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError($"Ошибка API Telegram: {response.StatusCode} - {errorContent}");
            throw new HttpRequestException($"Telegram API вернул ошибку: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogInfo($"Ответ от Telegram API: {responseContent}");
    }
} 