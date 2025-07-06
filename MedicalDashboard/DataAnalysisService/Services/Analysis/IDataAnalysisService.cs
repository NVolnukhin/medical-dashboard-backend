using Shared;

namespace DataAnalysisService.Services.Analysis;

public interface IDataAnalysisService
{
    Task AnalyzeMetricAsync(MetricDto metric);
} 