namespace ProductionClient;

// Собственные типы делегатов для событий API

// Запрос выполнен: адрес, HTTP-код и время выполнения в миллисекундах
public delegate void RequestCompletedHandler(string method, string endpoint, int statusCode, long elapsedMs);

// Запрос завершился ошибкой (сервер недоступен, таймаут и т. п.)
public delegate void RequestFailedHandler(string method, string endpoint, Exception error);
