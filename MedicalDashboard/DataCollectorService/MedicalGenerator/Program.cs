using Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Models;
using Processors;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.Configure<MetricGenerationConfig>(
    builder.Configuration.GetSection("MetricGeneration"));

builder.Services.AddSingleton<IGeneratorService, GeneratorService>();

builder.Services.AddSingleton<MetricGenerationConfig>(sp =>
    sp.GetRequiredService<IOptions<MetricGenerationConfig>>().Value);

builder.Services.AddSingleton<IMetricProcessor, HeartRateProcessor>();
builder.Services.AddSingleton<IMetricProcessor, SaturationProcessor>();
builder.Services.AddSingleton<IMetricProcessor, TemperatureProcessor>();
builder.Services.AddSingleton<IMetricProcessor, RespirationProcessor>();
builder.Services.AddSingleton<IMetricProcessor, PressureProcessor>();
builder.Services.AddSingleton<IMetricProcessor, HemoglobinProcessor>();
builder.Services.AddSingleton<IMetricProcessor, WeightProcessor>();
builder.Services.AddSingleton<IMetricProcessor, BMIProcessor>();
builder.Services.AddSingleton<IMetricProcessor, CholesterolProcessor>();

builder.Services.AddHostedService<WorkerService>();

builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

var host = builder.Build();
host.Run();