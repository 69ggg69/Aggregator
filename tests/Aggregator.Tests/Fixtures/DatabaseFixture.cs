using Aggregator.Data;
using Microsoft.EntityFrameworkCore;

namespace Aggregator.Tests.Fixtures;

/// <summary>
/// Фикстура для настройки тестовой базы данных
/// Создает изолированную In-Memory БД для каждого теста
/// </summary>
public class DatabaseFixture : IDisposable
{
    public ApplicationDbContext Context { get; private set; }

    public DatabaseFixture()
    {
        // Создаем уникальную In-Memory БД для каждого экземпляра
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging() // Для отладки в тестах
            .Options;

        Context = new ApplicationDbContext(options);
        
        // Убеждаемся, что БД создана
        Context.Database.EnsureCreated();
    }

    /// <summary>
    /// Создает новый экземпляр контекста с теми же настройками
    /// Полезно когда нужно симулировать отдельное подключение к БД
    /// </summary>
    public ApplicationDbContext CreateNewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Context.Database.GetDbConnection().Database)
            .EnableSensitiveDataLogging()
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// Очищает все данные из базы данных
    /// Полезно для подготовки чистого состояния между тестами
    /// </summary>
    public async Task ClearDatabaseAsync()
    {
        Context.Products.RemoveRange(Context.Products);
        await Context.SaveChangesAsync();
    }

    /// <summary>
    /// Добавляет тестовые данные в базу
    /// </summary>
    /// <param name="entities">Сущности для добавления</param>
    public async Task SeedDataAsync<T>(params T[] entities) where T : class
    {
        Context.Set<T>().AddRange(entities);
        await Context.SaveChangesAsync();
    }

    public void Dispose()
    {
        Context.Dispose();
    }
} 