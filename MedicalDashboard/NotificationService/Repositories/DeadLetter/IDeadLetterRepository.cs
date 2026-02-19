using NotificationService.Data.Models;

namespace NotificationService.Repositories.DeadLetter;

public interface IDeadLetterRepository
{
    Task<DeadLetterMessage> AddAsync(DeadLetterMessage message, CancellationToken cancellationToken = default);
    Task<DeadLetterMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<DeadLetterMessage>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DeadLetterMessage>> GetUnprocessedAsync(CancellationToken cancellationToken = default);
    Task<DeadLetterMessage> UpdateAsync(DeadLetterMessage message, CancellationToken cancellationToken = default);
    Task<DeadLetterMessage> MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default);
}