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
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            
            try
            {
                logger.LogInformation("🚀 Запуск приложения Aggregator v1.0");
                logger.LogInformation("Среда выполнения: {Environment}", 
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production");
                logger.LogInformation("Время запуска: {StartTime}", DateTime.Now);
                
                Console.WriteLine("✅ Приложение Aggregator успешно инициализировано");
                
                // Показываем простое меню (временно, пока не добавим API)
                await ShowMainMenuAsync(host, logger);
                
                logger.LogInformation("👋 Завершение работы приложения");
                logger.LogInformation("Время остановки: {StopTime}", DateTime.Now);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "💥 Критическая ошибка при запуске приложения");
                Console.WriteLine($"Критическая ошибка: {ex.Message}");
                throw;
            }
            finally
            {
                logger.LogInformation("🏁 Приложение полностью завершено");
            }
        }

        static async Task ShowMainMenuAsync(IHost host, ILogger logger)
        {
            logger.LogInformation("📋 Отображение главного меню");
            
            while (true)
            {
                Console.WriteLine("\n=== Aggregator - Product Parser ===");
                Console.WriteLine("1. Запустить парсинг");
                Console.WriteLine("2. Проверить подключение к БД");
                Console.WriteLine("3. Показать статистику");
                Console.WriteLine("0. Выход");
                Console.Write("Выберите действие: ");

                var choice = Console.ReadLine();
                logger.LogDebug("Пользователь выбрал действие: {UserChoice}", choice ?? "null");

                switch (choice)
                {
                    case "1":
                        logger.LogInformation("⚡ Пользователь запустил парсинг");
                        await RunParsingAsync(host, logger);
                        break;
                    case "2":
                        logger.LogInformation("🔍 Пользователь запустил проверку БД");
                        await CheckDatabaseConnectionAsync(host, logger);
                        break;
                    case "3":
                        logger.LogInformation("📊 Пользователь запросил статистику");
                        await ShowStatisticsAsync(host, logger);
                        break;
                    case "0":
                        logger.LogInformation("🚪 Пользователь выбрал выход из программы");
                        return;
                    default:
                        logger.LogWarning("❓ Пользователь ввел неверный выбор: {InvalidChoice}", choice ?? "null");
                        Console.WriteLine("Неверный выбор. Попробуйте снова.");
                        break;
                }
            }
        }

        static async Task RunParsingAsync(IHost host, ILogger logger)
        {
            var startTime = DateTime.Now;
            logger.LogInformation("🔄 Начало выполнения парсинга в {StartTime}", startTime);
            
            try
            {
                var parsingService = host.Services.GetRequiredService<ParsingApplicationService>();
                await parsingService.RunParsingAsync();
                
                var duration = DateTime.Now - startTime;
                logger.LogInformation("✅ Парсинг успешно завершен за {Duration}ms", duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                logger.LogError(ex, "❌ Ошибка при выполнении парсинга после {Duration}ms", duration.TotalMilliseconds);
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        static async Task CheckDatabaseConnectionAsync(IHost host, ILogger logger)
        {
            logger.LogInformation("🔍 Начало проверки подключения к базе данных");
            
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
                
                logger.LogInformation("✅ Подключение к БД успешно. Время отклика: {ResponseTime}ms, товаров: {ProductsCount}", 
                    responseTime, productsCount);
            }
            catch (Npgsql.NpgsqlException pgEx)
            {
                logger.LogError(pgEx, "❌ Ошибка PostgreSQL при проверке подключения");
                
                Console.WriteLine($"❌ Ошибка PostgreSQL: {pgEx.Message}");
                if (pgEx.Message.Contains("No connection could be made") || 
                    pgEx.Message.Contains("Connection refused") ||
                    pgEx.Message.Contains("could not connect to server"))
                {
                    Console.WriteLine("   💡 Возможно, Docker контейнер с PostgreSQL не запущен");
                    Console.WriteLine("   Запустите: docker-compose up -d postgres");
                }
            }
            catch (System.Net.Sockets.SocketException sockEx)
            {
                logger.LogError(sockEx, "❌ Ошибка сетевого подключения при проверке БД");
                
                Console.WriteLine($"❌ Ошибка сетевого подключения: {sockEx.Message}");
                Console.WriteLine("   💡 Проверьте, что Docker запущен и порт 5432 доступен");
            }
            catch (TimeoutException timeEx)
            {
                logger.LogError(timeEx, "⏰ Таймаут при подключении к БД");
                
                Console.WriteLine($"❌ Таймаут подключения: {timeEx.Message}");
                Console.WriteLine("   💡 База данных может быть недоступна или перегружена");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Общая ошибка при проверке подключения к БД");
                
                Console.WriteLine($"❌ Общая ошибка подключения к БД: {ex.Message}");
                Console.WriteLine($"   Тип ошибки: {ex.GetType().Name}");
                
                // Дополнительная диагностика
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Внутренняя ошибка: {ex.InnerException.Message}");
                }
            }
        }

        static async Task ShowStatisticsAsync(IHost host, ILogger logger)
        {
            logger.LogInformation("📊 Получение статистики из базы данных");
            
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
                
                logger.LogInformation("📊 Статистика отображена: товаров {TotalProducts}, магазинов {ShopsCount}, последний парсинг {LastParseDate}", 
                    statistics.TotalProducts, statistics.ShopStatistics.Count, statistics.LastParseDate);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Ошибка при получении статистики");
                Console.WriteLine($"❌ Ошибка получения статистики: {ex.Message}");
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
                    
                    // Добавляем цветной вывод в консоль (если поддерживается)
                    logging.AddConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                    });
                    
                    // Устанавливаем уровень логирования
                    logging.SetMinimumLevel(LogLevel.Information);
                    
                    // Для отладки можно включить Debug уровень
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        logging.SetMinimumLevel(LogLevel.Debug);
                    }
                });
    }
}
