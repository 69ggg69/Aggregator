using Aggregator.Models;

namespace Aggregator.Interfaces;

/// <summary>
/// Интерфейс для управления всеми операциями с базой данных
/// Координирует работу репозиториев и управляет транзакциями
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// Репозиторий для работы с товарами
    /// </summary>
    IProductRepository Products { get; }

    /// <summary>
    /// Проверяет подключение к базе данных
    /// </summary>
    /// <returns>True если подключение успешно</returns>
    Task<bool> CheckConnectionAsync();

    /// <summary>
    /// Выполняет миграции базы данных
    /// </summary>
    /// <returns>True если миграции применены успешно</returns>
    Task<bool> MigrateDatabaseAsync();

    /// <summary>
    /// Получает информацию о состоянии базы данных
    /// </summary>
    /// <returns>Информация о БД</returns>
    Task<DatabaseInfo> GetDatabaseInfoAsync();

    /// <summary>
    /// Выполняет операцию в рамках транзакции
    /// </summary>
    /// <param name="operation">Операция для выполнения</param>
    /// <returns>Результат операции</returns>
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);

    /// <summary>
    /// Очищает все данные из базы (для тестов)
    /// </summary>
    /// <returns>Task</returns>
    Task ClearAllDataAsync();

    /// <summary>
    /// Создает резервную копию данных
    /// </summary>
    /// <param name="backupPath">Путь для сохранения резервной копии</param>
    /// <returns>True если резервная копия создана</returns>
    Task<bool> CreateBackupAsync(string backupPath);
}

/// <summary>
/// Информация о состоянии базы данных
/// </summary>
public class DatabaseInfo
{
    /// <summary>
    /// Подключение активно
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Версия базы данных
    /// </summary>
    public string DatabaseVersion { get; set; } = string.Empty;

    /// <summary>
    /// Время отклика в миллисекундах
    /// </summary>
    public double ResponseTimeMs { get; set; }

    /// <summary>
    /// Применены ли все миграции
    /// </summary>
    public bool MigrationsApplied { get; set; }

    /// <summary>
    /// Список примененных миграций
    /// </summary>
    public List<string> AppliedMigrations { get; set; } = new();

    /// <summary>
    /// Общий размер базы данных в байтах
    /// </summary>
    public long DatabaseSizeBytes { get; set; }

    /// <summary>
    /// Количество таблиц
    /// </summary>
    public int TablesCount { get; set; }
} 