namespace DataAnalysisService.Config;

public class AnalysisSettings
{
    public double AlertThresholdPercent { get; set; } = 5.0;
    public double WarningThresholdPercent { get; set; } = 3.0;
    public double WarningBoundaryPercent { get; set; } = 3.0;
    public int WarningTimeoutMinutes { get; set; } = 10;
    public int AlertTimeoutMinutes { get; set; } = 5;
} 