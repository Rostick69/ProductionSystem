using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Services;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Строка подключения берётся из переменной окружения CONNECTION_STRING (задаётся в docker-compose.yml).
// Для локального запуска без Docker используется appsettings.json
string connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Не задана строка подключения к базе данных");

string redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379";

// PostgreSQL через Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// Redis как распределённый кэш
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "ProductionSystem_";
});
builder.Services.AddScoped<CacheService>();

builder.Services.AddControllers();

// Swagger UI с описаниями эндпоинтов из XML-комментариев
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ProductionSystem API", Version = "v1",
        Description = "Система управления производственным предприятием: изделия, материалы, заказы, смены" });
    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) c.IncludeXmlComments(xml);
});

var app = builder.Build();

// Миграции и тестовые данные применяются автоматически при запуске
await DbSeeder.MigrateAndSeedAsync(app.Services, app.Logger);

app.UseSwagger();
app.UseSwaggerUI();

// Метрики HTTP-запросов для Prometheus и эндпоинт /metrics
app.UseHttpMetrics();
app.MapMetrics();

app.MapControllers();

// Корень сайта перенаправляем на Swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

// Нужно для интеграционных тестов (WebApplicationFactory)
public partial class Program { }
