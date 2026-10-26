using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProductionClient;

// Вся работа с HTTP API собрана в одном классе.
// О результате каждого запроса класс сообщает через события — сам он ничего в консоль не пишет
public class ApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // События на основе собственных делегатов
    public event RequestCompletedHandler? RequestCompleted;
    public event RequestFailedHandler? RequestFailed;

    public ApiService(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(10) };
    }

    // Общий метод отправки запроса: замеряет время и вызывает события.
    // Func<HttpClient, Task<HttpResponseMessage>> — сама операция, переданная как параметр
    private async Task<HttpResponseMessage?> SendAsync(string method, string endpoint,
        Func<HttpClient, Task<HttpResponseMessage>> operation)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            HttpResponseMessage response = await operation(_http);
            sw.Stop();
            RequestCompleted?.Invoke(method, endpoint, (int)response.StatusCode, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
        {
            RequestFailed?.Invoke(method, endpoint, ex);
            return null;
        }
    }

    public async Task<List<T>> GetAllAsync<T>(string endpoint)
    {
        var response = await SendAsync("GET", endpoint, http => http.GetAsync(endpoint));
        if (response == null || !response.IsSuccessStatusCode) return new List<T>();
        return await response.Content.ReadFromJsonAsync<List<T>>(Json) ?? new List<T>();
    }

    public async Task<T?> GetByIdAsync<T>(string endpoint, int id)
    {
        string url = $"{endpoint}/{id}";
        var response = await SendAsync("GET", url, http => http.GetAsync(url));
        if (response == null || !response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<T>(Json);
    }

    public async Task<T?> CreateAsync<T>(string endpoint, T item)
    {
        var response = await SendAsync("POST", endpoint, http => http.PostAsJsonAsync(endpoint, item, Json));
        if (response == null || !response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<T>(Json);
    }

    public async Task<bool> UpdateAsync<T>(string endpoint, int id, T item)
    {
        string url = $"{endpoint}/{id}";
        var response = await SendAsync("PUT", url, http => http.PutAsJsonAsync(url, item, Json));
        return response?.IsSuccessStatusCode == true;
    }

    public async Task<bool> DeleteAsync(string endpoint, int id)
    {
        string url = $"{endpoint}/{id}";
        var response = await SendAsync("DELETE", url, http => http.DeleteAsync(url));
        return response?.IsSuccessStatusCode == true;
    }
}
