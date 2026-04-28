using System.Text;
using System.Text.Json;

// Адрес нашего API
const string baseUrl = "http://localhost:8080/api";
var client = new HttpClient();

// Логгер — просто выводит сообщение в консоль
LogDelegate consoleLogger = msg => Console.WriteLine($"[LOG] {msg}");

// Второй логгер — выводит с временной меткой
LogDelegate timeLogger = msg => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");

// Многоадресный делегат — вызывает оба логгера сразу
LogDelegate multiLogger = consoleLogger;
multiLogger += timeLogger;

// Демонстрация -= (отключаем второй логгер)
multiLogger += timeLogger;
multiLogger -= timeLogger;

// Делегат для обработки результата операции
OperationDelegate operationHandler = (operation, result) =>
    Console.WriteLine($"Операция: {operation} | Результат: {result}");

// Action<T> — действие которое ничего не возвращает
Action<string> logAction = msg => multiLogger(msg);

// Func<T, TResult> — функция которая возвращает результат
Func<object, string> serialize = obj => JsonSerializer.Serialize(obj);

// Вспомогательная функция для POST/PUT запросов
async Task<string> SendJson(HttpMethod method, string url, object data)
{
    var json = serialize(data);
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    var request = new HttpRequestMessage(method, url) { Content = content };
    var response = await client.SendAsync(request);
    return await response.Content.ReadAsStringAsync();
}

Console.WriteLine("=== Клиент системы управления производством ===\n");

// 1. GET — получить все изделия
logAction("Получаем список изделий...");
var response1 = await client.GetAsync($"{baseUrl}/products");
var result1 = await response1.Content.ReadAsStringAsync();
operationHandler("GET /products", response1.StatusCode.ToString());
Console.WriteLine(result1 + "\n");

// 2. POST — создать новое изделие
logAction("Создаём новое изделие...");
var newProduct = new { Name = "Болт М8", Description = "Стальной болт", Price = 5.99 };
var result2 = await SendJson(HttpMethod.Post, $"{baseUrl}/products", newProduct);
operationHandler("POST /products", result2);
Console.WriteLine();

// 3. GET — получить изделие по ID
logAction("Получаем изделие по ID...");
var response3 = await client.GetAsync($"{baseUrl}/products/1");
var result3 = await response3.Content.ReadAsStringAsync();
operationHandler("GET /products/1", response3.StatusCode.ToString());
Console.WriteLine(result3 + "\n");

// 4. PUT — обновить изделие
logAction("Обновляем изделие...");
var updatedProduct = new { Id = 1, Name = "Болт М8 (обновлён)", Description = "Стальной болт улучшенный", Price = 6.99 };
var result4 = await SendJson(HttpMethod.Put, $"{baseUrl}/products/1", updatedProduct);
operationHandler("PUT /products/1", result4);
Console.WriteLine();

// 5. DELETE — удалить изделие
logAction("Удаляем изделие...");
var response5 = await client.DeleteAsync($"{baseUrl}/products/1");
operationHandler("DELETE /products/1", response5.StatusCode.ToString());
Console.WriteLine("\nГотово!");

// Объявления делегатов — всегда в конце файла
delegate void LogDelegate(string message);
delegate void OperationDelegate(string operation, string result);