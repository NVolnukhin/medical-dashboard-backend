using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using NotificationService.Converters;
using NotificationService.Enums;

namespace NotificationService.Data.Models;

public class NotificationRequest
{
    [JsonConverter(typeof(NotificationTypeConverter))]
    public NotificationType Type { get; set; }

    [Required]
    [RegularExpression(@"(^\+?\d{7,15}$)|(^[\w\.-]+@[\w\.-]+\.\w{2,4}$)", ErrorMessage = "Введите корректный email или номер телефона.")]
    public string Recipient { get; set; } = string.Empty;

    [Required]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    [Required]
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    
    public string? TemplateName { get; set; }
    
    public Dictionary<string, string>? TemplateParameters { get; set; }

    public string? Sender { get; set; }

    // Поля для хранения данных шаблона
    public string? Topic { get; set; }
    public string? Message { get; set; }
} 