using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Aggregator.Data;
using Aggregator.Interfaces;
using Aggregator.Services;

namespace Aggregator.Services.Application
{
    /// <summary>
    /// Фабрика для создания инстансов ParsingApplicationService под каждый парсер
    /// </summary>
    public class ParsingApplicationServiceFactory(
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly ILoggerFactory _loggerFactory = loggerFactory;

        /// <summary>
        /// Создает новый инстанс ParsingApplicationService для указанного парсера
        /// </summary>
        /// <param name="parser">Парсер для которого создается сервис</param>
        /// <returns>Настроенный инстанс ParsingApplicationService</returns>
        public ParsingApplicationService CreateForParser(IParser parser)
        {
            if (parser == null)
                throw new ArgumentNullException(nameof(parser));

            // Получаем зависимости из DI контейнера
            var basicParsingService = _serviceProvider.GetRequiredService<BasicParsingService>();
            var detailedParsingService = _serviceProvider.GetRequiredService<DetailedParsingService>();
            var dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = _loggerFactory.CreateLogger<ParsingApplicationService>();

            return new ParsingApplicationService(
                parser,
                basicParsingService,
                detailedParsingService,
                dbContext,
                logger);
        }

        /// <summary>
        /// Создает инстансы ParsingApplicationService для всех доступных парсеров
        /// </summary>
        /// <param name="parsers">Коллекция парсеров</param>
        /// <returns>Словарь инстансов сервисов по названиям магазинов</returns>
        public Dictionary<string, ParsingApplicationService> CreateForAllParsers(IEnumerable<IParser> parsers)
        {
            if (parsers == null)
                throw new ArgumentNullException(nameof(parsers));

            var services = new Dictionary<string, ParsingApplicationService>();

            foreach (var parser in parsers)
            {
                var service = CreateForParser(parser);
                services[parser.ShopName] = service;
            }

            return services;
        }

        /// <summary>
        /// Создает инстанс ParsingApplicationService для парсера по названию магазина
        /// </summary>
        /// <param name="shopName">Название магазина</param>
        /// <param name="availableParsers">Доступные парсеры</param>
        /// <returns>Инстанс сервиса или null, если парсер не найден</returns>
        public ParsingApplicationService? CreateForShop(string shopName, IEnumerable<IParser> availableParsers)
        {
            if (string.IsNullOrEmpty(shopName))
                throw new ArgumentException("Название магазина не может быть пустым", nameof(shopName));

            if (availableParsers == null)
                throw new ArgumentNullException(nameof(availableParsers));

            var parser = availableParsers.FirstOrDefault(p => p.ShopName.Equals(shopName, StringComparison.OrdinalIgnoreCase));
            
            return parser != null ? CreateForParser(parser) : null;
        }
    }
} 