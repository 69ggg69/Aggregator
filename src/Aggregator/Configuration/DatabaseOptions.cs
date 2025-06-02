namespace Aggregator.Configuration
{
    /// <summary>
    /// Настройки подключения к базе данных
    /// </summary>
    public class DatabaseOptions
    {
        public const string SectionName = "Database";

        /// <summary>
        /// Строка подключения к базе данных
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Таймаут команды в секундах
        /// </summary>
        public int CommandTimeout { get; set; } = 30;

        /// <summary>
        /// Максимальное количество попыток подключения
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Включить детальное логирование SQL запросов
        /// </summary>
        public bool EnableSqlLogging { get; set; } = false;
    }
} 