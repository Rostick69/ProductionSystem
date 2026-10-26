using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Prometheus;

namespace ProductionSystem.Services;

// Обёртка над Redis (IDistributedCache): «взять из кэша или загрузить из базы и положить в кэш».
// Попадания и промахи считаются в метрике Prometheus — это видно на дашборде Grafana
public class CacheService
{
    // Время жизни записи в кэше (TTL)
    public static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    private static readonly Counter CacheRequests = Metrics.CreateCounter(
        "productionsystem_cache_requests_total",
        "Обращения к кэшу Redis",
        new CounterConfiguration { LabelNames = new[] { "result" } });

    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> loadFromDb) where T : class
    {
        try
        {
            string? cached = await _cache.GetStringAsync(key);
            if (cached != null)
            {
                CacheRequests.WithLabels("hit").Inc();
                return JsonSerializer.Deserialize<T>(cached);
            }
        }
        catch (Exception ex)
        {
            // Если Redis недоступен, API продолжает работать напрямую с базой
            _logger.LogWarning("Redis недоступен: {Message}", ex.Message);
        }

        CacheRequests.WithLabels("miss").Inc();
        T? value = await loadFromDb();
        if (value != null)
        {
            try
            {
                await _cache.SetStringAsync(key, JsonSerializer.Serialize(value),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Не удалось записать в Redis: {Message}", ex.Message);
            }
        }
        return value;
    }

    // Инвалидация: удаляем ключи после изменения данных
    public async Task RemoveAsync(params string[] keys)
    {
        foreach (var key in keys)
        {
            try { await _cache.RemoveAsync(key); }
            catch (Exception ex) { _logger.LogWarning("Не удалось удалить ключ {Key} из Redis: {Message}", key, ex.Message); }
        }
    }
}
