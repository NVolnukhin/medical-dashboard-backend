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

// Объединение ocelot.*.{ENV}.json файлов
var ocelotFiles = Directory.GetFiles(builder.Environment.ContentRootPath, $"ocelot.*.{builder.Environment.EnvironmentName}.json", SearchOption.TopDirectoryOnly);
var mergedConfig = new JObject();

foreach (var file in ocelotFiles)
{
    var json = JObject.Parse(File.ReadAllText(file));
    foreach (var prop in json.Properties())
    {
        if (mergedConfig[prop.Name] == null)
        {
            mergedConfig[prop.Name] = prop.Value;
        }
        else if (mergedConfig[prop.Name] is JArray existingArray && prop.Value is JArray newArray)
        {
            foreach (var item in newArray)
            {
                existingArray.Add(item);
            }
        }
        else
        {
            mergedConfig[prop.Name] = prop.Value;
        }
    }
}

var mergedFilePath = Path.Combine(builder.Environment.ContentRootPath, "ocelot.generated.json");
File.WriteAllText(mergedFilePath, mergedConfig.ToString());

builder.Configuration.AddJsonFile(mergedFilePath, optional: false, reloadOnChange: true);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
}

builder.Configuration.AddEnvironmentVariables();

// Сервисы
builder.Services.AddControllers();
builder.Services.AddSwaggerForOcelot(builder.Configuration);
builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

// Добавляем поддержку WebSocket
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
        builder.WithOrigins(
                "http://localhost:3000",  // React/Next.js
                "http://localhost:4200",  // Angular
                "http://localhost:8080",  // Vue.js
                "http://localhost:5000",  // Другие dev серверы
                "http://localhost:5173",  // Vite
                "http://localhost:3001",  // Дополнительные порты
                "http://localhost:3002"
            )
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials()); // Важно для WebSocket
});

// Приложение
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

// Добавляем SignalR прокси middleware перед Ocelot
app.UseWebSockets();

app.UseMiddleware<SignalRProxyMiddleware>();

app.UseSwagger();
app.UseSwaggerForOcelotUI(opt =>
{
    opt.PathToSwaggerGenerator = "/swagger/docs";
});

var option = new RewriteOptions();
app.UseRewriter(option);

await app.UseOcelot();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();