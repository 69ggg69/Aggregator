using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Aggregator.Configuration;

namespace Aggregator.Extensions
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Добавляет все конфигурационные опции в DI контейнер
        /// </summary>
        public static IServiceCollection AddConfigurationOptions(this IServiceCollection services, IConfiguration configuration)
        {
            // Регистрируем конфигурационные модели
            services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
            services.Configure<HttpClientOptions>(configuration.GetSection(HttpClientOptions.SectionName));
            services.Configure<ParsingOptions>(configuration.GetSection(ParsingOptions.SectionName));

            return services;
        }

        /// <summary>
        /// Получает конфигурацию базы данных
        /// </summary>
        public static DatabaseOptions GetDatabaseOptions(this IConfiguration configuration)
        {
            return configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        }

        /// <summary>
        /// Получает конфигурацию HTTP клиента
        /// </summary>
        public static HttpClientOptions GetHttpClientOptions(this IConfiguration configuration)
        {
            return configuration.GetSection(HttpClientOptions.SectionName).Get<HttpClientOptions>() ?? new HttpClientOptions();
        }

        /// <summary>
        /// Получает конфигурацию парсинга
        /// </summary>
        public static ParsingOptions GetParsingOptions(this IConfiguration configuration)
        {
            return configuration.GetSection(ParsingOptions.SectionName).Get<ParsingOptions>() ?? new ParsingOptions();
        }
    }
} 