using ProductionClient;

// Консольный клиент API системы управления производством.
// Демонстрирует: собственные делегаты, многоадресный делегат (+=, -=), Action<T> и Func<T, TResult>

Console.OutputEncoding = System.Text.Encoding.UTF8;

// По умолчанию обращаемся к API через Nginx (порт 80). Адрес можно передать аргументом
string baseUrl = args.Length > 0 ? args[0] : "http://localhost";
string logFile = Path.Combine(AppContext.BaseDirectory, "api-log.txt");
File.WriteAllText(logFile, $"=== Лог запросов {DateTime.Now:dd.MM.yyyy HH:mm:ss} ==={Environment.NewLine}");

var api = new ApiService(baseUrl);

// ---------- Обработчики событий ----------

// Обработчик 1: логирование в файл
RequestCompletedHandler logToFile = (method, endpoint, code, ms) =>
    File.AppendAllText(logFile, $"{DateTime.Now:HH:mm:ss.fff} {method} {endpoint} -> {code} ({ms} мс){Environment.NewLine}");

// Обработчик 2: вывод результата в консоль
RequestCompletedHandler printResult = (method, endpoint, code, ms) =>
{
    Console.ForegroundColor = code < 400 ? ConsoleColor.Green : ConsoleColor.Red;
    Console.WriteLine($"   {method,-6} {endpoint,-20} HTTP {code}  за {ms} мс");
    Console.ResetColor();
};

// Обработчик ошибок (сервер недоступен)
RequestFailedHandler printError = (method, endpoint, error) =>
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"   {method} {endpoint}: ошибка — {error.Message}");
    Console.ResetColor();
};

// Многоадресный делегат: на одно событие подписаны два обработчика
api.RequestCompleted += logToFile;
api.RequestCompleted += printResult;
api.RequestFailed += printError;

// Action<T> — действие без результата: печать заголовка шага
Action<string> step = title => Console.WriteLine($"{Environment.NewLine}>> {title}");

// Func<T, TResult> — функция с результатом: краткое описание изделия
Func<Product?, string> describe = p => p == null ? "(нет данных)" : p.ToString();

const string products = "/api/products";
Console.WriteLine($"Клиент ProductionSystem. API: {baseUrl}");
Console.WriteLine($"Лог запросов пишется в файл: {logFile}");

// ---------- Операции 1–3: оба обработчика активны ----------

step("1. POST — создание изделия");
var created = await api.CreateAsync(products, new Product { Name = "Шайба М8", Description = "Шайба плоская, сталь", Price = 1.80m });
Console.WriteLine("   Создано: " + describe(created));
if (created == null)
{
    Console.WriteLine("Не удалось создать изделие. Проверьте, что система запущена: docker compose up -d");
    return;
}

step("2. GET — список изделий");
var list = await api.GetAllAsync<Product>(products);
list.ForEach(p => Console.WriteLine("   " + describe(p)));   // List<T>.ForEach принимает Action<T>

step("3. GET по id — получение созданного изделия");
Console.WriteLine("   Получено: " + describe(await api.GetByIdAsync<Product>(products, created.Id)));

// ---------- Динамическая отписка ----------
api.RequestCompleted -= logToFile;
Console.WriteLine($"{Environment.NewLine}*** Обработчик логирования отключён (-=). Дальше запросы в файл не пишутся ***");

// ---------- Операции 4–5: только вывод в консоль ----------

step("4. PUT — обновление изделия");
created.Price = 2.10m;
created.Description = "Шайба плоская, сталь, оцинкованная";
bool updated = await api.UpdateAsync(products, created.Id, created);
Console.WriteLine("   Обновлено: " + (updated ? "да" : "нет"));

step("5. DELETE — удаление изделия");
bool deleted = await api.DeleteAsync(products, created.Id);
Console.WriteLine("   Удалено: " + (deleted ? "да" : "нет"));

step("6. GET по id — удалённое изделие (ожидается 404)");
Console.WriteLine("   Результат: " + describe(await api.GetByIdAsync<Product>(products, created.Id)));

// Подписываем логирование обратно (+=) и делаем ещё один запрос
api.RequestCompleted += logToFile;
step("7. GET — список заказов (логирование снова включено)");
var orders = await api.GetAllAsync<object>("/api/orders");
Console.WriteLine($"   Заказов в системе: {orders.Count}");

Console.WriteLine($"{Environment.NewLine}Содержимое лога ({logFile}):");
Console.WriteLine(File.ReadAllText(logFile));

// Чтобы окно консоли не закрылось сразу при запуске двойным щелчком
if (!Console.IsInputRedirected)
{
    Console.WriteLine("Нажмите любую клавишу для выхода...");
    Console.ReadKey(true);
}
