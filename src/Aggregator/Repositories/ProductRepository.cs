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
                .Include(p => p.Shop)
                .Where(p => p.Shop.Name == shopName)
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
    /// Проверяет существует ли товар с таким названием
    /// TODO: В новой архитектуре нужно проверять варианты товара с ценами
    /// </summary>
    public async Task<bool> ProductExistsAsync(string shopName, string productName, string? price = null)
    {
        try
        {
            return await _context.Products
                .Include(p => p.Shop)
                .AnyAsync(p => p.Shop.Name == shopName && p.Name == productName);
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
                .Include(p => p.Shop)
                .GroupBy(p => p.Shop.Name)
                .Select(g => new ShopStatistics
                {
                    ShopName = g.Key,
                    ProductCount = g.Count(),
                    LastUpdate = g.Max(p => p.CreatedAt)
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

    /// <summary>
    /// Получает список URL всех товаров магазина (для проверки дубликатов)
    /// </summary>
    public async Task<HashSet<string>> GetProductUrlsByShopAsync(string shopName)
    {
        try
        {
            var urls = await _context.Products
                .Include(p => p.Shop)
                .Where(p => p.Shop.Name == shopName && !string.IsNullOrEmpty(p.ProductUrl))
                .Select(p => p.ProductUrl!)
                .ToListAsync();

            _logger.LogDebug("Найдено {count} URL товаров для магазина {shopName}", urls.Count, shopName);
            return urls.ToHashSet();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении URL товаров магазина {shopName}", shopName);
            throw;
        }
    }

    /// <summary>
    /// Добавляет один товар в базу данных в отдельной транзакции
    /// </summary>
    public async Task<bool> AddProductAsync(Product product)
    {
        if (product == null)
        {
            _logger.LogWarning("Попытка сохранить null товар");
            return false;
        }

        // Используем отдельную транзакцию для каждого товара
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            await _context.Products.AddAsync(product);
            var savedCount = await _context.SaveChangesAsync();
            
            if (savedCount > 0)
            {
                await transaction.CommitAsync();
                _logger.LogDebug("Товар успешно сохранен: {productName} - {productUrl}", 
                    product.Name, product.ProductUrl);
                return true;
            }
            else
            {
                await transaction.RollbackAsync();
                _logger.LogWarning("Товар не был сохранен: {productName} - {productUrl}", 
                    product.Name, product.ProductUrl);
                return false;
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Ошибка при сохранении товара: {productName} - {productUrl}", 
                product.Name, product.ProductUrl ?? "нет URL");
            throw;
        }
    }

    /// <summary>
    /// Получает магазин по названию или создает его, если не существует
    /// </summary>
    public async Task<Shop> EnsureShopExistsAsync(string shopName, string? shopUrl = null)
    {
        if (string.IsNullOrEmpty(shopName))
            throw new ArgumentException("Название магазина не может быть пустым", nameof(shopName));

        try
        {
            // Сначала ищем существующий магазин
            var existingShop = await _context.Shops
                .FirstOrDefaultAsync(s => s.Name == shopName);

            if (existingShop != null)
            {
                _logger.LogDebug("Магазин {shopName} уже существует с ID {shopId}", shopName, existingShop.Id);
                return existingShop;
            }

            // Создаем новый магазин
            var newShop = new Shop
            {
                Name = shopName,
                // WTF
                Url = shopUrl ?? $"https://{shopName.ToLower().Replace(" ", "")}.com", // Генерируем URL по умолчанию
                Description = $"Автоматически созданный магазин для парсера {shopName}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Shops.AddAsync(newShop);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Создан новый магазин: {shopName} с ID {shopId}", shopName, newShop.Id);
            return newShop;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании/получении магазина {shopName}", shopName);
            throw;
        }
    }

    /// <summary>
    /// Получает ID магазина по названию
    /// </summary>
    public async Task<int?> GetShopIdByNameAsync(string shopName)
    {
        if (string.IsNullOrEmpty(shopName))
            return null;

        try
        {
            var shopId = await _context.Shops
                .Where(s => s.Name == shopName)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();

            _logger.LogDebug("ID магазина {shopName}: {shopId}", shopName, shopId);
            return shopId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении ID магазина {shopName}", shopName);
            throw;
        }
    }
}