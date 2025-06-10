using Aggregator.Interfaces;
using Aggregator.Models;
using Microsoft.Extensions.Logging;

namespace Aggregator.Services;

/// <summary>
/// Сервис парсинга который координирует работу парсеров и базы данных
/// Разделяет ответственности: парсер извлекает данные, DatabaseService работает с БД
/// </summary>
public class ParsingService(IDatabaseService databaseService, ILogger<ParsingService> logger)
{
    private readonly IDatabaseService _databaseService = databaseService;
    private readonly ILogger<ParsingService> _logger = logger;

    /// <summary>
    /// Выполняет полный цикл парсинга для одного парсера:
    /// 1. Парсит товары с сайта
    /// 2. Проверяет дубликаты в БД
    /// 3. Сохраняет новые товары
    /// </summary>
    /// <param name="parser">Парсер для конкретного магазина</param>
    /// <returns>Результат парсинга</returns>
    public async Task<ParsingResult> ParseShopAsync(IParser parser)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("🚀 Начинаем парсинг магазина {shopName}", parser.ShopName);

        try
        {
            // 1. Этап 1: Парсим базовую информацию о товарах с сайта
            var basicProducts = await parser.ParseBasicProductsAsync();
            _logger.LogInformation("📦 Парсер нашел {count} товаров (базовая информация) на сайте {shopName}",
                basicProducts.Count, parser.ShopName);

            if (basicProducts.Count == 0)
            {
                _logger.LogWarning("⚠️  Парсер не нашел товаров на сайте {shopName}", parser.ShopName);
                return new ParsingResult
                {
                    ShopName = parser.ShopName,
                    ParsedCount = 0,
                    AddedCount = 0,
                    StartTime = startTime,
                    EndTime = DateTime.UtcNow,
                    Success = true
                };
            }

            // 2. Этап 2: Парсим детальную информацию для каждого товара
            var detailedProducts = new List<Product>();
            for (int i = 0; i < basicProducts.Count; i++)
            {
                var product = basicProducts[i];
                _logger.LogInformation("🔍 Парсинг детальной информации для товара {index}/{total}: {productName}",
                    i + 1, basicProducts.Count, product.Name);
                
                var detailedProduct = await parser.ParseDetailedProductAsync(product);
                detailedProducts.Add(detailedProduct);
            }

            _logger.LogInformation("✅ Завершен двухэтапный парсинг {count} товаров для магазина {shopName}",
                detailedProducts.Count, parser.ShopName);

            // 3. Получаем существующие товары из БД
            var existingProducts = await _databaseService.Products.GetProductsByShopAsync(parser.ShopName);

            // 4. Фильтруем новые товары (избегаем дубликатов по имени)
            // TODO: В новой архитектуре нужно будет сравнивать по ProductVariants
            var newProducts = detailedProducts
                .Where(p => !existingProducts.Any(ep => ep.Name == p.Name))
                .ToList();

            _logger.LogInformation("🆕 Найдено {newCount} новых товаров из {totalCount} для магазина {shopName}",
                newProducts.Count, detailedProducts.Count, parser.ShopName);

            // 5. Сохраняем новые товары в БД
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

            return new ParsingResult
            {
                ShopName = parser.ShopName,
                ParsedCount = detailedProducts.Count,
                AddedCount = addedCount,
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка при парсинге магазина {shopName}", parser.ShopName);

            return new ParsingResult
            {
                ShopName = parser.ShopName,
                ParsedCount = 0,
                AddedCount = 0,
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Выполняет парсинг для нескольких магазинов
    /// </summary>
    /// <param name="parsers">Список парсеров</param>
    /// <returns>Результаты парсинга для каждого магазина</returns>
    public async Task<List<ParsingResult>> ParseMultipleShopsAsync(IEnumerable<IParser> parsers)
    {
        var results = new List<ParsingResult>();

        foreach (var parser in parsers)
        {
            var result = await ParseShopAsync(parser);
            results.Add(result);
        }

        // Общая статистика
        var totalParsed = results.Sum(r => r.ParsedCount);
        var totalAdded = results.Sum(r => r.AddedCount);
        var successCount = results.Count(r => r.Success);

        _logger.LogInformation("🎯 Парсинг завершен: {successCount}/{totalCount} магазинов, найдено {totalParsed} товаров, добавлено {totalAdded}",
            successCount, results.Count, totalParsed, totalAdded);

        return results;
    }
}

/// <summary>
/// Результат парсинга одного магазина
/// </summary>
public class ParsingResult
{
    public string ShopName { get; set; } = string.Empty;
    public int ParsedCount { get; set; }
    public int AddedCount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }

    public TimeSpan Duration => EndTime - StartTime;
}