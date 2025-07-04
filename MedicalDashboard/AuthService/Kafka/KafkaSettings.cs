namespace AuthService.Kafka
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; }
        public string TopicName { get; set; }
        public string ProducerClientId { get; set; }
    }
} 