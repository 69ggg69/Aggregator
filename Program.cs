using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net;
using Aggregator.Data;
using Aggregator.Services;
using Aggregator.ParserServices;
using Aggregator.Interfaces;
using Aggregator.Extensions;
using Aggregator.Services.Application;

namespace Aggregator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            try
            {
                var logger = host.Services.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Приложение Aggregator успешно инициализировано");
                
                // Показываем простое меню (временно, пока не добавим API)
                await ShowMainMenuAsync(host, logger);
            }
            catch (Exception ex)
            {
                var logger = host.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Произошла критическая ошибка при запуске приложения");
                throw;
            }
        }

        static async Task ShowMainMenuAsync(IHost host, ILogger logger)
        {
            while (true)
            {
                Console.WriteLine("\n=== Aggregator - Product Parser ===");
                Console.WriteLine("1. Запустить парсинг");
                Console.WriteLine("2. Проверить подключение к БД");
                Console.WriteLine("3. Показать статистику");
                Console.WriteLine("0. Выход");
                Console.Write("Выберите действие: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await RunParsingAsync(host, logger);
                        break;
                    case "2":
                        await CheckDatabaseConnectionAsync(host, logger);
                        break;
                    case "3":
                        await ShowStatisticsAsync(host, logger);
                        break;
                    case "0":
                        logger.LogInformation("Завершение работы приложения");
                        return;
                    default:
                        Console.WriteLine("Неверный выбор. Попробуйте снова.");
                        break;
                }
            }
        }

        static async Task RunParsingAsync(IHost host, ILogger logger)
        {
            try
            {
                logger.LogInformation("Запуск парсинга по запросу пользователя");
                var parsingService = host.Services.GetRequiredService<ParsingApplicationService>();
                await parsingService.RunParsingAsync();
                logger.LogInformation("Парсинг успешно завершен");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при выполнении парсинга");
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        static async Task CheckDatabaseConnectionAsync(IHost host, ILogger logger)
        {
            try
            {
                using var scope = host.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                Console.WriteLine("🔄 Проверяем подключение к базе данных...");
                
                // Реальная проверка - выполняем простой SQL запрос
                var startTime = DateTime.Now;
                await dbContext.Database.ExecuteSqlRawAsync("SELECT 1");
                var endTime = DateTime.Now;
                var responseTime = (endTime - startTime).TotalMilliseconds;
                
                // Дополнительно проверяем количество товаров
                var productsCount = await dbContext.Products.CountAsync();
                
                Console.WriteLine("✅ Подключение к базе данных успешно");
                Console.WriteLine($"   Время отклика: {responseTime:F2} мс");
                Console.WriteLine($"   Количество товаров в БД: {productsCount}");
                
                logger.LogInformation("Проверка подключения к БД - успешно. Время отклика: {ResponseTime}ms, товаров: {ProductsCount}", 
                    responseTime, productsCount);
            }
            catch (Npgsql.NpgsqlException pgEx)
            {
                Console.WriteLine($"❌ Ошибка PostgreSQL: {pgEx.Message}");
                if (pgEx.Message.Contains("No connection could be made") || 
                    pgEx.Message.Contains("Connection refused") ||
                    pgEx.Message.Contains("could not connect to server"))
                {
                    Console.WriteLine("   💡 Возможно, Docker контейнер с PostgreSQL не запущен");
                    Console.WriteLine("   Запустите: docker-compose up -d postgres");
                }
                logger.LogError(pgEx, "Ошибка подключения к PostgreSQL");
            }
            catch (System.Net.Sockets.SocketException sockEx)
            {
                Console.WriteLine($"❌ Ошибка сетевого подключения: {sockEx.Message}");
                Console.WriteLine("   💡 Проверьте, что Docker запущен и порт 5432 доступен");
                logger.LogError(sockEx, "Ошибка сетевого подключения к базе данных");
            }
            catch (TimeoutException timeEx)
            {
                Console.WriteLine($"❌ Таймаут подключения: {timeEx.Message}");
                Console.WriteLine("   💡 База данных может быть недоступна или перегружена");
                logger.LogError(timeEx, "Таймаут подключения к базе данных");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Общая ошибка подключения к БД: {ex.Message}");
                Console.WriteLine($"   Тип ошибки: {ex.GetType().Name}");
                
                // Дополнительная диагностика
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Внутренняя ошибка: {ex.InnerException.Message}");
                }
                
                logger.LogError(ex, "Общая ошибка подключения к базе данных");
            }
        }

        static async Task ShowStatisticsAsync(IHost host, ILogger logger)
        {
            try
            {
                var parsingService = host.Services.GetRequiredService<ParsingApplicationService>();
                var statistics = await parsingService.GetStatisticsAsync();

                Console.WriteLine($"\n📊 Детальная статистика:");
                Console.WriteLine($"   Всего товаров в БД: {statistics.TotalProducts}");
                Console.WriteLine($"   Последний парсинг: {(statistics.LastParseDate?.ToString("dd.MM.yyyy HH:mm") ?? "Не было")}");
                
                if (statistics.ShopStatistics.Count > 0)
                {
                    Console.WriteLine("\n📈 Статистика по магазинам:");
                    foreach (var shopStat in statistics.ShopStatistics)
                    {
                        Console.WriteLine($"   {shopStat.ShopName}: {shopStat.ProductCount} товаров (обновлено: {shopStat.LastUpdate:dd.MM.yyyy HH:mm})");
                    }
                }
                
                logger.LogInformation("Показана статистика: товаров {TotalProducts}, последний парсинг {LastParseDate}", 
                    statistics.TotalProducts, statistics.LastParseDate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения статистики: {ex.Message}");
                logger.LogError(ex, "Ошибка получения статистики");
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", 
                                     optional: true, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    // Используем extension метод для настройки всех сервисов
                    services.AddAggregatorServices(context.Configuration);
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.SetMinimumLevel(LogLevel.Information);
                });
    }
}
