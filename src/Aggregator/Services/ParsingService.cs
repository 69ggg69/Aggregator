using Aggregator.Interfaces;
using Aggregator.Models;
using Microsoft.Extensions.Logging;

namespace Aggregator.Services;

/// <summary>
/// Сервис парсинга который координирует работу парсеров и базы данных
/// Разделяет ответственности: парсер извлекает данные, DatabaseService работает с БД
/// </summary>
public class ParsingService
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<ParsingService> _logger;

    public ParsingService(IDatabaseService databaseService, ILogger<ParsingService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

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
            // 1. Парсим товары с сайта
            var parsedProducts = await parser.ParseProducts();
            _logger.LogInformation("📦 Парсер нашел {count} товаров на сайте {shopName}",
                parsedProducts.Count, parser.ShopName);

            if (parsedProducts.Count == 0)
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

            // 2. Получаем существующие товары из БД
            var existingProducts = await _databaseService.Products.GetProductsByShopAsync(parser.ShopName);

            // 3. Фильтруем новые товары (избегаем дубликатов по имени)
            // TODO: В новой архитектуре нужно будет сравнивать по ProductVariants
            var newProducts = parsedProducts
                .Where(p => !existingProducts.Any(ep => ep.Name == p.Name))
                .ToList();

            _logger.LogInformation("🆕 Найдено {newCount} новых товаров из {totalCount} для магазина {shopName}",
                newProducts.Count, parsedProducts.Count, parser.ShopName);

            // 4. Сохраняем новые товары в БД
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
                ParsedCount = parsedProducts.Count,
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