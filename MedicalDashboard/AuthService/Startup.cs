using System.Text;
using AuthService.Kafka;
using AuthService.Models;
using AuthService.Repository.PasswordRecovery;
using AuthService.Repository.User;
using AuthService.Services.Identity;
using AuthService.Services.Jwt;
using AuthService.Services.Password;
using AuthService.Services.PasswordRecovery;
using AuthService.Services.RecoveryToken;
using AuthService.Services.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Middleware;

namespace AuthService
{
    public class Startup(IConfiguration configuration)
    {
        private IConfiguration Configuration { get; } = configuration;

        // Этот метод вызывается средой выполнения. Используйте этот метод для добавления служб в КОНТЕЙНЕР.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // Подключаем сервис Шифровальщик
            services.AddTransient<IEncryptor, Encryptor>();

            // Подключаем репозитории
            services.AddTransient<IUserRepository, UserRepository>();

            // Подключаем сервис авторизации
            services.AddTransient<IIdentityService, IdentityService>();
            
            // Подключаем JWT сервис
            services.AddTransient<IJwtBuilder, JwtBuilder>();

            // Подключаем сервис шифрования пароля
            services.AddTransient<IPasswordService, PasswordService>();

            // Подключаем сервис генерации одноразового пароля
            services.AddTransient<IOneTimePasswordGenerator, OneTimePasswordGenerator>();

            //Подключаем генератор токена для восстановления пароля
            services.AddTransient<ITokenGenerator, TokenGenerator>(); 
            
            //Подключаем сервис пользователя (восстановление пароля)
            services.AddTransient<IUserService, UserService>();
            
            //Подключаем репо и сервис восстановления пароля
            services.AddTransient<IPasswordRecoveryTokenRepository, PasswordRecoveryTokenRepository>();
            services.AddTransient<IPasswordRecoveryService, PasswordRecoveryService>();
            
            // Конфигурация натсроек JWT 
            services.Configure<JwtConfig>(Configuration.GetSection("jwt"));
            
            // Чтение строки подключения для Kafka из конфигурации
            services.Configure<KafkaConfig>(
                configuration.GetSection("Kafka"));
            
            // Регистрация KafkaProducerService с использованием строки подключения как Transient
            services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
            
            // Чтение строки подключения для AuthorizationAppContext из конфигурации
            var authorizationConnectionString = Configuration.GetConnectionString("DefaultConnection");

            // Регистрация AuthorizationAppContext с использованием строки подключения как Transient
            services.AddDbContext<AuthorizationAppContext>(options =>
                options.UseNpgsql(authorizationConnectionString));

            // добавление сваггера
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthMicroservice API", Version = "v1" });

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
                    {securityScheme, Array.Empty<string>()}
                });
            });
            
            // Натсройки аутентификации JWT
            var jwtSettings = Configuration.GetSection("jwt").Get<JwtConfig>();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                        ClockSkew = TimeSpan.Zero // Убираем запас времени для истечения токена
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                            logger.LogError($"JWT validation failed: {context.Exception.Message}");
                            return Task.CompletedTask;
                        }
                    };
                });
            
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddConsole();
                loggingBuilder.SetMinimumLevel(LogLevel.Debug);
            });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });
        }

        // Этот метод вызывается средой выполнения. Используйте этот метод для настройки КОНВЕЙЕРА HTTP-запросов.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "User API V1");
                });
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            var option = new RewriteOptions();
            option.AddRedirect("^$", "swagger");
            app.UseRewriter(option);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
