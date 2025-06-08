using Aggregator.Data;
using Aggregator.Interfaces;
using Aggregator.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aggregator.Repositories;

/// <summary>
/// Репозиторий для работы с товарами в базе данных
/// Инкапсулирует всю логику работы с БД отдельно от парсинга
/// </summary>
public class ProductRepository(ApplicationDbContext context, ILogger<ProductRepository> logger) : IProductRepository
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<ProductRepository> _logger = logger;

    /// <summary>
    /// Получает существующие товары магазина за указанную дату
    /// </summary>
    public async Task<List<Product>> GetProductsByShopAsync(string shopName)
    {
        try
        {
            var products = await _context.Products
                .Where(p => p.Shop == shopName)
                .ToListAsync();

            _logger.LogDebug("Найдено {count} товаров для магазина {shopName}",
                products.Count, shopName);

            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении товаров магазина {shopName}", shopName);
            throw;
        }
    }

    /// <summary>
    /// Добавляет новые товары в базу данных
    /// </summary>
    public async Task<int> AddProductsAsync(List<Product> products)
    {
        if (products == null || products.Count == 0)
        {
            _logger.LogDebug("Нет товаров для добавления");
            return 0;
        }

        try
        {
            await _context.Products.AddRangeAsync(products);
            var savedCount = await _context.SaveChangesAsync();

            _logger.LogInformation("Добавлено {count} новых товаров в базу данных", savedCount);
            return savedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении {count} товаров в БД", products.Count);
            throw;
        }
    }

    /// <summary>
    /// Проверяет существует ли товар с таким названием и ценой
    /// </summary>
    public async Task<bool> ProductExistsAsync(string shopName, string productName, string? price)
    {
        try
        {
            return await _context.Products
                .AnyAsync(p => p.Shop == shopName
                    && p.Name == productName
                    && p.Price == price);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке существования товара {productName} в магазине {shopName}",
                productName, shopName);
            throw;
        }
    }

    /// <summary>
    /// Получает статистику товаров по магазинам
    /// </summary>
    public async Task<List<ShopStatistics>> GetShopStatisticsAsync()
    {
        try
        {
            var statistics = await _context.Products
                .GroupBy(p => p.Shop)
                .Select(g => new ShopStatistics
                {
                    ShopName = g.Key,
                    ProductCount = g.Count(),
                    LastUpdate = g.Max(p => p.ParseDate)
                })
                .ToListAsync();

            _logger.LogDebug("Получена статистика для {count} магазинов", statistics.Count);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении статистики по магазинам");
            throw;
        }
    }

    /// <summary>
    /// Получает общее количество товаров в БД
    /// </summary>
    public async Task<int> GetTotalProductsCountAsync()
    {
        try
        {
            var count = await _context.Products.CountAsync();
            _logger.LogDebug("Общее количество товаров в БД: {count}", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при подсчете общего количества товаров");
            throw;
        }
    }
}