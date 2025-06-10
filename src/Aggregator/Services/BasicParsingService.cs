using Aggregator.Interfaces;
using Aggregator.Models;
using Microsoft.Extensions.Logging;

namespace Aggregator.Services;

/// <summary>
/// Сервис для первого этапа парсинга - получение базовой информации о товарах
/// Отвечает за извлечение названий товаров, ссылок и их потоковое сохранение в БД
/// </summary>
public class BasicParsingService(IDatabaseService databaseService, ILogger<BasicParsingService> logger)
{
    private readonly IDatabaseService _databaseService = databaseService;
    private readonly ILogger<BasicParsingService> _logger = logger;

    /// <summary>
    /// Выполняет первый этап парсинга - получение базовой информации о товарах
    /// </summary>
    /// <param name="parser">Парсер для конкретного магазина</param>
    /// <returns>Результат базового парсинга</returns>
    public async Task<BasicParsingResult> ParseBasicProductsAsync(IParser parser)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("🚀 Начинаем базовый парсинг магазина {shopName}", parser.ShopName);

        try
        {
            // Этап 1: Парсим базовую информацию о товарах с сайта
            var basicProducts = await parser.ParseBasicProductsAsync();
            _logger.LogInformation("📦 Найдено {count} товаров (базовая информация) на сайте {shopName}",
                basicProducts.Count, parser.ShopName);

            if (basicProducts.Count == 0)
            {
                _logger.LogWarning("⚠️  Базовый парсер не нашел товаров на сайте {shopName}", parser.ShopName);
            }

            return new BasicParsingResult
            {
                ShopName = parser.ShopName,
                BasicProducts = basicProducts,
                ProductCount = basicProducts.Count,
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка при базовом парсинге магазина {shopName}", parser.ShopName);

            return new BasicParsingResult
            {
                ShopName = parser.ShopName,
                BasicProducts = new List<Product>(),
                ProductCount = 0,
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Выполняет потоковый базовый парсинг с сохранением товаров по мере их обработки
    /// Проверяет дубликаты по URL и сохраняет каждый товар в отдельной транзакции
    /// </summary>
    /// <param name="parser">Парсер для конкретного магазина</param>
    /// <returns>Результат потокового базового парсинга</returns>
    public async Task<StreamingBasicParsingResult> ParseAndSaveBasicProductsStreamAsync(IParser parser)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("🌊 Начинаем потоковый базовый парсинг с сохранением для магазина {shopName}", parser.ShopName);

        var result = new StreamingBasicParsingResult
        {
            ShopName = parser.ShopName,
            StartTime = startTime,
            Success = true
        };

        try
        {
            // Проверяем и создаем магазин, если его нет
            var shop = await _databaseService.Products.EnsureShopExistsAsync(parser.ShopName, parser.ShopUrl);
            _logger.LogInformation("🏪 Магазин {shopName} готов для парсинга (ID: {shopId})", 
                parser.ShopName, shop.Id);

            // Сначала получаем все существующие URL для этого магазина
            var existingProductUrls = await _databaseService.Products.GetProductUrlsByShopAsync(parser.ShopName);
            _logger.LogInformation("📋 Найдено {count} существующих товаров для магазина {shopName}", 
                existingProductUrls.Count, parser.ShopName);

            // Парсим базовую информацию
            var basicProducts = await parser.ParseBasicProductsAsync();
            _logger.LogInformation("🔍 Спаршено {count} товаров (базовая информация) для магазина {shopName}",
                basicProducts.Count, parser.ShopName);

            result.TotalParsedCount = basicProducts.Count;

            // Обрабатываем товары по одному
            foreach (var product in basicProducts)
            {
                try
                {
                    // Проверяем дубликат по URL
                    if (!string.IsNullOrEmpty(product.ProductUrl) && 
                        existingProductUrls.Contains(product.ProductUrl))
                    {
                        _logger.LogDebug("⏭️ Пропускаем дубликат товара по URL: {productName} - {productUrl}", 
                            product.Name, product.ProductUrl);
                        result.SkippedCount++;
                        continue;
                    }

                    // Устанавливаем статус базового парсинга и связываем с магазином
                    product.ParsingStatus = ParsingStatus.BasicParsed;
                    product.ShopId = shop.Id;
                    product.CreatedAt = DateTime.UtcNow;
                    product.UpdatedAt = DateTime.UtcNow;

                    // Сохраняем товар в отдельной транзакции
                    var saved = await _databaseService.Products.AddProductAsync(product);
                    
                    if (saved)
                    {
                        _logger.LogDebug("✅ Сохранен товар: {productName} - {productUrl}", 
                            product.Name, product.ProductUrl);
                        result.SavedCount++;
                        result.SavedProducts.Add(product);
                        
                        // Добавляем URL в существующие, чтобы избежать дубликатов в рамках этой сессии
                        if (!string.IsNullOrEmpty(product.ProductUrl))
                        {
                            existingProductUrls.Add(product.ProductUrl);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Не удалось сохранить товар: {productName} - {productUrl}", 
                            product.Name, product.ProductUrl);
                        result.FailedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Ошибка при сохранении товара: {productName} - {productUrl}", 
                        product.Name, product.ProductUrl ?? "нет URL");
                    result.FailedCount++;
                    result.Errors.Add($"Товар '{product.Name}': {ex.Message}");
                }
            }

            result.EndTime = DateTime.UtcNow;
            
            _logger.LogInformation("🎯 Потоковый базовый парсинг завершен для магазина {shopName}: " +
                                 "найдено {totalCount}, сохранено {savedCount}, пропущено {skippedCount}, ошибок {failedCount}",
                parser.ShopName, result.TotalParsedCount, result.SavedCount, result.SkippedCount, result.FailedCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Критическая ошибка при потоковом базовом парсинге магазина {shopName}", parser.ShopName);

            result.Success = false;
            result.Error = ex.Message;
            result.EndTime = DateTime.UtcNow;
            
            return result;
        }
    }

}

/// <summary>
/// Результат потокового базового парсинга одного магазина
/// </summary>
public class StreamingBasicParsingResult
{
    public string ShopName { get; set; } = string.Empty;
    public List<Product> SavedProducts { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public int TotalParsedCount { get; set; }
    public int SavedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }

    public TimeSpan Duration => EndTime - StartTime;
    public double SuccessRate => TotalParsedCount > 0 ? (double)SavedCount / TotalParsedCount * 100 : 0;
}

/// <summary>
/// Результат базового парсинга одного магазина
/// </summary>
public class BasicParsingResult
{
    public string ShopName { get; set; } = string.Empty;
    public List<Product> BasicProducts { get; set; } = new();
    public int ProductCount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }

    public TimeSpan Duration => EndTime - StartTime;
} 