using DataCollectorService.Models;

namespace DataCollectorService.Kafka
{
    public interface IKafkaService
    {
        Task ProduceAsync(string key, string message);
        Task SendToKafka(Patient patient, string metricName, double value);
        void Dispose();
    }
}
