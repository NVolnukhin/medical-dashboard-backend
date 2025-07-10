using DataCollectorService.DCSAppContext;
using DataCollectorService.Models;
using DataCollectorService.Observerer;
using DataCollectorService.Processors;
using DataCollectorService.Services;
using DataCollectorService.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Org.BouncyCastle.Utilities.Encoders;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.MedicalDashboard.DCSTests.Worker
{
    public class WorkerServiceTests : IDisposable
    {
        private readonly Mock<IGeneratorService> _generatorMock;
        private readonly Mock<ILogger<WorkerService>> _loggerMock;
        private readonly Mock<DataCollectorDbContext> _dbContextMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly List<Mock<IObserver>> _observerMocks;
        private readonly WorkerService _workerService;
        private readonly MetricGenerationConfig _config;

        public WorkerServiceTests()
        {
            _generatorMock = new Mock<IGeneratorService>();
            _loggerMock = new Mock<ILogger<WorkerService>>();
            _dbContextMock = new Mock<DataCollectorDbContext>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _observerMocks = new List<Mock<IObserver>>();

            _config = new MetricGenerationConfig();
            var options = Options.Create(_config);

            SetupMocks();

            _workerService = new WorkerService(
                _generatorMock.Object,
                _loggerMock.Object,
                options,
                _serviceProviderMock.Object,
                _dbContextMock.Object,
                _observerMocks.Select(m => m.Object).ToList());
        }

        private void SetupMocks()
        {
            var patientsData = new List<PatientDto>
            {
                new PatientDto
                {
                    PatientId = Guid.NewGuid(),
                    BirthDate = DateTime.Now.AddYears(-30),
                    Height = 175,
                    FirstName = "Имя",
                    MiddleName = "Отчество",
                    LastName = "Фамилия",
                    Sex = 'M',
                    Ward = 1
                }
            };

            var metricsData = new List<MetricDto>
            {
                new MetricDto { Type = "Weight", Value = 70.0 }
            };

            var patientsMockSet = MockDbSet(patientsData);
            var metricsMockSet = MockDbSet(metricsData);

            _dbContextMock.Setup(d => d.Patients).Returns(patientsMockSet.Object);
            _dbContextMock.Setup(d => d.Metrics).Returns(metricsMockSet.Object);

            var metricProcessors = new List<IMetricProcessor>();
            _serviceProviderMock
                .Setup(x => x.GetServices(typeof(IMetricProcessor)))
                .Returns(metricProcessors);

            for (int i = 0; i < 2; i++)
            {
                var observerMock = new Mock<IObserver>();
                _observerMocks.Add(observerMock);
            }
        }

        private static Mock<DbSet<T>> MockDbSet<T>(List<T> elements) where T : class
        {
            var queryable = elements.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
            return mockSet;
        }

        public void Dispose()
        {
            _workerService.Dispose();
        }

        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            Assert.NotNull(_workerService);
            Assert.Equal(2, _observerMocks.Count);
            Assert.Single(GetPatients());
        }

        [Fact]
        public void Attach_AddsNewObserver()
        {
            var newObserverMock = new Mock<IObserver>();
            var initialCount = _observerMocks.Count;

            _workerService.Attach(newObserverMock.Object);

            Assert.Equal(initialCount + 1, GetObserversCount());
        }

        [Fact]
        public void Detach_RemovesObserver()
        {
            var observerToRemove = _observerMocks.First().Object;
            var initialCount = _observerMocks.Count;

            _workerService.Detach(observerToRemove);

            Assert.Equal(initialCount - 1, GetObserversCount());
        }

        [Fact]
        public async Task ExecuteAsync_NotifiesAllObservers()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(100); // Остановить через 100 мс

            await _workerService.StartAsync(cts.Token);
            await Task.Delay(50); // Даем время на выполнение

            foreach (var observerMock in _observerMocks)
            {
                observerMock.Verify(o => o.Update(It.IsAny<List<Patient>>()), Times.AtLeastOnce());
            }
        }

        [Fact]
        public async Task ExecuteAsync_HandlesExceptions()
        {
            var faultyObserverMock = new Mock<IObserver>();
            faultyObserverMock.Setup(o => o.Update(It.IsAny<List<Patient>>()))
                .Throws(new Exception("Test error"));
            _workerService.Attach(faultyObserverMock.Object);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(100);

            await _workerService.StartAsync(cts.Token);
            await Task.Delay(50);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Ошибка в цикле генерации")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void InitPatients_CreatesCorrectPatientModels()
        {
            var patientDto = _dbContextMock.Object.Patients.First();

            var patients =  GetPatients();
            var patient = patients.First();

            Assert.Equal(patientDto.PatientId, patient.Id);
            Assert.Equal(patientDto.Height, patient.Height);
            Assert.Equal($"{patientDto.FirstName} {patientDto.MiddleName} {patientDto.LastName}".Trim(), patient.Name);
            Assert.Equal(30, patient.Age); // Проверка возраста
            Assert.Equal(70.0, patient.BaseWeight);
        }

        [Fact]
        public void CalculateAge_ReturnsCorrectAge()
        {
            var birthDate = DateTime.Now.AddYears(-25).AddMonths(-6);
            var age = WorkerService.CalculateAge(birthDate);
            Assert.Equal(25, age);
        }

        // Вспомогательные методы для тестирования приватных членов
        private static class PrivateAccess
        {
            public static List<Patient> GetPatients(WorkerService service) =>
                (List<Patient>)typeof(WorkerService)
                    .GetField("_patients", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(service);

            public static int GetObserversCount(WorkerService service) =>
                ((List<IObserver>)typeof(WorkerService)
                    .GetField("_observers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(service)).Count;
        }

        private List<Patient> GetPatients() => PrivateAccess.GetPatients(_workerService);
        private int GetObserversCount() => PrivateAccess.GetObserversCount(_workerService);
    }
}