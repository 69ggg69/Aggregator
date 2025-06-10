using Aggregator.Interfaces;
using Aggregator.Models;
using Microsoft.Extensions.Logging;

namespace Aggregator.Services;

/// <summary>
/// Сервис для второго этапа парсинга - получение детальной информации о товарах и сохранение в БД
/// Отвечает за парсинг детальной информации и взаимодействие с базой данных
/// </summary>
public class DetailedParsingService(IDatabaseService databaseService, ILogger<DetailedParsingService> logger)
{
    private readonly IDatabaseService _databaseService = databaseService;
    private readonly ILogger<DetailedParsingService> _logger = logger;

    /// <summary>
    /// Выполняет второй этап парсинга - получение детальной информации и сохранение в БД
    /// </summary>
    /// <param name="parser">Парсер для конкретного магазина</param>
    /// <param name="basicProducts">Список товаров с базовой информацией</param>
    /// <returns>Результат детального парсинга</returns>
    public async Task<DetailedParsingResult> ParseDetailedProductsAsync(IParser parser, List<Product> basicProducts)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("🔍 Начинаем детальный парсинг {count} товаров магазина {shopName}", 
            basicProducts.Count, parser.ShopName);

        try
        {
            // Этап 2: Парсим детальную информацию для каждого товара
            var detailedProducts = new List<Product>();
            for (int i = 0; i < basicProducts.Count; i++)
            {
                var product = basicProducts[i];
                _logger.LogInformation("🔍 Парсинг детальной информации для товара {index}/{total}: {productName}",
                    i + 1, basicProducts.Count, product.Name);
                
                var detailedProduct = await parser.ParseDetailedProductAsync(product);
                detailedProducts.Add(detailedProduct);
            }

            _logger.LogInformation("✅ Завершен детальный парсинг {count} товаров для магазина {shopName}",
                detailedProducts.Count, parser.ShopName);

            // Получаем существующие товары из БД
            var existingProducts = await _databaseService.Products.GetProductsByShopAsync(parser.ShopName);

            // Фильтруем новые товары (избегаем дубликатов по имени)
            var newProducts = detailedProducts
                .Where(p => !existingProducts.Any(ep => ep.Name == p.Name))
                .ToList();

            _logger.LogInformation("🆕 Найдено {newCount} новых товаров из {totalCount} для магазина {shopName}",
                newProducts.Count, detailedProducts.Count, parser.ShopName);

            // Сохраняем новые товары в БД
            var addedCount = 0;
            if (newProducts.Count > 0)
            {
                addedCount = await _databaseService.Products.AddProductsAsync(newProducts);
                _logger.LogInformation("✅ Добавлено {addedCount} товаров в БД для магазина {shopName}",
                    addedCount, parser.ShopName);
            }
            else
            {
                _logger.LogInformation("ℹ️ Новых товаров не найдено для магазина {shopName}", parser.ShopName);
            }

            return new DetailedParsingResult
            {
                ShopName = parser.ShopName,
                DetailedProducts = detailedProducts,
                ProcessedCount = detailedProducts.Count,
                AddedCount = addedCount,
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка при детальном парсинге магазина {shopName}", parser.ShopName);

            return new DetailedParsingResult
            {
                ShopName = parser.ShopName,
                DetailedProducts = new List<Product>(),
                ProcessedCount = 0,
                AddedCount = 0,
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Выполняет детальный парсинг для результатов базового парсинга нескольких магазинов
    /// </summary>
    /// <param name="parser">Парсер</param>
    /// <param name="basicResults">Результаты базового парсинга</param>
    /// <returns>Результаты детального парсинга</returns>
    public async Task<List<DetailedParsingResult>> ParseMultipleShopsDetailedAsync(
        IEnumerable<IParser> parsers, 
        List<BasicParsingResult> basicResults)
    {
        var results = new List<DetailedParsingResult>();
        var parsersList = parsers.ToList();

        foreach (var basicResult in basicResults.Where(r => r.Success && r.BasicProducts.Count > 0))
        {
            var parser = parsersList.FirstOrDefault(p => p.ShopName == basicResult.ShopName);
            if (parser == null)
            {
                _logger.LogWarning("⚠️ Не найден парсер для магазина {shopName}", basicResult.ShopName);
                continue;
            }

            var result = await ParseDetailedProductsAsync(parser, basicResult.BasicProducts);
            results.Add(result);
        }

        // Общая статистика
        var totalProcessed = results.Sum(r => r.ProcessedCount);
        var totalAdded = results.Sum(r => r.AddedCount);
        var successCount = results.Count(r => r.Success);

        _logger.LogInformation("🎯 Детальный парсинг завершен: {successCount}/{totalCount} магазинов, обработано {totalProcessed} товаров, добавлено {totalAdded}",
            successCount, results.Count, totalProcessed, totalAdded);

        return results;
    }
}

/// <summary>
/// Результат детального парсинга одного магазина
/// </summary>
public class DetailedParsingResult
{
    public string ShopName { get; set; } = string.Empty;
    public List<Product> DetailedProducts { get; set; } = new();
    public int ProcessedCount { get; set; }
    public int AddedCount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }

    public TimeSpan Duration => EndTime - StartTime;
} 