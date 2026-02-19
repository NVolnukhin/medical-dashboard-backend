using DataCollectorService.Models;
using DataCollectorService.Observerer;

namespace DataCollectorService.Processors
{
    public interface IMetricProcessor : IObserver
    {
        public Task ProcessPatientMetric(Patient patient, string metricName, DateTime now);
        void Log(Patient patient, ILogger logger);
    }
}
