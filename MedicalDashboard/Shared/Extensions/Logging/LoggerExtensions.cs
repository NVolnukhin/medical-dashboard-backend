using Microsoft.Extensions.Logging;

namespace Shared.Extensions.Logging;

public static class LoggerExtensions
{
    private const string YellowColor = "\x1b[33m";
    private const string GreenColor = "\x1b[32m";
    private const string RedColor = "\x1b[31m";
    private const string ResetColor = "\x1b[0m";

    public static void LogSuccess(this ILogger logger, string message)
    {
        logger.LogInformation($"{GreenColor}[SUCCESS, {{DateTime}}] {{Message}}{ResetColor}", 
            DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"), 
            message);
    }
    
    public static void LogWarning(this ILogger logger, string message)
    {
        logger.LogInformation($"{YellowColor}[WARN, {{DateTime}}] {{Message}}{ResetColor}", 
            DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"), 
            message);
    }

    public static void LogFailure(this ILogger logger, string message, Exception? ex = null)
    {
        if (ex != null)
        {
            logger.LogError($"{RedColor}[FAILED, {{DateTime}}] ErrorMessage: {{Message}}{ResetColor}", 
                DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"), 
                ex.Message);
        }
        else
        {
            logger.LogError($"{RedColor}[FAILED, {{DateTime}}] ErrorMessage: {{Message}}{ResetColor}", 
                DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"), 
                message);
        }
    }

    public static void LogInfo(this ILogger logger, string message)
    {
        logger.LogInformation("[INFO, {DateTime}] {Message}", 
            DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"), 
            message);
    }
} 