namespace AuthService.Kafka
{
    public class KafkaConfig
    {
        public string BootstrapServers { get; set; }
        public string TopicName { get; set; }
        public string ProducerClientId { get; set; }
    }
} 