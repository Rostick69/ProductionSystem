using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Подключаем базу данных PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Подключаем Redis кэш
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "redis:6379";
    options.InstanceName = "ProductionSystem_";
});

// Добавляем контроллеры
builder.Services.AddControllers();

// Swagger для тестирования API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Включаем Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Включаем сбор метрик для Prometheus
app.UseHttpMetrics();
app.MapMetrics();

app.UseAuthorization();

app.MapControllers();

app.Run();