using NotificationService.Data.Models;

namespace NotificationService.Services.DeadLetter;

public interface IDeadLetterService
{
    Task<IEnumerable<DeadLetterMessage>> GetAllDeadLettersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DeadLetterMessage>> GetUnprocessedDeadLettersAsync(CancellationToken cancellationToken = default);
    Task<DeadLetterMessage> ProcessDeadLetterAsync(Guid id, CancellationToken cancellationToken = default);
    Task PublishToDeadLetterQueueAsync(string topic, string message, string errorMessage, string receiver, CancellationToken cancellationToken = default);
} 