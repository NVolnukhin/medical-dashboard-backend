using DataCollectorService.Processors;
using DataCollectorService.DCSAppContext;
using DataCollectorService.Services;
using DataCollectorService.Observerer;
using DataCollectorService.Worker;
using DataCollectorService.Kafka;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.Configure<MetricGenerationConfig>(
    builder.Configuration.GetSection("MetricGeneration"));

builder.Services.AddDbContext<DataCollectorDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<KafkaConfig>(
    builder.Configuration.GetSection("Kafka"));

builder.Services.AddSingleton<IGeneratorService, GeneratorService>();
builder.Services.AddSingleton<IKafkaService, KafkaService>();

builder.Services.AddSingleton<MetricGenerationConfig>(sp =>
    sp.GetRequiredService<IOptions<MetricGenerationConfig>>().Value);

builder.Services.AddSingleton<IObserver, PulseProcessor>();
builder.Services.AddSingleton<IObserver, SaturationProcessor>();
builder.Services.AddSingleton<IObserver, TemperatureProcessor>();
builder.Services.AddSingleton<IObserver, RespirationProcessor>();
builder.Services.AddSingleton<IObserver, SystolicPressureProcessor>();
builder.Services.AddSingleton<IObserver, DiastolicPressureProcessor>();
builder.Services.AddSingleton<IObserver, HemoglobinProcessor>();
builder.Services.AddSingleton<IObserver, WeightProcessor>();
builder.Services.AddSingleton<IObserver, BMIProcessor>();
builder.Services.AddSingleton<IObserver, CholesterolProcessor>();

builder.Services.AddHostedService<WorkerService>();

builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

var host = builder.Build();
host.Run();