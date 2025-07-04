using AuthService.Models;

namespace AuthService.Kafka;

public interface IKafkaProducerService
{
    Task SendNotificationAsync(NotificationMessage message);

}