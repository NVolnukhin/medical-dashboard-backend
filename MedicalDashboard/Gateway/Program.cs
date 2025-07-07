using Microsoft.AspNetCore.Rewrite;
using Middleware;
using Middleёёware;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Newtonsoft.Json.Linq;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

var ocelotFiles = Directory.GetFiles(builder.Environment.ContentRootPath, $"ocelot.*.{builder.Environment.EnvironmentName}.json");
var mergedConfig = new JObject();

foreach (var file in ocelotFiles)
{
    var json = JObject.Parse(File.ReadAllText(file));
    foreach (var prop in json.Properties())
    {
        if (mergedConfig[prop.Name] == null)
            mergedConfig[prop.Name] = prop.Value;
        else if (mergedConfig[prop.Name] is JArray existingArray && prop.Value is JArray newArray)
            foreach (var item in newArray)
                existingArray.Add(item);
        else
            mergedConfig[prop.Name] = prop.Value;
    }
}

var mergedFilePath = Path.Combine(builder.Environment.ContentRootPath, "ocelot.generated.json");
File.WriteAllText(mergedFilePath, mergedConfig.ToString());
builder.Configuration.AddJsonFile(mergedFilePath, optional: false, reloadOnChange: true);

if (builder.Environment.IsDevelopment())
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Configuration.AddEnvironmentVariables();

// Сервисы
builder.Services.AddControllers();
builder.Services.AddSwaggerForOcelot(builder.Configuration);
builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:4200",
                "http://localhost:8080",
                "http://localhost:5000",
                "http://localhost:5173",
                "http://localhost:3001",
                "http://localhost:3002")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// ===== CORS middleware для всех запросов (до next)
app.Use(async (context, next) =>
{
    var origin = context.Request.Headers["Origin"].FirstOrDefault();
    if (!string.IsNullOrEmpty(origin))
    {
        context.Response.Headers["Access-Control-Allow-Origin"] = origin;
        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
        context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
    }

    if (context.Request.Method == HttpMethods.Options)
    {
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("OK");
        return; // НЕ вызываем next для preflight
    }

    await next();
});

// ===== Обычный CORS policy (для fallback / WebSocket / SignalR)
app.UseCors("CorsPolicy");

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseWebSockets();
app.UseMiddleware<SignalRProxyMiddleware>();
app.UseMiddleware<NotificationSignalRProxyMiddleware>();

app.UseSwagger();
app.UseSwaggerForOcelotUI(opt => opt.PathToSwaggerGenerator = "/swagger/docs");

var option = new RewriteOptions();
app.UseRewriter(option);

await app.UseOcelot();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
