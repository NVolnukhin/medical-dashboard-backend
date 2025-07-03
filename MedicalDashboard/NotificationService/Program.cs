using System.Reflection;
using Confluent.Kafka;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using NotificationService.Config;
using NotificationService.Data;
using NotificationService.Data.Models;
using NotificationService.Email.Sender;
using NotificationService.Extensions.Logging;
using NotificationService.Handlers;
using NotificationService.Services.Consumer;
using NotificationService.Services.Notification;
using NotificationService.Services.Queue;
using NotificationService.Services.Retry;
using NotificationService.Handlers;
using NotificationService.Interfaces;
using NotificationService.Repositories.Template;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Information);

// Добавляем Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Notification Service API",
        Version = "v1",
        Description = "API для сервиса уведомлений",
        Contact = new OpenApiContact
        {
            Name = "Development Team",
            Email = "dev@example.com"
        }
    });

    // Добавляем поддержку JWT авторизации в Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    
    // XML документация сваггера
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddControllers();

// cfg
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));
builder.Services.Configure<RetrySettings>(builder.Configuration.GetSection("RetrySettings"));

// Регистрация настроек как синглтоны
builder.Services.AddSingleton<KafkaSettings>(sp => 
    sp.GetRequiredService<IOptions<KafkaSettings>>().Value);

// Kafka
var kafkaSettings = builder.Configuration.GetSection("Kafka").Get<KafkaSettings>();
if (kafkaSettings == null)
{
    throw new InvalidOperationException("Kafka settings are missing");
}

// Регистрация продюсера Кафки
builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = kafkaSettings.BootstrapServers
    };
    return new ProducerBuilder<string, string>(config).Build();
});

// Регистрация консьюмера Кафки
builder.Services.AddKeyedSingleton<IConsumer<string, string>>("main-consumer", (sp, key) =>
{
    var config = new ConsumerConfig
    {
        BootstrapServers = kafkaSettings.BootstrapServers,
        GroupId = kafkaSettings.GroupId,
        AutoOffsetReset = Enum.Parse<AutoOffsetReset>(kafkaSettings.AutoOffsetReset),
        ClientId = "main-consumer"
    };
    return new ConsumerBuilder<string, string>(config).Build();
});


// Добавление БД 
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрация репо
builder.Services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();

// Регистрация сервисов
builder.Services.AddSingleton<INotificationSender, EmailNotificationSender>();
builder.Services.AddSingleton<IRetryService, RetryService>();
builder.Services.AddScoped<INotificationService, NotificationService.Services.Notification.NotificationService>();
builder.Services.AddScoped<IMessageHandler<NotificationRequest>, KafkaNotificationHandler>();

// Регистрация сервисов очереди
builder.Services.Configure<QueueSettings>(builder.Configuration.GetSection("QueueSettings"));
builder.Services.AddSingleton<IPriorityNotificationQueue, PriorityNotificationQueue>();
builder.Services.AddHostedService<NotificationQueueProcessor>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Регистрация сервисов
builder.Services.AddHostedService<KafkaInitializationService>();
builder.Services.AddHostedService<KafkaConsumerService<NotificationRequest>>();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Добавляем глобальный обработчик исключений
AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
{
    var exception = (Exception)args.ExceptionObject;
    logger.LogFailure($"Unhandled exception: {exception.Message}", exception);
    logger.LogInfo($"Exception type: {exception.GetType().FullName}");
    logger.LogInfo($"Stack trace: {exception.StackTrace}");
};

TaskScheduler.UnobservedTaskException += (sender, args) =>
{
    logger.LogFailure($"Незамеченное исключение задачи: {args.Exception.Message}", args.Exception);
    foreach (var exception in args.Exception.InnerExceptions)
    {
        logger.LogInfo($"Inner exception type: {exception.GetType().FullName}");
        logger.LogInfo($"Inner exception message: {exception.Message}");
        logger.LogInfo($"Inner exception stack trace: {exception.StackTrace}");
    }
};

// Проверка создания сервисов
var hostedServices = app.Services.GetServices<IHostedService>();
logger.LogInfo($"Зарегистрированные hosted services: {string.Join(", ", hostedServices.Select(s => s.GetType().Name))}");

var kafkaConsumer = hostedServices.FirstOrDefault(s => s is KafkaConsumerService<NotificationRequest>);
logger.LogInfo($"KafkaConsumerService найден: {kafkaConsumer != null}");

// Логируем информацию о зарегистрированных контроллерах
var endpointDataSource = app.Services.GetRequiredService<EndpointDataSource>();
var endpoints = endpointDataSource.Endpoints;

var registeredControllers = endpoints
    .OfType<RouteEndpoint>()
    .Select(e => e.RoutePattern.RawText?.Split('/').FirstOrDefault())
    .Where(name => !string.IsNullOrWhiteSpace(name))
    .Distinct()
    .ToList();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Добавляем CORS
app.UseCors(corsPolicyBuilder => corsPolicyBuilder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// Настраиваем middleware 
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification Service API V1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Notification Service API Documentation";
    c.DefaultModelsExpandDepth(-1);
    c.DisplayRequestDuration();
    c.EnableDeepLinking();
    c.EnableFilter();
    c.ShowExtensions();
});



// Добавляем обработчик ошибок
app.Map("/error", async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    logger.LogError(exception, "Unhandled exception occurred");
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new { error = "Ошибка обработки запроса." });
});

// Проверка, что БД создана
try 
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        logger.LogInfo("Проверка, что БД создана...");
        db.Database.EnsureCreated();
        logger.LogSuccess("БД инициализирована");
    }
}
catch (Exception ex)
{
    logger.LogFailure("Ошибка инициализации БД", ex);
    throw;
}

logger.LogInfo("Запуск приложения...");

await app.RunAsync(); 