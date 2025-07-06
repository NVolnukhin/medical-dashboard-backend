using DataCollectorService.Models;

namespace DataCollectorService.Processors
{
    public interface IMetricProcessor
    {
        void Generate(Patient patient);
        void Log(Patient patient, ILogger logger);
    }
}
