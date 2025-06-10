using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Aggregator.Data;
using Aggregator.Services;
using Aggregator.Models;
using Aggregator.Interfaces;

namespace Aggregator.Services.Application
{
    /// <summary>
    /// Сервис приложения для выполнения парсинга одного конкретного магазина
    /// Координирует двухэтапный парсинг для отдельного парсера
    /// </summary>
    public class ParsingApplicationService(
        IParser parser,
        BasicParsingService basicParsingService,
        DetailedParsingService detailedParsingService,
        ApplicationDbContext dbContext,
        ILogger<ParsingApplicationService> logger)
    {
        private readonly IParser _parser = parser;
        private readonly BasicParsingService _basicParsingService = basicParsingService;
        private readonly DetailedParsingService _detailedParsingService = detailedParsingService;
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly ILogger<ParsingApplicationService> _logger = logger;

        /// <summary>
        /// Название магазина для которого работает этот сервис
        /// </summary>
        public string ShopName => _parser.ShopName;

        /// <summary>
        /// Выполняет базовый парсинг для магазина
        /// </summary>
        public async Task<BasicParsingResult> RunBasicParsingAsync()
        {
            try
            {
                _logger.LogInformation("🚀 Запуск базового парсинга для магазина {ShopName}", ShopName);
                
                var result = await _basicParsingService.ParseBasicProductsAsync(_parser);
                
                if (result.Success)
                {
                    _logger.LogInformation("✅ Базовый парсинг завершен для магазина {ShopName}: найдено {count} товаров", 
                        ShopName, result.ProductCount);
                    
                    await DisplayBasicResultsAsync(result);
                }
                else
                {
                    _logger.LogError("❌ Базовый парсинг неуспешен для магазина {ShopName}: {error}", 
                        ShopName, result.Error);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Критическая ошибка при базовом парсинге магазина {ShopName}", ShopName);
                throw;
            }
        }

        /// <summary>
        /// Выполняет потоковый базовый парсинг с сохранением товаров по мере их обработки
        /// Проверяет дубликаты по URL и сохраняет каждый товар в отдельной транзакции
        /// </summary>
        public async Task<StreamingBasicParsingResult> RunStreamingBasicParsingAsync()
        {
            try
            {
                _logger.LogInformation("🌊 Запуск потокового базового парсинга для магазина {ShopName}", ShopName);
                
                var result = await _basicParsingService.ParseAndSaveBasicProductsStreamAsync(_parser);
                
                if (result.Success)
                {
                    _logger.LogInformation("✅ Потоковый базовый парсинг завершен для магазина {ShopName}: " +
                                         "найдено {total}, сохранено {saved}, пропущено {skipped}, ошибок {failed}", 
                        ShopName, result.TotalParsedCount, result.SavedCount, result.SkippedCount, result.FailedCount);
                    
                    await DisplayStreamingResultsAsync(result);
                }
                else
                {
                    _logger.LogError("❌ Потоковый базовый парсинг неуспешен для магазина {ShopName}: {error}", 
                        ShopName, result.Error);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Критическая ошибка при потоковом базовом парсинге магазина {ShopName}", ShopName);
                throw;
            }
        }

        /// <summary>
        /// Выполняет полный двухэтапный парсинг для магазина
        /// TODO: Пока что заглушка, будет реализовано позже
        /// </summary>
        public async Task<DetailedParsingResult> RunFullParsingAsync()
        {
            _logger.LogInformation("🔄 Запуск полного двухэтапного парсинга для магазина {ShopName}", ShopName);
            
            // Этап 1: Базовый парсинг
            var basicResult = await RunBasicParsingAsync();
            
            if (!basicResult.Success || basicResult.BasicProducts.Count == 0)
            {
                _logger.LogWarning("⚠️ Базовый парсинг не дал результатов, пропускаем детальный парсинг");
                
                // Возвращаем пустой результат детального парсинга
                return new DetailedParsingResult
                {
                    ShopName = ShopName,
                    Success = basicResult.Success,
                    Error = basicResult.Error,
                    StartTime = basicResult.StartTime,
                    EndTime = basicResult.EndTime
                };
            }

            // TODO: Этап 2: Детальный парсинг (заглушка)
            _logger.LogInformation("🔍 TODO: Здесь будет детальный парсинг {count} товаров", 
                basicResult.BasicProducts.Count);
            
            // Заглушка - возвращаем успешный результат без детального парсинга
            return new DetailedParsingResult
            {
                ShopName = ShopName,
                ProcessedCount = basicResult.ProductCount,
                AddedCount = 0, // TODO: Пока 0, потом будет реальное количество
                Success = true,
                StartTime = basicResult.StartTime,
                EndTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Получает статистику товаров для этого магазина
        /// </summary>
        public async Task<ShopStatistics> GetShopStatisticsAsync()
        {
            try
            {
                var productCount = await _dbContext.Products
                    .Include(p => p.Shop)
                    .Where(p => p.Shop.Name == ShopName)
                    .CountAsync();

                var lastUpdate = await _dbContext.Products
                    .Include(p => p.Shop)
                    .Where(p => p.Shop.Name == ShopName)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => (DateTime?)p.CreatedAt)
                    .FirstOrDefaultAsync();

                return new ShopStatistics
                {
                    ShopName = ShopName,
                    ProductCount = productCount,
                    LastUpdate = lastUpdate ?? DateTime.MinValue
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статистики для магазина {ShopName}", ShopName);
                throw;
            }
        }

        /// <summary>
        /// Отображает результаты базового парсинга в консоли
        /// </summary>
        private async Task DisplayBasicResultsAsync(BasicParsingResult result)
        {
            Console.WriteLine($"\n=== Результаты базового парсинга: {ShopName} ===");
            Console.WriteLine($"Найдено товаров: {result.ProductCount}");
            Console.WriteLine($"Время выполнения: {result.Duration.TotalSeconds:F2} сек");
            
            if (result.BasicProducts.Count > 0)
            {
                Console.WriteLine("\nНайденные товары (базовая информация):");
                foreach (var product in result.BasicProducts.Take(5)) // Показываем первые 5
                {
                    Console.WriteLine($"- {product.Name}");
                    if (!string.IsNullOrEmpty(product.ProductUrl))
                        Console.WriteLine($"  URL: {product.ProductUrl}");
                }
                
                if (result.BasicProducts.Count > 5)
                {
                    Console.WriteLine($"... и ещё {result.BasicProducts.Count - 5} товаров");
                }
            }
            
            await Task.CompletedTask; // Для async совместимости
        }

        /// <summary>
        /// Отображает результаты потокового базового парсинга в консоли
        /// </summary>
        private async Task DisplayStreamingResultsAsync(StreamingBasicParsingResult result)
        {
            Console.WriteLine($"\n=== Результаты потокового базового парсинга: {ShopName} ===");
            Console.WriteLine($"📊 Статистика:");
            Console.WriteLine($"   Найдено товаров: {result.TotalParsedCount}");
            Console.WriteLine($"   Сохранено: {result.SavedCount}");
            Console.WriteLine($"   Пропущено (дубликаты): {result.SkippedCount}");
            Console.WriteLine($"   Ошибок: {result.FailedCount}");
            Console.WriteLine($"   Успешность: {result.SuccessRate:F1}%");
            Console.WriteLine($"   Время выполнения: {result.Duration.TotalSeconds:F2} сек");
            
            if (result.SavedProducts.Count > 0)
            {
                Console.WriteLine("\n✅ Сохраненные товары:");
                foreach (var product in result.SavedProducts.Take(5)) // Показываем первые 5
                {
                    Console.WriteLine($"   - {product.Name}");
                    if (!string.IsNullOrEmpty(product.ProductUrl))
                        Console.WriteLine($"     URL: {product.ProductUrl}");
                }
                
                if (result.SavedProducts.Count > 5)
                {
                    Console.WriteLine($"   ... и ещё {result.SavedProducts.Count - 5} товаров");
                }
            }

            if (result.Errors.Count > 0)
            {
                Console.WriteLine("\n❌ Ошибки:");
                foreach (var error in result.Errors.Take(3)) // Показываем первые 3 ошибки
                {
                    Console.WriteLine($"   - {error}");
                }
                
                if (result.Errors.Count > 3)
                {
                    Console.WriteLine($"   ... и ещё {result.Errors.Count - 3} ошибок");
                }
            }
            
            await Task.CompletedTask; // Для async совместимости
        }
    }
} 