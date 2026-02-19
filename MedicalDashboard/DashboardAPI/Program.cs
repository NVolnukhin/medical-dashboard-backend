using System.Text;
using Confluent.Kafka;
using DashboardAPI.Config;
using DashboardAPI.Data;
using DashboardAPI.Hubs;
using DashboardAPI.Repositories.Alert;
using DashboardAPI.Repositories.Metric;
using DashboardAPI.Repositories.Patient;
using DashboardAPI.Repositories.Device;
using DashboardAPI.Services;
using DashboardAPI.Services.Kafka;
using DashboardAPI.Services.Kafka.Consumer;
using DashboardAPI.Services.Kafka.Retry;
using DashboardAPI.Services.Metric;
using DashboardAPI.Services.Patient;
using DashboardAPI.Services.SignalR;
using DashboardAPI.Services.Device;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Middleware;

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы в контейнер
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddSignalR();

// Конфигурация
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));
builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("jwt"));

// База данных
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DashboardDbContext>(options =>
    options.UseNpgsql(connectionString));

// Репозитории
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IMetricRepository, MetricRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IDeviceRepository, DashboardAPI.Repositories.Device.DeviceRepository>();

// Сервисы
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IMetricService, MetricService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<ISignalRService, SignalRService>();
builder.Services.AddScoped<IDeviceService, DashboardAPI.Services.Device.DeviceService>();
builder.Services.AddSingleton<IKafkaRetryService, KafkaRetryService>();

// Kafka конфигурация
var kafkaSettings = builder.Configuration.GetSection("Kafka").Get<KafkaSettings>();
if (kafkaSettings == null)
{
    throw new InvalidOperationException("Kafka settings are missing");
}

// Kafka консьюмеры
builder.Services.AddKeyedSingleton<IConsumer<string, string>>("metrics-consumer", (_, _) =>
{
    var config = new ConsumerConfig
    {
        BootstrapServers = kafkaSettings.BootstrapServers,
        GroupId = $"{kafkaSettings.GroupId}-metrics",
        AutoOffsetReset = Enum.Parse<AutoOffsetReset>(kafkaSettings.AutoOffsetReset),
        ClientId = "dashboard-metrics-consumer"
    };
    return new ConsumerBuilder<string, string>(config).Build();
});

// Kafka сервисы
builder.Services.AddHostedService<KafkaInitializationService>();
builder.Services.AddHostedService<KafkaConsumerService>(sp => 
    new KafkaConsumerService(
        sp.GetRequiredKeyedService<IConsumer<string, string>>("metrics-consumer"),
        sp.GetRequiredService<ILogger<KafkaConsumerService>>(),
        sp.GetRequiredService<IServiceScopeFactory>(),
        sp.GetRequiredService<IKafkaRetryService>(),
        "md-metrics"
    ));

// JWT аутентификация
var jwtSettings = builder.Configuration.GetSection("jwt").Get<JwtConfig>();
if (jwtSettings == null)
{
    throw new InvalidOperationException("JWT settings are missing");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };
        
        // Настройка для SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Dashboard API", 
        Version = "v1",
        Description = "API для медицинской панели управления"
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description = "Enter JWT Bearer token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

// CORS
builder.Services.AddCors(options => 
{
    options.AddPolicy("AllowSpecific", policy =>
    {
        policy.WithOrigins(
                "http://localhost:7168",  // Gateway
                "http://localhost:3000",  // React/Next.js
                "http://localhost:4200",  // Angular
                "http://localhost:8080",  // Vue.js
                "http://localhost:5000"   // Другие dev серверы
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Важно для SignalR
    });
});

var app = builder.Build();

// Настройка конвейера HTTP-запросов
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dashboard API V1");
    });
}
else
{
    app.UseExceptionHandler("/Error");
    // Отключаем HTTPS редирект для WebSocket соединений
    // app.UseHsts();
}

// Добавляем поддержку WebSocket для SignalR В САМОМ НАЧАЛЕ
app.UseWebSockets();

app.UseCors("AllowSpecific");

// Добавляем middleware для логирования WebSocket запросов
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Запрос: {Method} {Path} {Protocol}", 
        context.Request.Method, 
        context.Request.Path, 
        context.Request.Protocol);
    
    // Логируем все заголовки
    logger.LogInformation("Заголовки запроса:");
    foreach (var header in context.Request.Headers)
    {
        logger.LogInformation("  {Key}: {Value}", header.Key, header.Value);
    }
    
    if (context.WebSockets.IsWebSocketRequest)
    {
        logger.LogInformation("Это WebSocket запрос!");
    }
    else
    {
        logger.LogInformation("Это НЕ WebSocket запрос");
    }
    
    await next();
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MetricsHub>("/hubs/metrics");

// Миграция базы данных
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
    context.Database.Migrate();
}

app.Run();