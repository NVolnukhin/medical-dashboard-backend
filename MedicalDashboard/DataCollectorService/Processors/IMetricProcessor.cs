using DataCollectorService.Models;
using DataCollectorService.Observerer;

namespace DataCollectorService.Processors
{
    public interface IMetricProcessor : IObserver
    {
        Task Generate(Patient patient);
        void Log(Patient patient, ILogger logger);
    }
}
