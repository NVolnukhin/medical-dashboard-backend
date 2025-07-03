using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NotificationService.Config;
using NotificationService.Enums;
using NotificationService.Extensions.Logging;
using NotificationService.Services.Retry;
using NotificationService.Config;
using NotificationService.Interfaces;

namespace NotificationService.Email.Sender;

public class EmailNotificationSender : INotificationSender
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<EmailNotificationSender> _logger;
    private readonly IRetryService _retryService;

    public EmailNotificationSender(
        IOptions<SmtpSettings> smtpSettings,
        ILogger<EmailNotificationSender> logger,
        IRetryService retryService)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
        _retryService = retryService;
    }

    public NotificationType Type => NotificationType.Email;

    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            // _logger.LogInfo($"Preparing to send email to: {recipient}");
            // _logger.LogInfo($"SMTP settings - Host: {_smtpSettings.Host}, Port: {_smtpSettings.Port}, From: {_smtpSettings.From}");

            await _retryService.ExecuteWithRetryAsync(
                async () => 
                {
                    await SendEmailAsync(recipient, subject, body, cancellationToken);
                    return Task.CompletedTask;
                },
                $"Отправка письма на почту {recipient}",
                cancellationToken);

            _logger.LogSuccess($"Письмо успешно отправлено на почту {recipient}");
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка отправки письма на почту {recipient}", ex);
            throw;
        }
    }

    private async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        await SendEmailAsync(to, subject, body, null, cancellationToken);
    }

    private async Task SendEmailAsync(string to, string subject, string body, string? sender, CancellationToken cancellationToken)
    {
        SmtpClient? smtp = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(sender ?? _smtpSettings.From));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body,
                TextBody = "Данное письмо содержит HTML контент. Для корректного просмотра этого сообщения используйте почтовый клиент, поддерживающий HTML."
            };

            email.Body = bodyBuilder.ToMessageBody();

            smtp = new SmtpClient();
            
            // Устанавливаем таймауты для SMTP-клиента
            smtp.Timeout = 20000; // 20 секунд в миллисекундах
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            cancellationToken.ThrowIfCancellationRequested();
            await smtp.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, 
                _smtpSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
            
            cancellationToken.ThrowIfCancellationRequested();
            await smtp.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password, cancellationToken);
            
            cancellationToken.ThrowIfCancellationRequested();
            await smtp.SendAsync(email, cancellationToken);
            
            cancellationToken.ThrowIfCancellationRequested();
            await smtp.DisconnectAsync(true, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (smtp != null)
            {
                try
                {
                    await smtp.DisconnectAsync(true);
                }
                catch
                {
                    // Игнорируем ошибки при отключении
                }
            }
            throw;
        }
        catch (Exception)
        {
            if (smtp != null)
            {
                try
                {
                    await smtp.DisconnectAsync(true);
                }
                catch
                {
                    // Игнорируем ошибки при отключении
                }
            }
            throw;
        }
    }
} 

