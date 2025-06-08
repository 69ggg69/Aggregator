using Aggregator.Data;
using Aggregator.Interfaces;
using Aggregator.Models;
using Aggregator.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Aggregator.Services;

/// <summary>
/// Сервис для управления всеми операциями с базой данных
/// Координирует работу репозиториев и управляет соединениями
/// </summary>
public class DatabaseService : IDatabaseService
{
    private readonly ApplicationDbContext _context;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(
        ApplicationDbContext context,
        IProductRepository productRepository,
        ILogger<DatabaseService> logger)
    {
        _context = context;
        _productRepository = productRepository;
        _logger = logger;
    }

    /// <summary>
    /// Репозиторий для работы с товарами
    /// </summary>
    public IProductRepository Products => _productRepository;

    /// <summary>
    /// Проверяет подключение к базе данных
    /// </summary>
    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            Log.Info("🔍 Проверяем подключение к базе данных...");
            var stopwatch = Stopwatch.StartNew();
            
            // Выполняем простой запрос для проверки соединения
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");
            
            stopwatch.Stop();
            
            _logger.LogInformation("✅ Подключение к БД успешно. Время отклика: {ResponseTime}ms", 
                stopwatch.ElapsedMilliseconds);
            
            Log.Success($"База данных доступна (отклик: {stopwatch.ElapsedMilliseconds}ms)");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка подключения к базе данных");
            Log.Error("Ошибка подключения к БД", ex);
            return false;
        }
    }

    /// <summary>
    /// Выполняет миграции базы данных
    /// </summary>
    public async Task<bool> MigrateDatabaseAsync()
    {
        try
        {
            Log.Info("🚀 Применяем миграции базы данных...");
            
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                _logger.LogInformation("Найдено {count} ожидающих миграций", pendingMigrations.Count());
                
                foreach (var migration in pendingMigrations)
                {
                    _logger.LogDebug("Ожидающая миграция: {migration}", migration);
                }

                await _context.Database.MigrateAsync();
                Log.Success("Миграции успешно применены");
            }
            else
            {
                Log.Info("Все миграции уже применены");
            }

            _logger.LogInformation("✅ Миграции базы данных выполнены успешно");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка при выполнении миграций");
            Log.Error("Ошибка при применении миграций", ex);
            return false;
        }
    }

    /// <summary>
    /// Получает подробную информацию о состоянии базы данных
    /// </summary>
    public async Task<DatabaseInfo> GetDatabaseInfoAsync()
    {
        var info = new DatabaseInfo();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Проверяем подключение
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");
            info.IsConnected = true;
            stopwatch.Stop();
            info.ResponseTimeMs = stopwatch.ElapsedMilliseconds;

            // Получаем версию БД
            try
            {
                var versionResult = await _context.Database
                    .SqlQueryRaw<string>("SELECT version()")
                    .ToListAsync();
                info.DatabaseVersion = versionResult.FirstOrDefault() ?? "Unknown";
            }
            catch
            {
                info.DatabaseVersion = "Unable to determine";
            }

            // Проверяем миграции
            var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            
            info.AppliedMigrations = appliedMigrations.ToList();
            info.MigrationsApplied = !pendingMigrations.Any();

            // Получаем информацию о таблицах
            try
            {
                var tablesCount = await _context.Database
                    .SqlQueryRaw<int>("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public'")
                    .FirstAsync();
                info.TablesCount = tablesCount;
            }
            catch
            {
                info.TablesCount = 0;
            }

            _logger.LogDebug("Информация о БД получена: подключение={IsConnected}, версия={Version}, миграции={MigrationsApplied}", 
                info.IsConnected, info.DatabaseVersion, info.MigrationsApplied);

        }
        catch (Exception ex)
        {
            info.IsConnected = false;
            _logger.LogError(ex, "Ошибка при получении информации о БД");
        }

        return info;
    }

    /// <summary>
    /// Выполняет операцию в рамках транзакции
    /// </summary>
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        _logger.LogDebug("Начинаем транзакцию");
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var result = await operation();
            await transaction.CommitAsync();
            
            _logger.LogDebug("Транзакция успешно завершена");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка в транзакции, откатываем изменения");
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Очищает все данные из базы данных (для тестов)
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        try
        {
            Log.Warning("⚠️ ВНИМАНИЕ: Очищаем все данные из базы!");
            
            // Удаляем все товары
            var products = await _context.Products.ToListAsync();
            if (products.Any())
            {
                _context.Products.RemoveRange(products);
                var deletedCount = await _context.SaveChangesAsync();
                
                _logger.LogWarning("🗑️ Удалено {count} товаров из базы данных", deletedCount);
                Log.Warning($"Удалено {deletedCount} записей из БД");
            }
            else
            {
                Log.Info("База данных уже пуста");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при очистке базы данных");
            Log.Error("Ошибка при очистке БД", ex);
            throw;
        }
    }

    /// <summary>
    /// Создает резервную копию данных
    /// </summary>
    public async Task<bool> CreateBackupAsync(string backupPath)
    {
        try
        {
            Log.Info($"📦 Создаем резервную копию в {backupPath}");
            
            // Для PostgreSQL можно использовать pg_dump
            // Здесь простая реализация - экспорт в JSON
            var products = await _context.Products.ToListAsync();
            
            var backupData = new
            {
                BackupDate = DateTime.UtcNow,
                ProductsCount = products.Count,
                Products = products.Select(p => new 
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Shop,
                    p.ParseDate,
                    p.ImageUrl,
                    p.LocalImagePath
                })
            };

            var json = System.Text.Json.JsonSerializer.Serialize(backupData, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            await File.WriteAllTextAsync(backupPath, json);
            
            _logger.LogInformation("✅ Резервная копия создана: {backupPath}, товаров: {count}", 
                backupPath, products.Count);
            
            Log.Success($"Резервная копия создана ({products.Count} товаров)");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании резервной копии");
            Log.Error("Ошибка создания резервной копии", ex);
            return false;
        }
    }
} 