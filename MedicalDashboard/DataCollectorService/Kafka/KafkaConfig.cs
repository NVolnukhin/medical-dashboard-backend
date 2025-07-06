namespace DataCollectorService.Kafka
{
    public class KafkaConfig
    {
        public string BootstrapServers { get; set; }
        public string RawInformationTopic { get; set; }
        public string MetricsTopic { get; set; }
        public string ClientId { get; set; }
        public string Acks { get; set; }
        public int MessageTimeoutMs { get; set; }
    }
}
