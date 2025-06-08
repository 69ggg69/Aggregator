using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net;
using Aggregator.Data;
using Aggregator.Models;
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
                
                // Initialize seed data
                using (var scope = host.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    logger.LogInformation("🌱 Инициализация базовых данных...");
                    await SeedData.InitializeAsync(context);
                    logger.LogInformation("✅ Базовые данные инициализированы");
                }
                
                Console.WriteLine("✅ Приложение Aggregator успешно инициализировано");
                
                // ВРЕМЕННО: Тестируем новый ShopParser вместо обычного меню
                await TestNewShopParserAsync(host, logger);
                
                // Показываем простое меню (временно, пока не добавим API)
                // await ShowMainMenuAsync(host, logger);
                
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
                Console.WriteLine("4. Создать пример товара");
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
                    case "4":
                        logger.LogInformation("🛍️ Пользователь запросил создание примера товара");
                        await CreateSampleProductAsync(host, logger);
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

        static async Task CreateSampleProductAsync(IHost host, ILogger logger)
        {
            logger.LogInformation("🛍️ Создание примера товара с новой архитектурой БД");
            
            try
            {
                using var scope = host.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                Console.WriteLine("🔄 Создаем пример товара...");
                
                // Get lookup data
                var shop = await context.Shops.FirstAsync(s => s.Name == "MyShop");
                var material = await context.Materials.FirstAsync(m => m.Name == "Пух/Перо");
                var blackColor = await context.Colors.FirstAsync(c => c.Name == "Чёрный");
                var blueColor = await context.Colors.FirstAsync(c => c.Name == "Синий");
                var sizeM = await context.Sizes.FirstAsync(s => s.Name == "M");
                var sizeL = await context.Sizes.FirstAsync(s => s.Name == "L");
                var puffersCategory = await context.Categories.FirstAsync(c => c.Name == "Пуховики");
                var lightTag = await context.Tags.FirstAsync(t => t.Name == "Лёгкий");
                var waterproofTag = await context.Tags.FirstAsync(t => t.Name == "Водонепроницаемый");

                // Create product
                var product = new Product
                {
                    Name = "Пуховик зимний Premium",
                    Description = "Лёгкий водонепроницаемый пуховик для суровых зим. Отличный выбор для активного отдыха.",
                    Audience = ProductAudience.Unisex,
                    ShopId = shop.Id,
                    MaterialId = material.Id
                };

                context.Products.Add(product);
                await context.SaveChangesAsync();

                // Link to categories (only the most specific one)
                var productCategory = new ProductCategory 
                { 
                    ProductId = product.Id, 
                    CategoryId = puffersCategory.Id 
                };
                
                context.ProductCategories.Add(productCategory);

                // Link to tags
                var productTags = new[]
                {
                    new ProductTag { ProductId = product.Id, TagId = lightTag.Id },
                    new ProductTag { ProductId = product.Id, TagId = waterproofTag.Id }
                };
                
                context.ProductTags.AddRange(productTags);

                // Create variants
                var variants = new[]
                {
                    new ProductVariant
                    {
                        ProductId = product.Id,
                        ColorId = blackColor.Id,
                        SizeId = sizeM.Id,
                        Sku = "JKT-BLK-M",
                        Price = 199.99m
                    },
                    new ProductVariant
                    {
                        ProductId = product.Id,
                        ColorId = blueColor.Id,
                        SizeId = sizeL.Id,
                        Sku = "JKT-BLU-L",
                        Price = 199.99m
                    }
                };

                context.ProductVariants.AddRange(variants);
                await context.SaveChangesAsync();

                // Create availability records
                foreach (var variant in variants)
                {
                    var availability = new Availability
                    {
                        VariantId = variant.Id,
                        Quantity = 10,
                        IsAvailable = true
                    };
                    context.Availabilities.Add(availability);
                }

                await context.SaveChangesAsync();

                Console.WriteLine("✅ Пример товара успешно создан!");
                Console.WriteLine($"   Товар: {product.Name}");
                Console.WriteLine($"   Магазин: {shop.Name} ({shop.Url})");
                Console.WriteLine($"   Материал: {material.Name}");
                Console.WriteLine($"   Категория: {puffersCategory.Name}");
                Console.WriteLine($"   Теги: {string.Join(", ", new[] { lightTag.Name, waterproofTag.Name })}");
                Console.WriteLine($"   Варианты: {variants.Length}");
                Console.WriteLine($"     - {blackColor.Name} {sizeM.Name}: ${variants[0].Price}");
                Console.WriteLine($"     - {blueColor.Name} {sizeL.Name}: ${variants[1].Price}");
                
                logger.LogInformation("✅ Пример товара создан: {ProductName}, варианты: {VariantsCount}", 
                    product.Name, variants.Length);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Ошибка при создании примера товара");
                Console.WriteLine($"❌ Ошибка создания товара: {ex.Message}");
            }
        }

        /// <summary>
        /// ВРЕМЕННАЯ функция для тестирования нового ShopParser
        /// </summary>
        static async Task TestNewShopParserAsync(IHost host, ILogger logger)
        {
            logger.LogInformation("🧪 ТЕСТ: Запуск нового ShopParser для AskStudio");
            
            try
            {
                using var scope = host.Services.CreateScope();
                var clientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                var parserLogger = scope.ServiceProvider.GetRequiredService<ILogger<AskStudioShopParser>>();
                
                // Создаем экземпляр нашего нового парсера
                var parser = new AskStudioShopParser(clientFactory, parserLogger);
                
                Console.WriteLine($"🔄 Тестируем парсер для магазина: {parser.ShopName}");
                Console.WriteLine($"📋 Конфигурация URL:");
                
                foreach (var config in parser.BaseUrls)
                {
                    Console.WriteLine($"   URL: {config.BaseUrl}");
                    Console.WriteLine($"   Правила пагинации: {(config.PaginationRules.Length == 0 ? "Нет" : string.Join(", ", config.PaginationRules))}");
                }
                
                Console.WriteLine();
                Console.WriteLine("🚀 Запускаем парсинг базовой информации о товарах...");
                
                var startTime = DateTime.Now;
                var products = await parser.ParseBasicProductsAsync();
                var duration = DateTime.Now - startTime;
                
                Console.WriteLine();
                Console.WriteLine($"✅ Парсинг завершен за {duration.TotalSeconds:F2} секунд");
                Console.WriteLine($"📊 Результаты:");
                Console.WriteLine($"   Найдено товаров: {products.Count}");
                
                if (products.Count > 0)
                {
                    Console.WriteLine($"   Примеры найденных товаров:");
                    
                    var samplesToShow = Math.Min(5, products.Count);
                    for (int i = 0; i < samplesToShow; i++)
                    {
                        var product = products[i];
                        Console.WriteLine($"     {i + 1}. {product.Name}");
                        Console.WriteLine($"        URL: {product.ProductUrl}");
                        Console.WriteLine($"        Статус: {product.ParsingStatus}");
                        Console.WriteLine();
                    }
                    
                    if (products.Count > samplesToShow)
                    {
                        Console.WriteLine($"     ... и еще {products.Count - samplesToShow} товаров");
                    }
                }
                else
                {
                    Console.WriteLine("❌ Товары не найдены. Возможно, нужно откорректировать селекторы.");
                    Console.WriteLine("💡 Проверьте HTML структуру сайта и обновите селекторы в AskStudioShopParser");
                }
                
                logger.LogInformation("🧪 ТЕСТ завершен: найдено {ProductCount} товаров за {Duration}ms", 
                    products.Count, duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Ошибка при тестировании нового ShopParser");
                Console.WriteLine($"❌ Ошибка тестирования: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Внутренняя ошибка: {ex.InnerException.Message}");
                }
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
