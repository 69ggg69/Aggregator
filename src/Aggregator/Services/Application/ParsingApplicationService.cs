using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Aggregator.Data;
using Aggregator.Services;
using Aggregator.Models;

namespace Aggregator.Services.Application
{
    /// <summary>
    /// Основной сервис приложения для выполнения парсинга
    /// </summary>
    public class ParsingApplicationService(
        ParserManager parserManager,
        ApplicationDbContext dbContext,
        ILogger<ParsingApplicationService> logger)
    {
        private readonly ParserManager _parserManager = parserManager;
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly ILogger<ParsingApplicationService> _logger = logger;

        /// <summary>
        /// Выполняет полный цикл парсинга всех сайтов
        /// </summary>
        public async Task RunParsingAsync()
        {
            try
            {
                _logger.LogInformation("Начинаем парсинг всех сайтов...");
                await _parserManager.ParseAllSites();

                await DisplayParsingResultsAsync();
                
                _logger.LogInformation("Парсинг успешно завершен");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении парсинга");
                
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
                    Console.WriteLine($"Стек вызовов: {ex.InnerException.StackTrace}");
                }
                throw;
            }
        }

        /// <summary>
        /// Получает статистику товаров в базе данных
        /// </summary>
        public async Task<ParsingStatistics> GetStatisticsAsync()
        {
            try
            {
                var totalProducts = await _dbContext.Products.CountAsync();
                var lastParseDate = await _dbContext.Products
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => (DateTime?)p.CreatedAt)
                    .FirstOrDefaultAsync();

                var productsByShop = await _dbContext.Products
                    .Include(p => p.Shop)
                    .GroupBy(p => p.Shop.Name)
                    .Select(g => new ShopStatistics 
                    { 
                        ShopName = g.Key, 
                        ProductCount = g.Count(),
                        LastUpdate = g.Max(p => p.CreatedAt)
                    })
                    .ToListAsync();

                return new ParsingStatistics
                {
                    TotalProducts = totalProducts,
                    LastParseDate = lastParseDate,
                    ShopStatistics = productsByShop
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статистики");
                throw;
            }
        }

        /// <summary>
        /// Отображает результаты парсинга в консоли
        /// </summary>
        private async Task DisplayParsingResultsAsync()
        {
            var allProducts = await _dbContext.Products
                .Include(p => p.Shop)
                .Include(p => p.ProductVariants)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("Парсинг завершен. Найдено товаров: {ProductCount}", allProducts.Count);

            Console.WriteLine("\nВсе товары в базе данных:");
            Console.WriteLine("==========================");
            
            if (!allProducts.Any())
            {
                Console.WriteLine("База данных пуста. Товары не найдены.");
            }
            else
            {
                foreach (var product in allProducts)
                {
                    var variantInfo = product.ProductVariants.Count != 0
                        ? $" ({product.ProductVariants.Count} вариантов)" 
                        : " [Без вариантов]";
                    Console.WriteLine($"{product.Shop.Name} - {product.Name}{variantInfo} (создан: {product.CreatedAt:dd.MM.yyyy HH:mm})");
                }
            }
        }
    }


} 