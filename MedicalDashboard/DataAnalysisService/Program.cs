using Confluent.Kafka;
using DataAnalysisService.Config;
using DataAnalysisService.Data;
using DataAnalysisService.Services.Alert;
using DataAnalysisService.Services.Analysis;
using DataAnalysisService.Services.Kafka;
using DataAnalysisService.Services.Kafka.Consumer;
using DataAnalysisService.Services.Kafka.Producer;
using DataAnalysisService.Services.Patient;
using DataAnalysisService.Services.Redis;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.MetricLimits;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("KafkaSettings"));
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("RedisSettings"));
builder.Services.Configure<AnalysisSettings>(builder.Configuration.GetSection("AnalysisSettings"));
builder.Services.Configure<MetricsConfig>(builder.Configuration.GetSection("MetricsConfig"));

// База данных
builder.Services.AddDbContext<DataAnalysisDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var redisSettings = builder.Configuration.GetSection("RedisSettings").Get<RedisSettings>();
    return ConnectionMultiplexer.Connect(redisSettings!.ConnectionString);
});

// Сервисы
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IDataAnalysisService, DataAnalysisService.Services.Analysis.DataAnalysisService>();

// Kafka
builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
builder.Services.AddSingleton<IKafkaConsumerService, KafkaConsumerService>();

// Hosted Services
builder.Services.AddHostedService<KafkaInitializationService>();
builder.Services.AddHostedService<KafkaConsumerHostedService>();

var app = builder.Build();

// Миграции базы данных
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DataAnalysisDbContext>();
    context.Database.Migrate();
}

app.Run();