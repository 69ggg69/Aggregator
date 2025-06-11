using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Aggregator.Interfaces;
using Aggregator.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Aggregator.ParserServices
{
    /// <summary>
    /// Базовый класс для парсинга основной информации о товарах из каталогов магазинов
    /// Реализует первый этап двухступенчатого парсинга - извлечение названий товаров и ссылок
    /// </summary>
    /// <remarks>
    /// Класс предназначен для:
    /// - Парсинга каталогных страниц магазинов
    /// - Извлечения базовой информации о товарах (название и ссылка)
    /// - Поддержки пагинации и множественных базовых URL
    /// - Обработки различных правил навигации по страницам
    /// 
    /// Наследники должны определить специфичные для магазина селекторы, URL и правила пагинации.
    /// </remarks>
    /// <remarks>
    /// Инициализирует новый экземпляр парсера магазина
    /// </remarks>
    /// <param name="clientFactory">Фабрика HTTP клиентов</param>
    /// <param name="logger">Логгер</param>
    public abstract class ShopParser(IHttpClientFactory clientFactory, ILogger logger) : IParser
    {
        #region Fields and Properties

        /// <summary>
        /// Фабрика HTTP-клиентов для сетевых запросов
        /// </summary>
        protected readonly IHttpClientFactory _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));

        /// <summary>
        /// Логгер для записи событий парсинга
        /// </summary>
        protected readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        #endregion
        #region Constructor

        #endregion

        #region Abstract Properties

        /// <summary>
        /// Название магазина
        /// </summary>
        public abstract string ShopName { get; }

        /// <summary>
        /// URL магазина
        /// </summary>
        public abstract string ShopUrl { get; }

        /// <summary>
        /// Массив базовых URL для парсинга с правилами навигации по страницам
        /// Каждый элемент содержит URL и массив правил пагинации
        /// </summary>
        public abstract ShopUrlConfig[] BaseUrls { get; }

        /// <summary>
        /// CSS селектор для поиска товаров на странице каталога
        /// </summary>
        protected abstract string ProductSelector { get; }

        /// <summary>
        /// CSS селектор для извлечения названия товара
        /// </summary>
        protected abstract string ProductNameSelector { get; }

        /// <summary>
        /// CSS селектор для извлечения ссылки на товар
        /// </summary>
        protected abstract string ProductLinkSelector { get; }

        #endregion

        #region IParser Implementation

        /// <summary>
        /// Парсит основную информацию о товарах из всех настроенных URL магазина
        /// </summary>
        /// <returns>Список товаров с базовой информацией</returns>
        public virtual async Task<List<Product>> ParseBasicProductsAsync()
        {
            var allProducts = new List<Product>();

            foreach (var urlConfig in BaseUrls)
            {
                try
                {
                    var productsFromUrl = await ParseProductsFromUrlConfigAsync(urlConfig);
                    allProducts.AddRange(productsFromUrl);

                    _logger.LogInformation("Получено {count} товаров с URL: {url} для магазина {shopName}",
                        productsFromUrl.Count, urlConfig.BaseUrl, ShopName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при парсинге URL: {url} для магазина {shopName}",
                        urlConfig.BaseUrl, ShopName);
                }
            }

            _logger.LogInformation("Всего найдено {totalCount} товаров для магазина {shopName}",
                allProducts.Count, ShopName);

            return allProducts;
        }

        /// <summary>
        /// Парсит детальную информацию о товаре
        /// Базовая реализация - заглушка, должна быть переопределена в наследниках
        /// </summary>
        /// <param name="product">Товар с базовой информацией</param>
        /// <returns>Товар с детальной информацией</returns>
        public virtual async Task<Product> ParseDetailedProductAsync(Product product)
        {
            if (string.IsNullOrEmpty(product.ProductUrl))
            {
                _logger.LogWarning("У товара {ProductName} отсутствует ссылка для детального парсинга", product.Name);
                return product;
            }

            try
            {
                _logger.LogDebug("Начинаем детальный парсинг товара {ProductName} из магазина {ShopName}",
                    product.Name, ShopName);

                var doc = await LoadHtmlDocumentAsync(product.ProductUrl);

                // Базовая реализация - парсим детальную информацию
                await ParseProductDetailsAsync(product, doc);

                product.ParsingStatus = ParsingStatus.DetailedParsed;
                product.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Детально спаршен товар {ProductName} из магазина {ShopName}",
                    product.Name, ShopName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при детальном парсинге товара {ProductName} из магазина {ShopName}",
                    product.Name, ShopName);
            }

            return product;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Переопределяемый метод для парсинга детальной информации о товаре
        /// Базовая реализация - заглушка, должна быть переопределена в наследниках
        /// </summary>
        /// <param name="product">Товар для обновления</param>
        /// <param name="doc">HTML документ страницы товара</param>
        protected virtual async Task ParseProductDetailsAsync(Product product, HtmlDocument doc)
        {
            // Базовая реализация - заглушка
            // Парсим базовое описание из meta тега
            var descriptionNode = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
            if (descriptionNode != null)
            {
                var description = descriptionNode.GetAttributeValue("content", "")?.Trim();
                if (!string.IsNullOrEmpty(description))
                {
                    product.Description = description;
                }
            }

            _logger.LogDebug("Базовый парсинг детальной информации для товара {ProductName} завершен", product.Name);

            // Заглушка для async/await
            await Task.CompletedTask;
        }

        /// <summary>
        /// Парсит товары из одной конфигурации URL с учетом правил пагинации
        /// </summary>
        /// <param name="urlConfig">Конфигурация URL с правилами пагинации</param>
        /// <returns>Список товаров</returns>
        protected virtual async Task<List<Product>> ParseProductsFromUrlConfigAsync(ShopUrlConfig urlConfig)
        {
            var products = new List<Product>();
            var currentUrl = urlConfig.BaseUrl;
            var pageNumber = 1;

            do
            {
                try
                {
                    var doc = await LoadHtmlDocumentAsync(currentUrl);
                    var pageProducts = await ParseProductsFromPageAsync(doc);

                    if (pageProducts.Count == 0)
                    {
                        _logger.LogInformation("На странице {pageNumber} не найдено товаров. Завершение парсинга URL: {url}",
                            pageNumber, currentUrl);
                        break;
                    }

                    products.AddRange(pageProducts);
                    _logger.LogDebug("Страница {pageNumber}: найдено {count} товаров", pageNumber, pageProducts.Count);

                    // Получаем следующий URL для пагинации
                    currentUrl = GetNextPageUrl(urlConfig, pageNumber);
                    pageNumber++;

                    // Проверяем, нужно ли продолжать пагинацию
                    if (string.IsNullOrEmpty(currentUrl) || !ShouldContinuePagination(urlConfig, pageNumber))
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при парсинге страницы {pageNumber} для URL: {url}",
                        pageNumber, currentUrl);
                }
            }
            while (true);

            return products;
        }



        /// <summary>
        /// Парсит товары с одной HTML страницы
        /// </summary>
        /// <param name="doc">HTML документ страницы</param>
        /// <returns>Список товаров со страницы</returns>
        protected virtual async Task<List<Product>> ParseProductsFromPageAsync(HtmlDocument doc)
        {
            var products = new List<Product>();
            var productNodes = doc.DocumentNode.SelectNodes(ProductSelector);

            if (productNodes == null || productNodes.Count == 0)
            {
                return products;
            }

            foreach (var node in productNodes)
            {
                try
                {
                    var name = ExtractProductName(node);
                    var productLink = ExtractProductLink(node);

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(productLink))
                    {
                        products.Add(new Product
                        {
                            Name = name,
                            ProductUrl = productLink,
                            ParsingStatus = ParsingStatus.BasicParsed,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ошибка при парсинге товара из узла HTML");
                    break;
                }

            }

            await Task.CompletedTask; // Для async совместимости
            return products;
        }

        /// <summary>
        /// Извлекает название товара из HTML узла
        /// </summary>
        /// <param name="node">HTML узел товара</param>
        /// <returns>Название товара</returns>
        protected virtual string? ExtractProductName(HtmlNode node)
        {
            var nameNode = node.SelectSingleNode(ProductNameSelector);
            _logger.LogDebug("Найдено название товара с селектором {Selector}: {ProductName}", ProductNameSelector, nameNode?.InnerText?.Trim());
            return nameNode?.InnerText?.Trim();
        }

        /// <summary>
        /// Извлекает ссылку на товар из HTML узла
        /// </summary>
        /// <param name="node">HTML узел товара</param>
        /// <returns>Ссылка на товар</returns>
        protected virtual string? ExtractProductLink(HtmlNode node)
        {
            var linkNode = node.SelectSingleNode(ProductLinkSelector);
            if (linkNode == null) return null;

            var href = linkNode.GetAttributeValue("href", "");
            if (string.IsNullOrEmpty(href)) return null;

            return NormalizeProductLink(href);
        }

        /// <summary>
        /// Нормализует ссылку на товар (делает абсолютной, если она относительная)
        /// </summary>
        /// <param name="link">Исходная ссылка</param>
        /// <returns>Нормализованная ссылка</returns>
        protected virtual string NormalizeProductLink(string link)
        {
            if (string.IsNullOrEmpty(link)) return string.Empty;

            if (link.StartsWith('/'))
            {
                // Для относительных ссылок используем первый базовый URL для определения домена
                var baseUrl = BaseUrls.FirstOrDefault()?.BaseUrl;
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    var baseUri = new Uri(baseUrl);
                    return $"{baseUri.Scheme}://{baseUri.Host}{link}";
                }
            }

            return link;
        }

        /// <summary>
        /// Загружает HTML документ из URL
        /// </summary>
        /// <param name="url">URL для загрузки</param>
        /// <returns>HTML документ</returns>
        protected virtual async Task<HtmlDocument> LoadHtmlDocumentAsync(string url)
        {
            var doc = new HtmlDocument();

            if (url.StartsWith("http://") || url.StartsWith("https://"))
            {
                var web = new HtmlWeb();
                doc = await web.LoadFromWebAsync(url);
            }
            else if (url.StartsWith("file://"))
            {
                var filePath = url.Replace("file://", "");
                var html = await File.ReadAllTextAsync(filePath);
                doc.LoadHtml(html);
            }
            else
            {
                var html = await File.ReadAllTextAsync(url);
                doc.LoadHtml(html);
            }

            return doc;
        }

        /// <summary>
        /// Получает URL следующей страницы на основе правил пагинации
        /// </summary>
        /// <param name="urlConfig">Конфигурация URL</param>
        /// <param name="currentPageNumber">Номер текущей страницы</param>
        /// <returns>URL следующей страницы или null, если пагинация не требуется</returns>
        protected virtual string? GetNextPageUrl(ShopUrlConfig urlConfig, int currentPageNumber)
        {
            if (urlConfig.PaginationRules == null || urlConfig.PaginationRules.Length == 0)
            {
                return null; // Пагинация не настроена
            }

            // Простая реализация для URL-паттернов
            // В будущем можно расширить для поддержки API запросов и скроллинга
            var rule = urlConfig.PaginationRules.FirstOrDefault();
            if (string.IsNullOrEmpty(rule))
            {
                return null;
            }

            // Заменяем плейсхолдеры в правиле пагинации
            var nextPageNumber = currentPageNumber + 1;
            return rule.Replace("{page}", nextPageNumber.ToString())
                      .Replace("{baseUrl}", urlConfig.BaseUrl);
        }

        /// <summary>
        /// Определяет, нужно ли продолжать пагинацию
        /// </summary>
        /// <param name="urlConfig">Конфигурация URL</param>
        /// <param name="pageNumber">Номер текущей страницы</param>
        /// <returns>True, если нужно продолжать пагинацию</returns>
        protected virtual bool ShouldContinuePagination(ShopUrlConfig urlConfig, int pageNumber)
        {
            // Базовая логика - можно переопределить в наследниках
            // Например, ограничить максимальное количество страниц
            return pageNumber <= 100; // Ограничение для безопасности
        }

        #endregion
    }

    /// <summary>
    /// Конфигурация URL магазина с правилами пагинации
    /// </summary>
    public class ShopUrlConfig
    {
        /// <summary>
        /// Базовый URL для парсинга
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Правила пагинации для данного URL
        /// Пустой массив означает отсутствие пагинации
        /// Примеры правил:
        /// - "{baseUrl}/page/{page}" - для URL-пагинации
        /// - "api/products?page={page}" - для API запросов
        /// - "scroll" - для скроллинга (будет реализовано позже)
        /// </summary>
        public string[] PaginationRules { get; set; } = Array.Empty<string>();
    }
}