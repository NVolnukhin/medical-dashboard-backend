using Shared;

namespace DataCollectorService.Services
{
    public interface IDataService
    {
        List<PatientDto> GetPatients();
        List<MetricDto> GetMetrics();
        event Action DataUpdated;
    }
}