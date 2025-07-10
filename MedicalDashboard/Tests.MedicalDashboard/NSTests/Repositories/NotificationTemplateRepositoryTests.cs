using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Data.Models;
using NotificationService.Enums;
using NotificationService.Repositories.Template;
using Xunit;

namespace Tests.MedicalDashboard.NSTests.Repositories
{
    public class NotificationTemplateRepositoryTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private readonly ApplicationDbContext _context;
        private readonly NotificationTemplateRepository _repository;

        public NotificationTemplateRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(_options);
            _repository = new NotificationTemplateRepository(_context);
        }

        [Fact]
        public async Task GetBySubjectAndTypeAsync_WithValidSubjectAndType_ShouldReturnTemplate()
        {
            // Arrange
            var template = new NotificationTemplate
            {
                Id = Guid.NewGuid(),
                Subject = "Test Subject",
                Type = NotificationType.Email,
                Body = "Test content",
                CreatedAt = DateTime.UtcNow
            };
            
            await _context.NotificationTemplates.AddAsync(template);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetBySubjectAndTypeAsync("Test Subject", NotificationType.Email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(template.Subject, result.Subject);
            Assert.Equal(template.Type, result.Type);
            Assert.Equal(template.Body, result.Body);
        }

        [Fact]
        public async Task GetBySubjectAndTypeAsync_WithInvalidSubject_ShouldReturnNull()
        {
            // Arrange
            var template = new NotificationTemplate
            {
                Id = Guid.NewGuid(),
                Subject = "Test Subject",
                Type = NotificationType.Email,
                Body = "Test content",
                CreatedAt = DateTime.UtcNow
            };
            
            await _context.NotificationTemplates.AddAsync(template);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetBySubjectAndTypeAsync("Invalid Subject", NotificationType.Email);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBySubjectAndTypeAsync_WithInvalidType_ShouldReturnNull()
        {
            // Arrange
            var template = new NotificationTemplate
            {
                Id = Guid.NewGuid(),
                Subject = "Test Subject",
                Type = NotificationType.Email,
                Body = "Test content",
                CreatedAt = DateTime.UtcNow
            };
            
            await _context.NotificationTemplates.AddAsync(template);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetBySubjectAndTypeAsync("Test Subject", NotificationType.WebPush);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBySubjectAndTypeAsync_WithEmptySubject_ShouldReturnNull()
        {
            // Arrange
            var template = new NotificationTemplate
            {
                Id = Guid.NewGuid(),
                Subject = "Test Subject",
                Type = NotificationType.Email,
                Body = "Test content",
                CreatedAt = DateTime.UtcNow
            };
            
            await _context.NotificationTemplates.AddAsync(template);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetBySubjectAndTypeAsync("", NotificationType.Email);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBySubjectAndTypeAsync_WithNullSubject_ShouldReturnNull()
        {
            // Arrange
            var template = new NotificationTemplate
            {
                Id = Guid.NewGuid(),
                Subject = "Test Subject",
                Type = NotificationType.Email,
                Body = "Test content",
                CreatedAt = DateTime.UtcNow
            };
            
            await _context.NotificationTemplates.AddAsync(template);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetBySubjectAndTypeAsync(null, NotificationType.Email);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBySubjectAndTypeAsync_WithMultipleTemplates_ShouldReturnCorrectTemplate()
        {
            // Arrange
            var templates = new List<NotificationTemplate>
            {
                new NotificationTemplate
                {
                    Id = Guid.NewGuid(),
                    Subject = "Subject 1",
                    Type = NotificationType.Email,
                    Body = "Content 1",
                    CreatedAt = DateTime.UtcNow
                },
                new NotificationTemplate
                {
                    Id = Guid.NewGuid(),
                    Subject = "Subject 2",
                    Type = NotificationType.Email,
                    Body = "Content 2",
                    CreatedAt = DateTime.UtcNow
                },
                new NotificationTemplate
                {
                    Id = Guid.NewGuid(),
                    Subject = "Subject 1",
                    Type = NotificationType.WebPush,
                    Body = "Content 3",
                    CreatedAt = DateTime.UtcNow
                }
            };
            
            await _context.NotificationTemplates.AddRangeAsync(templates);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetBySubjectAndTypeAsync("Subject 1", NotificationType.Email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Subject 1", result.Subject);
            Assert.Equal(NotificationType.Email, result.Type);
            Assert.Equal("Content 1", result.Body);
        }

        [Fact]
        public async Task GetBySubjectAndTypeAsync_WithAllNotificationTypes_ShouldReturnCorrectTemplate()
        {
            // Arrange
            var templates = new List<NotificationTemplate>
            {
                new NotificationTemplate
                {
                    Id = Guid.NewGuid(),
                    Subject = "Test Subject",
                    Type = NotificationType.Email,
                    Body = "Email content",
                    CreatedAt = DateTime.UtcNow
                },
                new NotificationTemplate
                {
                    Id = Guid.NewGuid(),
                    Subject = "Test Subject",
                    Type = NotificationType.WebPush,
                    Body = "WebPush content",
                    CreatedAt = DateTime.UtcNow
                }
            };
            
            await _context.NotificationTemplates.AddRangeAsync(templates);
            await _context.SaveChangesAsync();

            // Act & Assert
            var emailResult = await _repository.GetBySubjectAndTypeAsync("Test Subject", NotificationType.Email);
            var webPushResult = await _repository.GetBySubjectAndTypeAsync("Test Subject", NotificationType.WebPush);

            Assert.NotNull(emailResult);
            Assert.Equal(NotificationType.Email, emailResult.Type);
            Assert.Equal("Email content", emailResult.Body);

            Assert.NotNull(webPushResult);
            Assert.Equal(NotificationType.WebPush, webPushResult.Type);
            Assert.Equal("WebPush content", webPushResult.Body);
        }

        [Fact]
        public async Task GetBySubjectAndTypeAsync_WithCaseSensitiveSubject_ShouldReturnCorrectTemplate()
        {
            // Arrange
            var template = new NotificationTemplate
            {
                Id = Guid.NewGuid(),
                Subject = "Test Subject",
                Type = NotificationType.Email,
                Body = "Test content",
                CreatedAt = DateTime.UtcNow
            };
            
            await _context.NotificationTemplates.AddAsync(template);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetBySubjectAndTypeAsync("test subject", NotificationType.Email);

            // Assert
            Assert.Null(result); // Should be case sensitive
        }

        [Fact]
        public async Task GetBySubjectAndTypeAsync_WithSpecialCharactersInSubject_ShouldReturnCorrectTemplate()
        {
            // Arrange
            var template = new NotificationTemplate
            {
                Id = Guid.NewGuid(),
                Subject = "Test!@#$%^&*()Subject",
                Type = NotificationType.Email,
                Body = "Test content",
                CreatedAt = DateTime.UtcNow
            };
            
            await _context.NotificationTemplates.AddAsync(template);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetBySubjectAndTypeAsync("Test!@#$%^&*()Subject", NotificationType.Email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test!@#$%^&*()Subject", result.Subject);
        }

        [Fact]
        public async Task GetBySubjectAndTypeAsync_WithUnicodeCharactersInSubject_ShouldReturnCorrectTemplate()
        {
            // Arrange
            var template = new NotificationTemplate
            {
                Id = Guid.NewGuid(),
                Subject = "Тестовая тема",
                Type = NotificationType.Email,
                Body = "Test content",
                CreatedAt = DateTime.UtcNow
            };
            
            await _context.NotificationTemplates.AddAsync(template);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetBySubjectAndTypeAsync("Тестовая тема", NotificationType.Email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Тестовая тема", result.Subject);
        }

        [Fact]
        public async Task GetBySubjectAndTypeAsync_WithLongSubject_ShouldReturnCorrectTemplate()
        {
            // Arrange
            var longSubject = new string('a', 1000);
            var template = new NotificationTemplate
            {
                Id = Guid.NewGuid(),
                Subject = longSubject,
                Type = NotificationType.Email,
                Body = "Test content",
                CreatedAt = DateTime.UtcNow
            };
            
            await _context.NotificationTemplates.AddAsync(template);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetBySubjectAndTypeAsync(longSubject, NotificationType.Email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(longSubject, result.Subject);
        }

        [Fact]
        public async Task GetBySubjectAndTypeAsync_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange
            var template = new NotificationTemplate
            {
                Id = Guid.NewGuid(),
                Subject = "Test Subject",
                Type = NotificationType.Email,
                Body = "Test content",
                CreatedAt = DateTime.UtcNow
            };
            
            await _context.NotificationTemplates.AddAsync(template);
            await _context.SaveChangesAsync();

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _repository.GetBySubjectAndTypeAsync("Test Subject", NotificationType.Email, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GetBySubjectAndTypeAsync_WithNoTemplates_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetBySubjectAndTypeAsync("Test Subject", NotificationType.Email);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBySubjectAndTypeAsync_WithWhitespaceSubject_ShouldReturnNull()
        {
            // Arrange
            var template = new NotificationTemplate
            {
                Id = Guid.NewGuid(),
                Subject = "Test Subject",
                Type = NotificationType.Email,
                Body = "Test content",
                CreatedAt = DateTime.UtcNow
            };
            
            await _context.NotificationTemplates.AddAsync(template);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetBySubjectAndTypeAsync("   ", NotificationType.Email);

            // Assert
            Assert.Null(result);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 