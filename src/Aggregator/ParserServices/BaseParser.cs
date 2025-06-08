using Aggregator.Data;
using Aggregator.Interfaces;
using Aggregator.Models;
using Aggregator.Services;
using Aggregator.Helpers;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Aggregator.ParserServices
{
    /// <summary>
    /// Базовый класс для парсеров товаров с поддержкой двухэтапного парсинга
    /// Этап 1: Базовый парсинг - название товара и ссылка на страницу
    /// Этап 2: Детальный парсинг - описание, материал, варианты, изображения
    /// </summary>
    /// <remarks>
    /// Класс содержит общую логику для:
    /// - Парсинга HTML страниц с товарами
    /// - Извлечения информации о товарах (название, цена, изображения)
    /// - Сохранения товаров в базу данных
    /// - Загрузки и сохранения изображений товаров
    /// 
    /// Наследники должны определить специфичные для сайта селекторы и базовый URL.
    /// </remarks>
    /// <remarks>
    /// Инициализирует новый экземпляр базового парсера
    /// </remarks>
    /// <param name="clientFactory">Фабрика HTTP клиентов</param>
    /// <param name="logger">Логгер</param>
    /// <param name="imageService">Сервис для работы с изображениями</param>
    public abstract class BaseParser(
        IHttpClientFactory clientFactory,
        ILogger logger,
        ImageService imageService) : IParser
    {
        #region Fields and Properties

        /// <summary>
        /// Фабрика HTTP-клиентов для сетевых запросов
        /// </summary>
        protected readonly IHttpClientFactory _clientFactory = clientFactory;

        /// <summary>
        /// Логгер для записи событий парсинга
        /// </summary>
        protected readonly ILogger _logger = logger;

        /// <summary>
        /// Сервис для работы с изображениями
        /// </summary>
        protected readonly ImageService _imageService = imageService;

        #endregion

        #region Abstract Properties

        /// <summary>
        /// Название магазина (должно быть реализовано в наследниках)
        /// </summary>
        public abstract string ShopName { get; }

        /// <summary>
        /// Базовый URL для парсинга списка товаров
        /// </summary>
        protected abstract string BaseUrl { get; }

        /// <summary>
        /// CSS селектор для поиска товаров на странице каталога
        /// </summary>
        protected abstract string ProductSelector { get; }

        /// <summary>
        /// CSS селектор для извлечения названия товара
        /// </summary>
        protected abstract string NameSelector { get; }

        /// <summary>
        /// CSS селектор для извлечения ссылки на товар
        /// </summary>
        protected abstract string ProductLinkSelector { get; }

        #endregion

        #region Optional Selectors (для старых парсеров)

        /// <summary>
        /// CSS селектор для извлечения цены (для обратной совместимости)
        /// </summary>
        protected virtual string PriceSelector => "";

        /// <summary>
        /// CSS селектор для извлечения изображения (для обратной совместимости)
        /// </summary>
        protected virtual string ImageSelector => "";

        #endregion

        #region Two-Step Parsing Implementation

        /// <summary>
        /// Этап 1: Парсинг базовой информации о товарах
        /// </summary>
        public virtual async Task<List<Product>> ParseBasicProductsAsync()
        {
            var products = new List<Product>();
            
            try
            {
                var doc = await LoadHtmlDocumentAsync(BaseUrl);
                var productNodes = doc.DocumentNode.SelectNodes(ProductSelector);

                if (productNodes == null)
                {
                    _logger.LogWarning("Товары не найдены на странице {ShopName}", ShopName);
                    return products;
                }

                foreach (var node in productNodes)
                {
                    var name = node.SelectSingleNode(NameSelector)?.InnerText?.Trim();
                    var productLink = ExtractProductLink(node);

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(productLink))
                    {
                        products.Add(new Product
                        {
                            Name = name,
                            ProductUrl = productLink,
                            ParsingStatus = ParsingStatus.BasicParsed,
                            // ShopId will be set by the calling service
                        });
                    }
                }

                _logger.LogInformation("Найдено {count} товаров с базовой информацией для магазина {ShopName}", 
                    products.Count, ShopName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при базовом парсинге магазина {ShopName}", ShopName);
            }

            return products;
        }

        /// <summary>
        /// Этап 2: Парсинг детальной информации о товаре
        /// </summary>
        public virtual async Task<Product> ParseDetailedProductAsync(Product product)
        {
            if (string.IsNullOrEmpty(product.ProductUrl))
            {
                _logger.LogWarning("У товара {ProductName} отсутствует ссылка для детального парсинга", product.Name);
                return product;
            }

            try
            {
                var doc = await LoadHtmlDocumentAsync(product.ProductUrl);
                
                // Парсим детальную информацию (переопределяется в наследниках)
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

        /// <summary>
        /// Переопределяемый метод для парсинга детальной информации о товаре
        /// </summary>
        protected virtual async Task ParseProductDetailsAsync(Product product, HtmlDocument doc)
        {
            // Базовая реализация - может быть переопределена в наследниках
            
            // Пример парсинга описания
            var descriptionNode = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
            if (descriptionNode != null)
            {
                product.Description = descriptionNode.GetAttributeValue("content", "")?.Trim();
            }

            // TODO: Добавить парсинг других полей (материал, варианты и т.д.)
            // Это будет реализовано в конкретных парсерах
            
            await Task.CompletedTask; // Для async совместимости
        }

        #endregion

        #region Backward Compatibility

        /// <summary>
        /// Устаревший метод для обратной совместимости
        /// </summary>
        [Obsolete("Используйте ParseBasicProductsAsync и ParseDetailedProductAsync")]
        public virtual async Task<List<Product>> ParseProducts()
        {
            // Простая реализация для обратной совместимости
            var basicProducts = await ParseBasicProductsAsync();
            
            // Для обратной совместимости возвращаем только базовую информацию
            _logger.LogWarning("Используется устаревший метод ParseProducts() в парсере {ShopName}. " +
                              "Рекомендуется перейти на двухэтапный парсинг.", ShopName);
            
            return basicProducts;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Загружает HTML документ из URL или файла
        /// </summary>
        protected async Task<HtmlDocument> LoadHtmlDocumentAsync(string url)
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
        /// Извлекает URL изображения из HTML узла
        /// </summary>
        protected virtual string? ExtractImageUrl(HtmlNode node)
        {
            if (string.IsNullOrEmpty(ImageSelector)) return null;

            var imageNode = node.SelectSingleNode(ImageSelector);
            if (imageNode == null) return null;

            // Для тега img с атрибутом src
            var src = imageNode.GetAttributeValue("src", "");
            if (!string.IsNullOrEmpty(src))
            {
                return NormalizeImageUrl(src);
            }

            // Для элементов с background-image в style
            var style = imageNode.GetAttributeValue("style", "");
            if (!string.IsNullOrEmpty(style))
            {
                var urlMatch = System.Text.RegularExpressions.Regex.Match(style, @"url\(([^)]+)\)");
                if (urlMatch.Success)
                {
                    var url = urlMatch.Groups[1].Value.Trim('"', '\'');
                    return NormalizeImageUrl(url);
                }
            }

            return null;
        }

        /// <summary>
        /// Нормализует URL изображения
        /// </summary>
        protected virtual string NormalizeImageUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return string.Empty;

            if (url.StartsWith('/'))
            {
                var baseUri = new Uri(BaseUrl);
                return $"{baseUri.Scheme}://{baseUri.Host}{url}";
            }

            return url;
        }

        /// <summary>
        /// Извлекает ссылку на товар из HTML узла
        /// </summary>
        protected virtual string? ExtractProductLink(HtmlNode node)
        {
            var linkNode = node.SelectSingleNode(ProductLinkSelector);
            if (linkNode == null) return null;

            var href = linkNode.GetAttributeValue("href", "");
            if (string.IsNullOrEmpty(href)) return null;

            return NormalizeProductLink(href);
        }

        /// <summary>
        /// Нормализует ссылку на товар
        /// </summary>
        protected virtual string NormalizeProductLink(string link)
        {
            if (string.IsNullOrEmpty(link)) return string.Empty;

            if (link.StartsWith('/'))
            {
                var baseUri = new Uri(BaseUrl);
                return $"{baseUri.Scheme}://{baseUri.Host}{link}";
            }

            return link;
        }

        #endregion
    }
}