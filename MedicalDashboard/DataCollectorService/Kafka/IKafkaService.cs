using System;
using Confluent.Kafka;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;

namespace Kafka
{
    public interface IKafkaService
    {
        Task ProduceAsync(string key, string message);
        Task SendToKafka(Patient patient, string metricName, double value);
        void Dispose();
    }
}
