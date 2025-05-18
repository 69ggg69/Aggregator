using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Aggregator.Data;
using Aggregator.Services;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Aggregator.ParserServices;
using Aggregator.Interfaces;

namespace Aggregator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = ConfigureServices();
            
            // Создаем scope для работы с сервисами
            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            
            try 
            {
                var parserManager = scope.ServiceProvider.GetRequiredService<ParserManager>();
                Console.WriteLine("Начинаем парсинг...");
                await parserManager.ParseAllSites();

                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var allProducts = await dbContext.Products
                    .OrderByDescending(p => p.ParseDate)
                    .ToListAsync();

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
                        Console.WriteLine($"{product.Shop} - {product.Name}: {product.Price} (спаршено: {product.ParseDate})");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
                    Console.WriteLine($"Стек вызовов: {ex.InnerException.StackTrace}");
                }
            }
        }

        private static IServiceCollection ConfigureServices()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var services = new ServiceCollection();

            // Регистрация сервисов
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
            
            // Регистрируем HttpClient с нашим handler
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
                UseProxy = false, // Отключаем прокси
                AutomaticDecompression = DecompressionMethods.All
            };

            services.AddHttpClient("SafeHttpClient")
                .ConfigurePrimaryHttpMessageHandler(() => handler)
                .SetHandlerLifetime(TimeSpan.FromMinutes(5)); // Увеличиваем время жизни handler
            
            services.AddScoped<AskStudioParser>();
            services.AddScoped<ZnwrParser>();
            services.AddScoped<ParserManager>();
            services.AddScoped<IEnumerable<IParser>>(sp => new List<IParser>
            {
                sp.GetRequiredService<AskStudioParser>(),
                sp.GetRequiredService<ZnwrParser>()
            });

            return services;
        }
    }
}
