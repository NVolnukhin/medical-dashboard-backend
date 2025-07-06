using DataCollectorService.Models;

namespace DataCollectorService.Processors
{
    public interface IMetricProcessor
    {
        Task Generate(Patient patient);
        void Log(Patient patient, ILogger logger);
    }
}
