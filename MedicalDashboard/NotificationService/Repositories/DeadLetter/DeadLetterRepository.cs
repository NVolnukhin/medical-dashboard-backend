using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Data.Models;

namespace NotificationService.Repositories.DeadLetter;

public class DeadLetterRepository : IDeadLetterRepository
{
    private readonly ApplicationDbContext _context;

    public DeadLetterRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DeadLetterMessage> AddAsync(DeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        message.Id = Guid.NewGuid();
        message.CreatedAt = DateTime.UtcNow;
        message.IsProcessed = false;

        await _context.DeadLetterMessages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return message;
    }

    public async Task<DeadLetterMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DeadLetterMessages.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<DeadLetterMessage>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DeadLetterMessages
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DeadLetterMessage>> GetUnprocessedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DeadLetterMessages
            .Where(m => !m.IsProcessed)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<DeadLetterMessage> UpdateAsync(DeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        _context.DeadLetterMessages.Update(message);
        await _context.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<DeadLetterMessage> MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await GetByIdAsync(id, cancellationToken);
        if (message == null)
        {
            throw new KeyNotFoundException($"'Мертвое письмо' с ID {id} не найдено");
        }

        message.IsProcessed = true;
        message.ProcessedAt = DateTime.UtcNow;
        
        return await UpdateAsync(message, cancellationToken);
    }
} 