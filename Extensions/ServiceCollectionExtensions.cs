using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net;
using Aggregator.Data;
using Aggregator.Services;
using Aggregator.Services.Application;
using Aggregator.ParserServices;
using Aggregator.Interfaces;

namespace Aggregator.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Добавляет настройку базы данных
        /// </summary>
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
                
            return services;
        }

        /// <summary>
        /// Добавляет настройку HTTP клиентов
        /// </summary>
        public static IServiceCollection AddHttpClients(this IServiceCollection services)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
                UseProxy = false,
                AutomaticDecompression = DecompressionMethods.All
            };

            services.AddHttpClient("SafeHttpClient")
                .ConfigurePrimaryHttpMessageHandler(() => handler)
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            return services;
        }

        /// <summary>
        /// Добавляет сервисы приложения
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Core application services
            services.AddScoped<ParsingApplicationService>();
            services.AddScoped<ParserManager>();
            
            // Infrastructure services
            services.AddScoped<ImageService>();
            
            return services;
        }

        /// <summary>
        /// Добавляет парсеры
        /// </summary>
        public static IServiceCollection AddParsers(this IServiceCollection services)
        {
            // Individual parsers
            services.AddScoped<AskStudioParser>();
            services.AddScoped<ZnwrParser>();
            
            // Collection of all parsers
            services.AddScoped<IEnumerable<IParser>>(sp => new List<IParser>
            {
                sp.GetRequiredService<AskStudioParser>(),
                sp.GetRequiredService<ZnwrParser>()
            });

            return services;
        }

        /// <summary>
        /// Добавляет все сервисы приложения одним методом
        /// </summary>
        public static IServiceCollection AddAggregatorServices(this IServiceCollection services, IConfiguration configuration)
        {
            return services
                .AddConfigurationOptions(configuration)
                .AddDatabase(configuration)
                .AddHttpClients()
                .AddApplicationServices()
                .AddParsers();
        }
    }
} 