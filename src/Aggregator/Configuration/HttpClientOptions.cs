namespace Aggregator.Configuration
{
    /// <summary>
    /// Настройки HTTP клиентов
    /// </summary>
    public class HttpClientOptions
    {
        public const string SectionName = "HttpClient";

        /// <summary>
        /// Таймаут запроса в секундах
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Время жизни HTTP обработчика в минутах
        /// </summary>
        public int HandlerLifetimeMinutes { get; set; } = 5;

        /// <summary>
        /// Максимальное количество попыток запроса
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Задержка между попытками в миллисекундах
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Игнорировать ошибки SSL сертификатов
        /// </summary>
        public bool IgnoreSslErrors { get; set; } = true;

        /// <summary>
        /// User Agent для запросов
        /// </summary>
        public string UserAgent { get; set; } = "Aggregator Bot 1.0";
    }
} 