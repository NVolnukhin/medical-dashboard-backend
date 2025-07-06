namespace Shared.MetricLimits;

public class MetricsConfig
{
    public MetricLimits Pulse { get; set; } = new() { Min = 60, Max = 100 };
    public MetricLimits RespirationRate { get; set; } = new() { Min = 12, Max = 20 };
    public MetricLimits Temperature { get; set; } = new() { Min = 36.0, Max = 37.5 };
    public MetricLimits SystolicPressure { get; set; } = new() { Min = 90, Max = 140 };
    public MetricLimits DiastolicPressure { get; set; } = new() { Min = 60, Max = 90 };
    public MetricLimits Saturation { get; set; } = new() { Min = 95, Max = 100 };
    public MetricLimits Weight { get; set; } = new() { Min = 30, Max = 200 };
    public MetricLimits Hemoglobin { get; set; } = new() { Min = 120, Max = 180 };
    public MetricLimits Cholesterol { get; set; } = new() { Min = 3.0, Max = 5.2 };
}