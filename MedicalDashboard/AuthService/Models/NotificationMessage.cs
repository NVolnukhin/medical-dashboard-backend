namespace AuthService.Models;

public class NotificationMessage
{
    public int Type { get; set; }
    public string Recipient { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public int Priority { get; set; }
    public string TemplateName { get; set; }
    public Dictionary<string, string> TemplateParameters { get; set; }
}
